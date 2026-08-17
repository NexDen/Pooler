using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using PoolerBotCS.Models;

namespace PoolerBotCS.Services;

public class BanchoLobbyService : IBanchoLobbyService
{
    
    private readonly IBanchoPlayerService _playerService;
    private readonly IIrcBotService _ircBotService;
    private List<BanchoLobby> _lobbies = new();

    public BanchoLobbyService(IBanchoPlayerService playerService,  IIrcBotService ircBotService)
    {
        _playerService = playerService;
        _ircBotService = ircBotService;
    }

    public void Make(string name)
    {
        _ircBotService.SendMessage("BanchoBot", $"!mp make {name}");
    }
    
    public async Task Create(string[] playerNames)
    {
        List<PoolerPlayer> Players = [];

        foreach (var playerName in playerNames)
        {
            var player = (PoolerPlayer)_playerService.GetOrCreatePlayerByName(playerName);
            Players.Add(player);
        }
        
        var playersCopy = new List<PoolerPlayer>(Players);
        var targetPlayers = new List<PoolerPlayer>();
        var roleCounts = RoleCounts(Players.Count);

        Log(roleCounts);

        for (var i = 0; i < roleCounts.Length; i++)
        {
            for (var j = roleCounts[i]; j > 0 ; j--)
            {
                var randomPlayer = playersCopy[Random.Shared.Next(playersCopy.Count)];
                randomPlayer.CurrentRole = i switch
                {
                    0 => Role.Pooler,
                    1 => Role.Reader,
                    2 => Role.Tapper,
                    3 => Role.Antimodder,
                    4 => Random.Shared.Next(2) == 0 ? Role.Thrower : Role.Cheater, // her poola max 1 tane oyunsatan/hilebaz/oyuncu
                    _ => Role.Player,
                };
                Log(roleCounts, i);
                roleCounts[i]--;
                playersCopy.Remove(randomPlayer);
                targetPlayers.Add(randomPlayer);
            }

        }


        Players = targetPlayers;
        

        var playerCount = Players.Count;
        var mapCount = MathF.Ceiling(playerCount/2) - 1;
        if (playerCount >= 10){
            mapCount -= 1;
        }
        if (Players.Any(p => p.CurrentRole == Role.Thrower)){
            mapCount -= 1; // to be filled by the thrower map
        }
    
           
        
        // poolers create the pool
        
        var pool = new List<PoolerMap>
        {
            // fake pool for testing
            new()
            {
                BeatmapId = 315,
                Url = "https://osu.ppy.sh/beatmapsets/141#osu/315",
                Mods = MapMods.NM,
            },
            new()
            {
                BeatmapId = 4907863,
                Url = "https://osu.ppy.sh/beatmapsets/2250239#osu/4907863",
                Mods = MapMods.HD,
                IsThrowerMap = true
            },
            new()
            {
                BeatmapId = 5341644,
                Url = "https://osu.ppy.sh/beatmapsets/2191876#osu/5341644",
                Mods = MapMods.HR,
            },
        };

        var poolWithoutThrower = pool.Where(m => !m.IsThrowerMap).ToList();
        var throwerMap = pool.First(m => m.IsThrowerMap); // .First is fine since we know there's only one thrower map

        // shuffle the pool without the thrower map to ensure that it's not the last item
        poolWithoutThrower = poolWithoutThrower.OrderBy(m => Random.Shared.Next()).ToList();
        var randomIdx = Random.Shared.Next(poolWithoutThrower.Count - 1);
        // insert the thrower map at the random index that can't be the last index :D
        poolWithoutThrower.Insert(randomIdx, throwerMap);
        
        // TODO: reveal pool via discord

        // TODO: get confirmation from players
        
        // make bancho lobby
        var assignedId = Random.Shared.Next(1000, 9999).ToString();
        var lobbyChannelId = await _ircBotService.CreateLobby("BanchoBot", $"Pooler-{assignedId}");
        Log($"Joined lobby channel: {lobbyChannelId}");
        var lobby = new BanchoLobby()
        {
            Id = Guid.NewGuid(),
            IrcMatchId = lobbyChannelId,
            AssignedMatchId = assignedId,
            IsActive = true,
            MapPool = poolWithoutThrower,
            Players = targetPlayers,
            LobbyStatus = LobbyStatus.NotReady,
            ReadySignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously),
            FinishedSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously),
        };
        _lobbies.Add(lobby);

        // invite players
        foreach (var player in targetPlayers)
        {
            Log(player.Username, player.CurrentRole.ToString());
            await _ircBotService.SendMessage(lobbyChannelId, $"!mp invite {player.Username}");
            Log($"Invited {player.Username} to lobby {lobbyChannelId}");
        }
        
        // start game loop
        var playedMapCount = 0;

        while (playedMapCount < pool.Count){
            lobby.ReadySignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            lobby.FinishedSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var map = pool[playedMapCount];
            ChangeMap(lobbyChannelId, map.BeatmapId.ToString());
            playedMapCount++;
            Log($"Changing map to {map.BeatmapId}, played {playedMapCount} of {pool.Count}");
            await lobby.ReadySignal.Task; // wait for the signal that everyone is ready
            lobby.IsReady = true;
            Log($"All players are ready in {lobbyChannelId}");
            await _ircBotService.SendMessage(lobbyChannelId, $"!mp start 10");
            lobby.LobbyStatus = LobbyStatus.InProgress;
            await lobby.FinishedSignal.Task; // wait for the signal that the map is finished
            Log($"The match has finished!");
            lobby.LobbyStatus = LobbyStatus.Discussion;
            
        }

    }

    public void ChangeMap(string lobbyId, string mapId)
    {
        var lobby = FindLobby(lobbyId);
        if (lobby == null) return;

        var map = FindMap(lobby, mapId);
        if (map == null) return;

        _ircBotService.SendMessage(lobbyId, $"!mp map {map.BeatmapId}");
        //_ircBotService.SendMessage(lobbyId, $"!mp mods {string.Join(" ", GetModTokens(map.Mods))}");

        lobby.CurrentMap = map;
    }

    public void PlayerJoinLobby(string lobbyId, string playerName)
    {
        if (!lobbyId.StartsWith("#mp_")) return; // ignore saving players just logging into #osu
        var lobby = FindLobby(lobbyId);
        var player = _playerService.GetOrCreatePlayerByName(playerName);
        Log($"Player {player.Username} joined the lobby {lobby?.Id}");
    }


    public void InvitePlayer(string lobbyId, string playerName)
    {
        var lobby = FindLobby(lobbyId);
        if (lobby == null) return;
        _ircBotService.SendMessage(lobbyId, $"!mp invite {SanitizeUsername(playerName)}");
    }

    public void ParseBanchoBotMessage(string lobbyId, string message)
    {
        message = message.StartsWith(':') ? message[1..] : message;
        Log($"Received: {message}");
        var lobby = FindLobby(lobbyId);
        switch (message)
        {
            case "All players are ready":
                Log($"All players are ready in {lobbyId}");
                lobby?.ReadySignal?.TrySetResult(true);
                break;
            case "The match has finished!":
                lobby?.FinishedSignal?.TrySetResult(true);
                break;
        }
    }
    
    

    private BanchoLobby? FindLobby(string lobbyId)
    {
        var lobby = _lobbies
            .FirstOrDefault(l => l.IrcMatchId == lobbyId);
        return lobby;
    }

    private static PoolerMap? FindMap(BanchoLobby lobby, string mapId)
    {
        return int.TryParse(mapId, out var beatmapId)
            ? lobby.MapPool.FirstOrDefault(m => m.BeatmapId == beatmapId)
            : null;
    }

    private static IEnumerable<string> GetModTokens(MapMods mods) => mods switch
    {
        MapMods.NM => [],
        MapMods.HD => ["HD"],
        MapMods.HR => ["HR"],
        MapMods.HDHR => ["HD", "HR"],
        MapMods.EZ => ["EZ"],
        MapMods.EZHD => ["EZ", "HD"],
        _ => [],
    };


    private static int[] RoleCounts(int lobbySize)
    {
        int[] result = [0, 0, 0, 0, 0, 0];

        var rng = new Random();

        switch (lobbySize)
        {
            case 5:
                result = [1, 1, 0, 0, 0, 2];
                result[2] += rng.Next(2); // +1 tambasan/oyuncu
                break;
            case 6:
                result = [1, 1, 0, 0, 1, 2];
                result[2] += rng.Next(2); // +1 tambasan/oyuncu
                break;
            case 7:
                result = [1, 1, 0, 0, 1, 2];
                result[0] += rng.Next(2); // +1 havuzcu/oyuncu
                result[2] += rng.Next(2); // +1 tambasan/oyuncu
                break;
            case 8:
                result = [1, 1, 0, 1, 1, 2];
                result[0] += rng.Next(2); // +1 havuzcu/oyuncu
                result[2] += rng.Next(2); // +1 tambasan/oyuncu
                break;
            case 9:
                result = [1, 1, 1, 1, 1, 2];
                result[0] += rng.Next(2); // +1 havuzcu/oyuncu
                result[2] += rng.Next(2); // +1 tambasan/oyuncu
                break;
            case 10:
                result = [1, 1, 1, 1, 1, 3];
                result[0] += rng.Next(2); // +1 havuzcu/oyuncu
                result[2] += rng.Next(2); // +1 tambasan/oyuncu
                break;
            case 11:
                result = [2, 1, 1, 1, 1, 3];
                result[0] += rng.Next(2); // +1 havuzcu/oyuncu
                result[2] += rng.Next(2); // +1 tambasan/oyuncu
                break;
            case 12:
                result = [2, 1, 1, 1, 1, 4];
                result[0] += rng.Next(2); // +1 havuzcu/oyuncu
                result[2] += rng.Next(2); // +1 tambasan/oyuncu
                break;
            case 13:
                result = [2, 1, 2, 1, 1, 3];
                result[0] += rng.Next(2); // +1 havuzcu/oyuncu
                result[1] += rng.Next(2); // +1 okurgeçer/oyuncu
                result[2] += rng.Next(2); // +1 tambasan/oyuncu
                break;
            case 14:
                result = [3, 1, 2, 1, 1, 2];
                result[0] += rng.Next(2); // +1 havuzcu/oyuncu
                result[1] += rng.Next(2); // +1 okurgeçer/oyuncu
                result[2] += rng.Next(2); // +1 tambasan/oyuncu
                result[3] += rng.Next(2); // +1 modötesi/oyuncu
                break;
            case 15:
                result = [3, 1, 2, 1, 1, 3];
                result[0] += rng.Next(2); // +1 havuzcu/oyuncu
                result[1] += rng.Next(2); // +1 okurgeçer/oyuncu
                result[2] += rng.Next(2); // +1 tambasan/oyuncu
                result[3] += rng.Next(2); // +1 modötesi/oyuncu
                break;
            case 16:
                result = [3, 1, 2, 1, 1, 4];
                result[0] += rng.Next(2); // +1 havuzcu/oyuncu
                result[1] += rng.Next(2); // +1 okurgeçer/oyuncu
                result[2] += rng.Next(2); // +1 tambasan/oyuncu
                result[3] += rng.Next(2); // +1 modötesi/oyuncu
                break;
        }

        result[5] += lobbySize - result.Sum();

        return result;
    }
    
    
    
    
    private static void Log(params object?[] parameters)
    {
        var now = DateTime.Now;
        var msgLine = $"{now} // [BANCHOLOBBYSERVICE] ";
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

    private static string SanitizeUsername(string username) => username.Replace(" ", "_");
    
}

