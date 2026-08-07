using PoolerBotCS.Models;

namespace PoolerBotCS.Services;

public class BanchoPlayerService : IBanchoPlayerService
{
    
    public BanchoPlayerService(){
    }


    public PoolerPlayer GetOrCreatePlayerByName(string playerName)
    {
        return new PoolerPlayer {
            Username = playerName,
            CurrentRole = Role.Player,
        };
        
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
