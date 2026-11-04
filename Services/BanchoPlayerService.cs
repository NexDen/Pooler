using PoolerBotCS.Models;

namespace PoolerBotCS.Services;

public class BanchoPlayerService : IBanchoPlayerService
{
    
    private readonly PoolerDbContext _db;

    public BanchoPlayerService(PoolerDbContext db){
        _db = db;
    }

    
    


    public BanchoPlayer GetOrCreatePlayerByName(string playerName)
    {
        var player = _db.BanchoPlayers.FirstOrDefault(p => p.Username == playerName);
        if (player != null) return player;
        player = new BanchoPlayer {
            Username = playerName,
        };
        _db.BanchoPlayers.Add(player);
        _db.SaveChanges();
        return player;
    }
    

    public bool TryVerifyPlayer(string playerName, int verificationCode)
    {
        var player = GetOrCreatePlayerByName(playerName);
        var matchingVerification = _db.Verifications.FirstOrDefault(v => v.DiscordUserId == player.DiscordUserId);
        if (verificationCode != matchingVerification?.Code) return false;
        player.IsVerified = true;
        return true;
    }

    private static void Log(params object?[] parameters)
    {
        var now = DateTime.Now;
        var msgLine = $"{now} // [BANCHOPLAYERSERVICE] ";
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
    
}
