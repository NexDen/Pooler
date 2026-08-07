using PoolerBotCS.Models;

namespace PoolerBotCS.Services;

public interface IBanchoPlayerService
{
    public PoolerPlayer GetOrCreatePlayerByName(string playerName);
}