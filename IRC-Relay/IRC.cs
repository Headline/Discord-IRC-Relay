using System;
using System.Linq;

using Meebey.SmartIrc4net;
using System.Collections;
using System.Threading;
using System.Timers;
using IRCRelay.Logs;

namespace IRCRelay
{
    public class IRC
    {
        public static System.Timers.Timer timer = null;

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
            Program.IRC.OnDisconnected += new EventHandler(IRC.OnDisconnected);

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
                Program.IRC.Login(Config.Config.Instance.IRCNick, Config.Config.Instance.IRCLoginName);

                if (Config.Config.Instance.IRCAuthString.Length != 0)
                {
                    Program.IRC.SendMessage(SendType.Message, Config.Config.Instance.IRCAuthUser, Config.Config.Instance.IRCAuthString);

                    Thread.Sleep(1000); // login delay
                }

                Program.IRC.RfcJoin(channel);

                Program.IRC.Listen();

                timer = new System.Timers.Timer();

                timer.Elapsed += Timer_Callback;

                timer.Enabled = true;
                timer.AutoReset = true;
                timer.Interval = TimeSpan.FromSeconds(30.0).TotalMilliseconds;
                timer.Start();

            
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void Timer_Callback(Object source, ElapsedEventArgs e)
        {
            if (Program.IRC.IsConnected)
            {
                return;
            }

            Console.WriteLine("Bot disconnected! Retrying...");
            IRC.SpawnBot();
        }

        public static void OnDisconnected(object sender, EventArgs e)
        {
            Console.WriteLine("Disconnecting");
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

            if (Config.Config.Instance.IRCLogMessages)
                LogManager.WriteLog(MsgSendType.IRCToDiscord, e.Data.Nick, e.Data.Message, "log.txt");

            Helpers.SendMessageAllToTarget(Config.Config.Instance.DiscordGuildName, "**<" + e.Data.Nick + ">** " + e.Data.Message, Config.Config.Instance.DiscordChannelName);
        }
    }
}
