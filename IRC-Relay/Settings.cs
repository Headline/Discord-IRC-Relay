using System.IO;
using System.Xml.Serialization;

namespace IRCRelay.Config
{
	public class Config
	{
		// ---------------------------------------------------------------------------------------
		// Insert configurable values below:
		public string IRCServer;
		public string IRCPort;
        public string IRCChannel;
        public string IRCNick;

        public bool IRCLogMessages;

        public string IRCAuthUser;
        public string IRCLoginName;
        public string IRCAuthString;

        public string DiscordBotToken;
        public string DiscordGuildName;
        public string DiscordChannelName;
		// ---------------------------------------------------------------------------------------

		// Name of configuration file.
		[XmlIgnore]
		public const string FileName = "Settings.xml";

		// Globally accessable instance of loaded configuration.
		[XmlIgnore]
		public static Config Instance { get; private set; }

		// Empty constructor for XmlSerializer.
		public Config()
		{
		}

		// Used to load the default configuration if Load() fails.
		public static void Default()
        {
            Config.Instance = new Config()
            {
                IRCServer = "server",
                IRCPort = "port",
                IRCChannel = "#channel",
                IRCNick = "nick",

                IRCLoginName = "auth login name",
                IRCAuthString = "some command here",
                IRCAuthUser = "some user",

                DiscordBotToken = "token",
                DiscordGuildName = "server name",
                DiscordChannelName = "text channel"
            };
        }

        // Loads the configuration from file.
        public static void Load()
		{
			var serializer = new XmlSerializer(typeof(Config));

			using (var fStream = new FileStream(Config.FileName, FileMode.Open))
				Config.Instance = (Config)serializer.Deserialize(fStream);
		}

		// Saves the configuration to file.
		public void Save()
		{
			var serializer = new XmlSerializer(typeof(Config));

			using (var fStream = new FileStream(Config.FileName, FileMode.Create))
				serializer.Serialize(fStream, this);
		}
	}
}