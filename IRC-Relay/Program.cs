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
        private CommandService commands;
        private IServiceProvider services;

        public static IrcClient IRC;
        public static Program Instance;
        
        public static void Main(string[] args)
        {
            Instance = new Program();
            IRC = new IrcClient();
            
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
            

            /* Run IRC Bot */
            new Thread(() => 
            {
                IRCRelay.IRC.SpawnBot();


            }).Start();

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
                int start = formatted.IndexOf(text);
                int end = formatted.IndexOf(text, start + text.Length);

                string code = formatted.Substring(start + text.Length, (end - start) - text.Length);

                url = Helpers.UploadMarkDown(code);

                formatted = formatted.Remove(start, (end - start) + text.Length);
            }

            // Send IRC Message
            if (messageParam.Content.Length > 1000)
            {
                await messageParam.Channel.SendMessageAsync("Error: messages > 1000 characters cannot be sent!");
                return;
            }

            if (formatted.Replace(" ", "").Replace("\n", "").Length != 0) // if the string is not empty or just spaces
            {
                if (Config.Config.Instance.IRCLogMessages)
                    LogManager.WriteLog(MsgSendType.DiscordToIRC, messageParam.Author.Username, formatted, "log.txt");

                Program.IRC.SendMessage(SendType.Message, Config.Config.Instance.IRCChannel, "<" + messageParam.Author.Username + "> " + formatted);
            }

            if (!url.Equals(""))
            {
                if (Config.Config.Instance.IRCLogMessages)
                    LogManager.WriteLog(MsgSendType.DiscordToIRC, messageParam.Author.Username, url, "log.txt");

                Program.IRC.SendMessage(SendType.Message, Config.Config.Instance.IRCChannel, "<" + messageParam.Author.Username + "> " + url);
            }
        }

        public Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return null;
        }
    }
}
