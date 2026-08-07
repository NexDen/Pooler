namespace PoolerBotCS.Models;

public class PoolerPlayer : BanchoPlayer
{
    public Role CurrentRole { get; set; } = Role.Player;
    public bool IsDead { get; set; } = false;
    public bool IsInLobby  { get; set; } = false;
}

public enum Role
{
    Reader,         // okurgeçer
    Tapper,         // tambasan
    Antimodder,     // modötesi
    Thrower,        // oyunsatan
    Cheater,        // hilebaz
    Player,         // düz oyuncu
    Pooler,         // havuz mu cu
}