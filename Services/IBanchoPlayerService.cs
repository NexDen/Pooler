using PoolerBotCS.Models;

namespace PoolerBotCS.Services;

public interface IBanchoPlayerService
{
    public BanchoPlayer GetOrCreatePlayerByName(string playerName);
    public bool TryVerifyPlayer(string playerName, int verificationCode);
}
