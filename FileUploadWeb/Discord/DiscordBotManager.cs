using DSharpPlus.Entities;

namespace FileUploadWeb.Discord
{
    public class DiscordBotManager
    {
        private readonly ILogger<DiscordBotManager> _logger;
        private readonly DiscordBot.DiscordBot _discordBot;
        private readonly string _guild;

        public DiscordBotManager(
            ILogger<DiscordBotManager> logger,
            DiscordBot.DiscordBot discordBot,
            IConfiguration config)
        {
            _logger = logger;
            _discordBot = discordBot;
            _guild = config.GetSection("Authentication").GetValue<string>("AllowedServerId");
        }

        public void SendLink(string channelId, string url, string userName)
        {
            _discordBot.SendLink(
                ulong.Parse(_guild),
                ulong.Parse(channelId),
                url,
                userName);
        }

        public IReadOnlyList<DiscordChannel> GetChannels()
        {
            return _discordBot.Discord.GetGuildAsync(ulong.Parse(_guild)).Result.GetChannelsAsync().Result;
        }

    }
}
