using System.Collections.Concurrent;
using Discord;
using Discord.WebSocket;

namespace PoolerBotCS.Services;

public class DiscordBotService : BackgroundService, IDiscordBotService
{
    private readonly DiscordSocketClient _client;
    private readonly string _token;

    // keyed by discord user id, completed when that user's next DM arrives
    private readonly ConcurrentDictionary<ulong, TaskCompletionSource<string>> _pendingReplies = new();

    public DiscordBotService(IConfiguration config)
    {
        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds
                              | GatewayIntents.GuildMessages
                              | GatewayIntents.DirectMessages
                              | GatewayIntents.MessageContent
        });
        _token = config["Discord:Token"] ?? "";
        _client.Log += LogDiscord;
        _client.MessageReceived += OnMessageReceived;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log("Connecting to Discord...");
        await _client.LoginAsync(TokenType.Bot, _token);
        await _client.StartAsync();

        Log("Discord Bot Started");
    }

    private Task OnMessageReceived(SocketMessage message)
    {
        if (message.Author.Id == _client.CurrentUser.Id) return Task.CompletedTask;
        if (message.Channel is not IDMChannel) return Task.CompletedTask;

        if (_pendingReplies.TryRemove(message.Author.Id, out var tcs))
        {
            tcs.TrySetResult(message.Content);
        }

        return Task.CompletedTask;
    }

    public async Task SendDirectMessage(ulong userId, string message)
    {
        var user = _client.GetUser(userId);
        if (user == null) return;

        var dmChannel = await user.CreateDMChannelAsync();
        await dmChannel.SendMessageAsync(message);
    }

    public async Task<string?> AskDirectMessage(ulong userId, string question, TimeSpan timeout)
    {
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingReplies[userId] = tcs;

        await SendDirectMessage(userId, question);

        using var cts = new CancellationTokenSource(timeout);
        await using var registration = cts.Token.Register(() => tcs.TrySetCanceled());

        try
        {
            return await tcs.Task;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
        finally
        {
            _pendingReplies.TryRemove(userId, out _);
        }
    }





    public async Task SendMessage(ulong guildId, ulong channelId, string message){
        var isInGuild = IsInGuild(guildId);
        if (!isInGuild) return;
        
        var channel = _client.GetGuild(guildId).GetTextChannel(channelId);
        if (channel == null) return;
        
        await channel.SendMessageAsync(message);
        
    }




    private bool IsInGuild(ulong guildId) {
        var guild = _client.GetGuild(guildId);
        return guild != null;
    }

    private Task LogDiscord(LogMessage arg) {
        Log(arg.Message, arg.Exception);
        return Task.CompletedTask;
    }

    private static void Log(object? message, params object[]? messages)
    {
        var msg = "[DISCORD] " + message?.ToString();
        foreach (var msgLine in messages??[])
        {
            msg += " " + msgLine.ToString();
        }
        Console.WriteLine(msg);
    }
    
}
