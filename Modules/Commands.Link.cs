using Discord.Interactions;
using PoolerBotCS.Models;
using PoolerBotCS.Services;

namespace PoolerBotCS.Modules;

public partial class Commands
{
    [Group("link", "Link your osu! account to your Discord account")]
    public class LinkModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly IBanchoLobbyService _lobbyService;
        private readonly IBanchoPlayerService _playerService;

        private readonly PoolerDbContext _db;

        private readonly IConfiguration _config;

        public LinkModule(IBanchoLobbyService lobbyService, IBanchoPlayerService playerService, PoolerDbContext db, IConfiguration config) {
            _lobbyService = lobbyService;
            _playerService = playerService;
            _db = db;
            _config = config;
        }


        [SlashCommand("start", "Link your osu! account to your Discord account")]
        public async Task LinkOsu([Summary(description: "Your osu! username.")] string username)
        {
            await DeferAsync();

            var discordUserId = Context.User.Id;
            var verificationCode = Random.Shared.Next(100000, 999999);

            var verification = new Verification
            {
                DiscordUserId = discordUserId,
                Code = verificationCode,
                ExpiresAt = DateTimeOffset.Now.AddMinutes(5),
            };
            _db.Verifications.Add(verification);
            await _db.SaveChangesAsync();

            await FollowupAsync($"Verification code: {verificationCode}. Please send it to {_config["Irc:Username"]} via osu! dms then run /link verify!");

        }


        [SlashCommand("verify", "Verify your osu! account")]
        public async Task Verify([Summary(description: "The verification code")] int code)
        {
            await DeferAsync();

            var activeVerification = _db.Verifications
                .FirstOrDefault(v => v.DiscordUserId == Context.User.Id);
            if (activeVerification == null)
            {
                await FollowupAsync("You haven't run /link start yet.");
                return;
            }
            if (activeVerification.ExpiresAt < DateTimeOffset.Now)
            {
                await FollowupAsync("Verification code expired.");
                return;
            }
            if (activeVerification.Code != code)
            {
                await FollowupAsync("Invalid verification code.");
                return;
            }

            var player = _playerService.GetOrCreatePlayerByName(Context.User.Username);
            player.DiscordUserId = Context.User.Id;
            _db.BanchoPlayers.Add(player);
            await _db.SaveChangesAsync();

            await FollowupAsync("Successfully linked your osu! account!");

        }
    }
}
