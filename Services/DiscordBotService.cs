using Discord;
using Discord.WebSocket;

namespace PoolerBotCS.Services;

public class DiscordBotService : BackgroundService, IDiscordBotService
{
    private readonly DiscordSocketClient _client;
    private string _token;
    
    public DiscordBotService(IConfiguration config)
    {
        _client = new DiscordSocketClient();
        _token = config["Discord:Token"] ?? "";
        
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log("Connecting to Discord...");
        await _client.LoginAsync(TokenType.Bot, _token);
        await _client.StartAsync();
        
        Log("Discord Bot Started");
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
