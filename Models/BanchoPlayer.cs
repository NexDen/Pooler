using System.ComponentModel.DataAnnotations.Schema;

namespace PoolerBotCS.Models;

public class BanchoPlayer
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public ulong DiscordUserId { get; set; }
    public bool IsVerified { get; set; }
    public Verification? ActiveVerification { get; set; }
}
