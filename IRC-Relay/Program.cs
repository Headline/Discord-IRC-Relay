using System;
using System.Threading;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;
using Discord.Commands;

using Microsoft.Extensions.DependencyInjection;

using Meebey.SmartIrc4net;
using IRCRelay.Logs;

namespace IRCRelay
{
    class Program
    {
        public DiscordSocketClient client;

        private IRC irc;
        private CommandService commands;
        private IServiceProvider services;

        public static Program Instance;
        
        public static void Main(string[] args)
        {
            Instance = new Program();

            try
            {
                Config.Config.Load();
            }
            catch
            {
                Console.WriteLine("Unable to load config. Ensure Settings.xml is formatted correctly.");
                Config.Config.Default();
                Config.Config.Instance.Save();
                return;
            }

            Instance.MainAsync().GetAwaiter().GetResult();
        }

        private async Task MainAsync()
        {
            client = new DiscordSocketClient();
            commands = new CommandService();

            client.Log += Log;

            services = new ServiceCollection().BuildServiceProvider();

            client.MessageReceived += OnDiscordMessage;

            await client.LoginAsync(TokenType.Bot, Config.Config.Instance.DiscordBotToken);
            await client.StartAsync();

            // TODO IRC Connect here
            int.TryParse(Config.Config.Instance.IRCPort, out int port);
            irc = new IRC(Config.Config.Instance.IRCServer,
                          port,
                          Config.Config.Instance.IRCNick,
                          Config.Config.Instance.IRCChannel,
                          Config.Config.Instance.IRCLoginName,
                          Config.Config.Instance.IRCAuthString,
                          Config.Config.Instance.IRCAuthUser);

            irc.SpawnBot();

            await Task.Delay(-1);
        }

        public async Task OnDiscordMessage(SocketMessage messageParam)
        {
            string url = "";
            var message = messageParam as SocketUserMessage;
            if (message == null) return;

            int argPos = 0;

            if (message.HasCharPrefix('!', ref argPos)) return;

            if (!messageParam.Channel.Name.Contains(Config.Config.Instance.DiscordChannelName)) return;
            if (messageParam.Author.IsBot) return;

            /* Santize discord-specific notation to human readable things */
            string formatted = Helpers.MentionToUsername(messageParam.Content, message);
            formatted = Helpers.EmojiToName(formatted, message);
            formatted = Helpers.ChannelMentionToName(formatted, message);

            string text = "```";
            if (formatted.Contains(text))
            {
                int start = formatted.IndexOf(text, StringComparison.CurrentCulture);
                int end = formatted.IndexOf(text, start + text.Length, StringComparison.CurrentCulture);

                string code = formatted.Substring(start + text.Length, (end - start) - text.Length);

                url = Helpers.UploadMarkDown(code);

                formatted = formatted.Remove(start, (end - start) + text.Length);
            }

            // Send IRC Message
            if (formatted.Length > 1000)
            {
                await messageParam.Channel.SendMessageAsync("Error: messages > 1000 characters cannot be sent!");
                return;
            }

            if (formatted.Replace(" ", "").Replace("\n", "").Length != 0) // if the string is not empty or just spaces
            {
                if (Config.Config.Instance.IRCLogMessages)
                    LogManager.WriteLog(MsgSendType.DiscordToIRC, messageParam.Author.Username, formatted, "log.txt");

                irc.SendMessage("<" + messageParam.Author.Username + "> " + formatted);
            }

            if (!url.Equals(""))
            {
                if (Config.Config.Instance.IRCLogMessages)
                    LogManager.WriteLog(MsgSendType.DiscordToIRC, messageParam.Author.Username, url, "log.txt");

                irc.SendMessage("<" + messageParam.Author.Username + "> " + url);
            }
        }

        public Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return null;
        }
    }
}
