namespace PoolerBotCS.Services;

public interface IBanchoLobbyService
{
    public void Make(string name);
    public Task Create(string[] playerNames);
    public void PlayerJoinLobby(string lobbyId, string playerName);
    public void ParseBanchoBotMessage(string lobbyId, string message);

}
