namespace PoolerBotCS.Services;

public interface IDiscordBotService
{
    Task SendMessage(ulong guildId, ulong channelId, string message);

    Task SendDirectMessage(ulong userId, string message);

    /// <summary>
    /// Sends a DM to the user and waits for their next DM reply.
    /// Returns null if the user doesn't reply within the timeout.
    /// </summary>
    Task<string?> AskDirectMessage(ulong userId, string question, TimeSpan timeout);
}
