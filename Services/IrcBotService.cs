
using System.Net.Security;
using System.Net.Sockets;
using PoolerBotCS.Models;

namespace PoolerBotCS.Services;

public class IrcBotService : BackgroundService, IIrcBotService
{
    private string  _host;
    private int     _port;
    private bool    _ssl;
    private string  _username;
    private string  _password;
    private string  _prefix;
    private string  _baseChannel;

    private TcpClient client;
    private bool _isConnected       = false;
    private bool _isAuthenticated   = false;
    private StreamWriter? _writer;
    private StreamReader? _reader;
    private readonly IServiceProvider _serviceProvider;

    private readonly List<PendingBanchoResponse> _pendingBanchoResponses = new();
    private readonly object _pendingBanchoResponsesLock = new();

    private TaskCompletionSource<string>? _pendingLobbyJoin;
    private readonly object _pendingLobbyJoinLock = new();

    public IrcBotService(IConfiguration config, IServiceProvider serviceProvider)
    {
        _host        = config["Irc:Host"] ?? "";
        _port        = config.GetValue<int>("Irc:Port");
        _ssl         = config.GetValue<bool>("Irc:Ssl");
        _username    = config["Irc:Username"] ?? "";
        _password    = config["Irc:Password"] ?? "";
        _prefix      = config["Irc:Prefix"] ?? "";
        _baseChannel = "#osu";
        client = new TcpClient();
        _serviceProvider = serviceProvider;
    }

    protected override async Task<Task> ExecuteAsync(CancellationToken stoppingToken)
    {
        // connect to osu!irc and initialize read-write streams
        await client.ConnectAsync(_host, _port, stoppingToken);
        
        Stream stream = client.GetStream();
        if (_ssl)
        {
            var sslStream = new SslStream(stream, false);
            await sslStream.AuthenticateAsClientAsync(_host);
            stream = sslStream;
        }
        _reader = new StreamReader(stream);
        _writer = new StreamWriter(stream);
        _writer.AutoFlush = true;
        _writer.NewLine = "\r\n";
        
        
        
        // send authentication to osu!irc
        await SendRawMessage($"PASS {_password}");
        await SendRawMessage($"NICK {_username}");
        
        await SendRawMessage($"JOIN {_baseChannel}");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            var line = await _reader.ReadLineAsync(stoppingToken); // read server messages continuously
            if (line is null) break; // closed connection

            if (line.StartsWith("PING"))
            {
                var payload = line.Substring("PING ".Length);
                await _writer.WriteLineAsync($"PONG {payload}"); // we have to respond to pings with the same message
                continue;
            }

            var message = ParseMessage(line);
            var channelId = message.Parameters[1];
            channelId = channelId.StartsWith(':') ? channelId[1..] : channelId;
            LogMessage(line);
            
            switch (message.Command)
            {
                case "376": // RPL_ENDOFMOTD
                    // osu!irc never sends 376 if the authentication is wrong for any reason,
                    // so 376 is a good sign that we are authenticated.
                    // the server normally sends 464 (ERR_PASSWDMISMATCH) if the password 
                    // is wrong, but that message is not guaranteed.
                    _isAuthenticated = true;
                    Log($"Authentication successful!");
                    break;
                
                case "464": // ERR_PASSWDMISMATCH
                    Log("Authentication unsuccessful.");
                    break;
                
                case "001": // RPL_WELCOME 
                    _isConnected = true;
                    Log($"Connected to {_host}:{_port}!");
                    break;
                
                case "MODE": // sent when we get joined into a newly created lobby channel.
                    if (channelId != "#osu")
                    {
                        lock (_pendingLobbyJoinLock)
                        {
                            _pendingLobbyJoin?.TrySetResult(channelId);
                        }
                   }
                    break;
                
                case "JOIN":
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var banchoLobbyService = scope.ServiceProvider.GetRequiredService<IBanchoLobbyService>();
                        banchoLobbyService.PlayerJoinLobby(channelId, message.Username);
                    }

                    break;

                case "PRIVMSG":
                    if (message.Username == "BanchoBot")
                    {
                        DispatchBanchoBotMessage(channelId, message);
                    }

                    break;
            }
            
            
        }
        return Task.CompletedTask;
    }

    public async Task SendRawMessage(string message)
    {
        if (_writer is null) throw new InvalidOperationException("IRC connection is not established yet.");
        await _writer.WriteLineAsync(message);
    }

    /// <summary>
    /// Sends a message to the specified channel.
    /// </summary>
    /// <param name="channel">The IRC channel to send the message, should be preceded with an #.</param>
    /// <param name="message">The message to be sent.</param>
    /// <exception cref="InvalidOperationException">Raised if the IRC connection has not been established yet.</exception>
    public async Task SendMessage(string channel, string message)
    {
        if (_writer is null) throw new InvalidOperationException("IRC connection is not established yet.");
        await _writer.WriteLineAsync($"PRIVMSG {channel} {message}");
        Log("<<", $"PRIVMSG {channel} {message}");
    }

    public async Task<List<string>> SendMessageAndCollectResponses(string channel, string message, int lineCount, TimeSpan? timeout = null)
    {
        if (lineCount <= 0) throw new ArgumentOutOfRangeException(nameof(lineCount));

        var pending = new PendingBanchoResponse(channel, lineCount);
        lock (_pendingBanchoResponsesLock)
        {
            _pendingBanchoResponses.Add(pending);
        }

        try
        {
            await SendMessage(channel, message);

            using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(10));
            await using var registration = cts.Token.Register(() => pending.Tcs.TrySetException(
                new TimeoutException($"Timed out waiting for {lineCount} line(s) from BanchoBot in {channel}.")));

            return await pending.Tcs.Task;
        }
        finally
        {
            lock (_pendingBanchoResponsesLock)
            {
                _pendingBanchoResponses.Remove(pending);
            }
        }
    }

    public async Task<string> CreateLobby(string channel, string name, TimeSpan? timeout = null)
    {
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_pendingLobbyJoinLock)
        {
            if (_pendingLobbyJoin != null) throw new InvalidOperationException("A lobby creation is already in progress.");
            _pendingLobbyJoin = tcs;
        }

        try
        {
            await SendMessage(channel, $"!mp make {name}");

            using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(10));
            await using var registration = cts.Token.Register(() => tcs.TrySetException(
                new TimeoutException($"Timed out waiting for the lobby \"{name}\" to be joined.")));

            return await tcs.Task;
        }
        finally
        {
            lock (_pendingLobbyJoinLock)
            {
                _pendingLobbyJoin = null;
            }
        }
    }

    private void DispatchBanchoBotMessage(string channel, IRCMessage message)
    {
        var text = string.Join(" ", message.Parameters[2..]);
        text = text.StartsWith(':') ? text[1..] : text;

        lock (_pendingBanchoResponsesLock)
        {
            var pending = _pendingBanchoResponses.FirstOrDefault(p => p.Channel == channel && p.LinesRemaining > 0);
            if (pending != null)
            {
                pending.Lines.Add(text);
                pending.LinesRemaining--;

                if (pending.LinesRemaining == 0)
                {
                    pending.Tcs.TrySetResult(new List<string>(pending.Lines));
                }
                
            }

            else
            {
                using var scope = _serviceProvider.CreateScope();
                var banchoLobbyService = scope.ServiceProvider.GetRequiredService<IBanchoLobbyService>();
                banchoLobbyService.ParseBanchoBotMessage(channel, string.Join(' ', message.Parameters[2..]));
            }

        }
    }




    // users prefixed with "+" are connected via external IRC,
    // and users prefixed with "@" are GMTs.
    private static string CleanUsername(string username)
    {
        if (username.StartsWith('+') || username.StartsWith('@'))
        {
            return username[1..];
        }

        return username;
    }

    private static IRCMessage ParseMessage(string message)
    {
        var (source, command, parameters) = message.Split(" ");
        
        source = source.StartsWith(':') ? source[1..] : source;

        var (username, _) = source.Split("!"); // 2nd param is always the hostname, can be ignored

        parameters[0] = parameters[0].StartsWith(':') ? parameters[0][1..] : parameters[0];

        return new IRCMessage()
        {
            Command = command,
            Parameters = parameters,
            Username = username
        };
    }
    
    
    private static void LogMessage(string message, bool ignoreCrap = true, bool ignoreOsu = true)
    {
        var (source, command, parameters) = message.Split(" ");
        
        var (username, _) = source.Split("!"); // 2nd param is always the hostname, can be ignored
        username = CleanUsername(username);

        var (_, channel, messages) = parameters;

        channel = channel.StartsWith(':') ? channel[1..] : channel; // trim leading ":"
        
        if (ignoreOsu && (channel == "#osu" || parameters[1] == ":#osu")) return;
        
        // QUIT sent whenever someone leaves the server (at least 10 per second lmao)
        // 372 = motd, 375 = motd start, we capture motd end
        if (ignoreCrap && 
            command is 
                "QUIT" or "372" or "375" or 
                "353" or "366"
           ) return;
        
        if (command is "PRIVMSG")
        {
            Log(source, command, parameters);
            //Log($"{username}: {string.Join(" ", parameters)}");
        }
        else
        {
            Log(">>", source, command, parameters);
        }
        
    }

    
    
    
    
    private static void Log(params object?[] parameters)
    {
       var now = DateTime.Now;
       var msgLine = $"{now} // [IRC] ";
       foreach (var param in parameters)
       {
           if (param is object[] items)
           {
               msgLine += "[" + string.Join(", ", items) + "]";
           }
           else
           {
               msgLine += param + " ";
           }
       }

       Console.WriteLine(msgLine);
    }

    private class PendingBanchoResponse
    {
        public PendingBanchoResponse(string channel, int lineCount)
        {
            Channel = channel;
            LinesRemaining = lineCount;
        }

        public string Channel { get; }
        public int LinesRemaining { get; set; }
        public List<string> Lines { get; } = new();
        public TaskCompletionSource<List<string>> Tcs { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}


public class IRCMessage
{
    public string   Username    { get; set; }
    public string   Command     { get; set; }
    public string[] Parameters  { get; set; }
}

public static class ArrayExtensions
{
    public static void Deconstruct(this string[] array, out string first, out string second)
    {
        first = array.Length > 0 ? array[0] : null;
        second = array.Length > 1 ? array[1] : null;
    }
    public static void Deconstruct(this string[] array, out string first, out string second, out string[] third)
    {
        first = array.Length > 0 ? array[0] : null;
        second = array.Length > 1 ? array[1] : null;
        third = array.Length > 2 ? array[1..] : null;
    }
}
