using System;

using Meebey.SmartIrc4net;

namespace IRCRelay
{
    public class IRC
    {
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
            Program.IRC.OnConnected += new EventHandler(IRCRelay.IRC.OnConnected);

            int port;
            int.TryParse(Config.Config.Instance.IRCPort, out port);

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
                Program.IRC.Login("r", "Discord - IRC Relay"); // todo: make this configurable in settings.xml

                Program.IRC.RfcJoin(channel);

                Program.IRC.Listen();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        public static void OnConnected(object sender, EventArgs e)
        {
            Program.IRC.SendMessage(SendType.Message, "AuthServ@Services.Gamesurge.net", Config.Config.Instance.AuthString);
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

            Helpers.SendMessageAllToTarget(Config.Config.Instance.DiscordGuildName, "<" + e.Data.Nick + "> " + e.Data.Message, Config.Config.Instance.DiscordChannelName);
        }
    }
}
