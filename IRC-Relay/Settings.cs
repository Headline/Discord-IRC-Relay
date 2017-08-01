using System.Collections.Generic;
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
        public string AuthString;

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
			Config.Instance = new Config();

            Config.Instance.IRCServer = "server";
            Config.Instance.IRCPort = "port";
            Config.Instance.IRCChannel = "#channel";
            Config.Instance.AuthString = "some command here";

            Config.Instance.DiscordBotToken = "token";
            Config.Instance.DiscordGuildName = "server name";
            Config.Instance.DiscordChannelName = "text channel";
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