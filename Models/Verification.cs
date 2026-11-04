namespace PoolerBotCS.Models;

public class Verification
{
    public Guid Id { get; set; }
    public ulong DiscordUserId { get; set; }
    public int Code { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    
}

