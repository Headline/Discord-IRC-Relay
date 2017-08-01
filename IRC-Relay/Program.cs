using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

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
            

            
            new Thread(() =>
            {
                IRC.Encoding = System.Text.Encoding.UTF8;
                IRC.SendDelay = 200;

                IRC.ActiveChannelSyncing = true;

                IRC.AutoRetry = true;
                IRC.AutoRejoin = true;
                IRC.AutoRelogin = true;
                IRC.AutoRejoinOnKick = true;

                IRC.OnError += new ErrorEventHandler(OnError);
                IRC.OnChannelMessage += new IrcEventHandler(OnChannelMessage);
                IRC.OnConnected += new EventHandler(OnConnected);

                int port;
                int.TryParse(Config.Config.Instance.IRCPort, out port);
            
                string channel = Config.Config.Instance.IRCChannel;

                try
                {
                        IRC.Connect(Config.Config.Instance.IRCServer, port);
                }
                catch (ConnectionException e)
                {
                    System.Console.WriteLine("couldn't connect! Reason: " + e.Message);
                    return;
                }

                try
                {
                    IRC.Login("r", "Discord - IRC Relay"); // todo: make this configurable in settings.xml

                    IRC.RfcJoin(channel);

                    IRC.Listen();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }


            }).Start();

            await Task.Delay(-1);
        }

        public static void OnConnected(object sender, EventArgs e)
        {
            IRC.SendMessage(SendType.Message, "AuthServ@Services.Gamesurge.net", Config.Config.Instance.AuthString);
            //Program.IRC.SendMessage(SendType.Message, Config.Config.Instance.IRCChannel, Config.Config.Instance.IRCCommand);
        }

        public static void OnError(object sender, ErrorEventArgs e)
        {
            System.Console.WriteLine("Error: " + e.ErrorMessage);
            Environment.Exit(0);
        }

        public static void OnChannelMessage(object sender, IrcEventArgs e)
        {
            if (e.Data.Message.StartsWith("!") || e.Data.Nick.Contains("idle"))
            {
                return;
            }

            if (e.Data.Nick.Equals("r"))
            {
                return;
            }

            SendMessageAllToTarget(Config.Config.Instance.DiscordGuildName, "<" + e.Data.Nick + "> " + e.Data.Message, Config.Config.Instance.DiscordChannelName);
        }

        public static void SendMessageAllToTarget(string targetGuild, string message, string targetChannel)
        {
            foreach (SocketGuild guild in Program.Instance.client.Guilds) // loop through each discord guild
            {
                if (guild.Name.ToLower().Contains(targetGuild.ToLower())) // find target 
                {
                    SocketTextChannel channel = FindChannel(guild, targetChannel); // find desired channel

                    if (channel != null) // target exists
                    {
                        Program.Instance.Log(new LogMessage(LogSeverity.Info, "SendMsg", "Sending msg to: " + channel.Name));
                        channel.SendMessageAsync(message);
                    }
                }
            }
        }

        public static SocketTextChannel FindChannel(SocketGuild guild, string text)
        {
            Program.Instance.Log(new LogMessage(LogSeverity.Info, "SendMsg", "Trying to find #"+text+" in: " + guild.Name));

            foreach (SocketTextChannel channel in guild.TextChannels)
            {
                if (channel.Name.Contains(text))
                {
                    return channel;
                }
            }

            return null;
        }

        public async Task OnDiscordMessage(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;
            if (message == null) return;

            int argPos = 0;

            if (message.HasCharPrefix('!', ref argPos)) return;

            if (!messageParam.Channel.Name.Contains("irc")) return;
            if (messageParam.Author.IsBot) return;

            // Send IRC Message
            if (messageParam.Content.Length > 200)
            {
                await messageParam.Channel.SendMessageAsync("Error: messages > 200 characters cannot be sent!");
                return;
            }

            Program.IRC.SendMessage(SendType.Message, Config.Config.Instance.IRCChannel, "<" + messageParam.Author.Username + "> " + messageParam.Content);
        }

        public Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return null;
        }
    }
}
