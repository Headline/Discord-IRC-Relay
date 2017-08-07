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

using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using System.Text;

using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

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

                IRC.OnError += new Meebey.SmartIrc4net.ErrorEventHandler(OnError);
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

        public static void OnError(object sender, Meebey.SmartIrc4net.ErrorEventArgs e)
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
            string url = "";
            var message = messageParam as SocketUserMessage;
            if (message == null) return;

            int argPos = 0;

            if (message.HasCharPrefix('!', ref argPos)) return;

            if (!messageParam.Channel.Name.Contains(Config.Config.Instance.DiscordChannelName)) return;
            if (messageParam.Author.IsBot) return;

            string formatted = MentionToUsername(messageParam.Content, message);

            Console.WriteLine("Trying to send: " + formatted);

            string text = "```";
            if (formatted.Contains(text))
            {
                int start = formatted.IndexOf(text);
                int end = formatted.IndexOf(text, start + text.Length);
                Console.WriteLine("Starting index: {0} | Ending index: {1}", start, end);

                string code = formatted.Substring(start+text.Length, (end - start) - text.Length);
                Console.WriteLine("Code value: {0}", code);

                url = UploadMarkDown(code);
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

        public static string UploadMarkDown(string input)
        {

            using (var client = new WebClient())
            {
                client.Headers[HttpRequestHeader.ContentType] = "text/plain";
                
                var response = client.UploadString("https://hastebin.com/documents", input);
                JObject obj = JObject.Parse(response);

                if (!obj.HasValues)
                {
                    return "";
                }

                string key = (string)obj["key"];
                string hasteUrl = "https://hastebin.com/" + key + ".cs";
                
                return hasteUrl;
            }
        }

        public string MentionToUsername(string input, SocketUserMessage message)
        {
            string returnString = message.Content;

            Regex regex = new Regex("<@[0-9]+>");
            Match match = regex.Match(input);
            if (match.Success) // contains a mention
            {
                string substring = input.Substring(match.Index, match.Length);

                SocketUser user = message.MentionedUsers.First();
                

                returnString = message.Content.Replace(substring, user.Username + ":");
            }

            return returnString;
        }

        public Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return null;
        }
    }
}
