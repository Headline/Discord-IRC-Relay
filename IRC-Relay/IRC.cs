using System;
using System.Linq;

using Meebey.SmartIrc4net;
using System.Collections;
using System.Threading;

namespace IRCRelay
{
    public class IRC
    {
        public static readonly string operatorPrefix = "@";
        public static readonly string voicePrefix = "+";

        public static void SpawnBot()
        {
            Program.IRC.Encoding = System.Text.Encoding.UTF8;
            Program.IRC.SendDelay = 200;

            Program.IRC.ActiveChannelSyncing = true;

            Program.IRC.AutoRetry = true;
            Program.IRC.AutoRejoin = true;
            Program.IRC.AutoRelogin = true;
            Program.IRC.AutoRejoinOnKick = true;

            Program.IRC.OnError += new Meebey.SmartIrc4net.ErrorEventHandler(IRCRelay.IRC.OnError);
            Program.IRC.OnChannelMessage += new IrcEventHandler(IRCRelay.IRC.OnChannelMessage);
            
            int.TryParse(Config.Config.Instance.IRCPort, out int port);

            string channel = Config.Config.Instance.IRCChannel;

            try
            {
                Program.IRC.Connect(Config.Config.Instance.IRCServer, port);
            }
            catch (ConnectionException e)
            {
                System.Console.WriteLine("couldn't connect! Reason: " + e.Message);
                return;
            }

            try
            {
                Program.IRC.Login("r", "discord-relay"); // todo: make this configurable in settings.xml

                Program.IRC.SendMessage(SendType.Message, "authserv@services.gamesurge.net", Config.Config.Instance.AuthString);
                Thread.Sleep(1000);

                Program.IRC.RfcJoin(channel);

                Program.IRC.Listen();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static void OnError(object sender, Meebey.SmartIrc4net.ErrorEventArgs e)
        {
            Console.WriteLine("Error: " + e.ErrorMessage);
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


            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("IRC -> Discord <" + e.Data.Nick + ">: " + e.Data.Message);
            Console.ForegroundColor = ConsoleColor.White;


            Helpers.SendMessageAllToTarget(Config.Config.Instance.DiscordGuildName, "**<" + e.Data.Nick + ">** " + e.Data.Message, Config.Config.Instance.DiscordChannelName);
        }
    }
}
