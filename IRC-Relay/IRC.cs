using System;

using Meebey.SmartIrc4net;
using System.Threading;
using System.Timers;
using IRCRelay.Logs;

namespace IRCRelay
{
    public class IRC
    {
		private IrcClient ircClient;
		private System.Timers.Timer timer = null;

        private string server;
        private int port;
        private string nick;
        private string channel;
        private string loginName;
        private string authstring;
        private string authuser;

		public IRC(string server, int port, string nick, string channel, string loginName, string authstring, string authuser)
        {
            ircClient = new IrcClient();

			ircClient.Encoding = System.Text.Encoding.UTF8;
			ircClient.SendDelay = 200;

			ircClient.ActiveChannelSyncing = true;

			ircClient.AutoRetry = true;
			ircClient.AutoRejoin = true;
			ircClient.AutoRelogin = true;
			ircClient.AutoRejoinOnKick = true;

			ircClient.OnError += new Meebey.SmartIrc4net.ErrorEventHandler(IRCRelay.IRC.OnError);
			ircClient.OnChannelMessage += new IrcEventHandler(IRCRelay.IRC.OnChannelMessage);
			ircClient.OnDisconnected += new EventHandler(IRC.OnDisconnected);

			timer = new System.Timers.Timer();

			timer.Elapsed += Timer_Callback;

			timer.Enabled = true;
			timer.AutoReset = true;
            timer.Interval = TimeSpan.FromSeconds(30.0).TotalMilliseconds;

            /* Connection Info */
			this.server = server;
			this.port = port;
			this.nick = nick;
			this.channel = channel;
			this.loginName = loginName;
			this.authstring = authstring;
            this.authuser = authuser;
		}

        public void SendMessage(string message)
        {
            ircClient.SendMessage(SendType.Message, Config.Config.Instance.IRCChannel, message);
		}

        public void SpawnBot()
        {
			new Thread(() =>
			{
				try
				{
					ircClient.Connect(server, port);

                    ircClient.Login(nick, loginName);

					if (Config.Config.Instance.IRCAuthString.Length != 0)
					{
                        ircClient.SendMessage(SendType.Message, authuser, authstring);

						Thread.Sleep(1000); // login delay
					}

					ircClient.RfcJoin(channel);

					ircClient.Listen();

					timer.Start();
                }
				catch (Exception ex)
				{
					Console.WriteLine(ex.Message);
                    return;
				}

			}).Start();
        }

        private void Timer_Callback(Object source, ElapsedEventArgs e)
        {
            if (ircClient.IsConnected)
            {
                return;
            }

            Console.WriteLine("Bot disconnected! Retrying...");
            this.SpawnBot();
        }

        private static void OnDisconnected(object sender, EventArgs e)
        {
            Console.WriteLine("Disconnecting");
        }

        private static void OnError(object sender, Meebey.SmartIrc4net.ErrorEventArgs e)
        {
            Console.WriteLine("Error: " + e.ErrorMessage);
            Environment.Exit(0);
        }

        private static void OnChannelMessage(object sender, IrcEventArgs e)
        {
            if (e.Data.Message.StartsWith("!", StringComparison.CurrentCulture) || e.Data.Nick.Contains("idle"))
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
