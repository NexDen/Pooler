using System.ComponentModel.DataAnnotations.Schema;

namespace PoolerBotCS.Models;

public class BanchoLobby
{
    public Guid Id { get; set; }
    public bool IsActive { get; set; }
    public string IrcMatchId { get; set; }
    public string AssignedMatchId { get; set; } 
    public PoolerMap? CurrentMap { get; set; }
    public List<PoolerMap> MapPool { get; set; }
    public List<PoolerPlayer> Players { get; set; }
    public bool IsReady { get; set; }
    public bool IsFinished { get; set; }
    public LobbyStatus LobbyStatus { get; set; }
 
    public ulong? LobbyDiscordGuildId { get; set; }
    public ulong? LobbyDiscordChannelId { get; set; }

    // signal to the game loop that the lobby is ready
    [NotMapped] 
    public TaskCompletionSource<bool>? ReadySignal { get; set; }
    [NotMapped]
    public TaskCompletionSource<bool>? FinishedSignal { get; set; }
}

public class PoolerMap
{
    public Guid Id { get; set; }
    public int BeatmapId { get; set; }
    public string Url { get; set; }
    public MapMods Mods { get; set; }
    public bool IsThrowerMap = false;
}

public enum MapMods
{
    NM,
    HD,
    HR, 
    HDHR,
    EZ,
    EZHD,
}

public enum LobbyStatus
{
    NotReady,
    Ready,
    Validated,
    InProgress,
    Discussion,
    Voting,
} 
