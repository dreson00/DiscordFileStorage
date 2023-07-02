using DSharpPlus;
using Newtonsoft.Json;
using System.Data;
using System.Threading.Channels;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordBot
{
    public class DiscordBot
    {
        public DiscordClient Discord { get; private set; }

        public DiscordBot(IConfiguration config)
        {
            var botConfig = new BotConfig(config.GetSection("DiscordBot")["Token"]!, config.GetSection("DiscordBot")["Prefix"]!);
            var discordConfiguration = new DiscordConfiguration()
            {
                Token = botConfig.Token,
                TokenType = TokenType.Bot
            };

            Discord = new DiscordClient(discordConfiguration);
            //var services = new ServiceCollection().BuildServiceProvider();

            //var commandConfig = new CommandsNextConfiguration()
            //{
            //    Services = services,
            //    StringPrefixes = new[] { botConfig.Prefix },
            //    CaseSensitive = false
            //};

            //var commands = Discord.UseCommandsNext(commandConfig);
            //commands.RegisterCommands<VoiceChatAudioCommandModule>();
        }

        public async Task StartBot()
        {
            await Discord.ConnectAsync();

            await Task.Delay(-1);
        }

        public async void SendLink(ulong serverId, ulong channelId, string videoLink, string userName)
        {
            var msg = await new DiscordMessageBuilder()
                .WithContent($"{userName}:\n" + videoLink)
                .SendAsync(Discord
                    .GetGuildAsync(serverId).Result
                    .GetChannelsAsync().Result
                    .FirstOrDefault(channel => channel.Id == channelId));
        }
    }
}