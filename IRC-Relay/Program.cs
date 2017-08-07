using System;
using System.Threading;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;
using Discord.Commands;

using Microsoft.Extensions.DependencyInjection;

using Meebey.SmartIrc4net;


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

            string formatted = Helpers.MentionToUsername(messageParam.Content, message);

            Console.WriteLine("Trying to send: " + formatted);

            string text = "```";
            if (formatted.Contains(text))
            {
                int start = formatted.IndexOf(text);
                int end = formatted.IndexOf(text, start + text.Length);
                Console.WriteLine("Starting index: {0} | Ending index: {1}", start, end);

                string code = formatted.Substring(start+text.Length, (end - start) - text.Length);
                Console.WriteLine("Code value: {0}", code);

                url = Helpers.UploadMarkDown(code);
                Console.WriteLine("URL: {0}", url);

                formatted = formatted.Remove(start, ( end - start ) + text.Length);
                Console.WriteLine("Formatted: {0}", formatted);
            }

            // Send IRC Message
            if (messageParam.Content.Length > 500)
            {
                await messageParam.Channel.SendMessageAsync("Error: messages > 500 characters cannot be sent!");
                return;
            }

            Program.IRC.SendMessage(SendType.Message, Config.Config.Instance.IRCChannel, "<" + messageParam.Author.Username + "> " + formatted);
            if (!url.Equals(""))
            {
                Program.IRC.SendMessage(SendType.Message, Config.Config.Instance.IRCChannel, "<" + messageParam.Author.Username + "> " + url);
            }

            Console.WriteLine("Sending discord message....");
        }

        public Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return null;
        }
    }
}
