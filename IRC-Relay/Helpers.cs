using System.Net;
using System.Linq;
using Newtonsoft.Json.Linq;

using System.Text.RegularExpressions;
using System.Collections.Generic;

using Discord.WebSocket;

namespace IRCRelay
{
    class Helpers
    {
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

        public static string MentionToUsername(string input, SocketUserMessage message)
        {
            string returnString = message.Content;

            Regex regex = new Regex("<@!?[0-9]+>");
            Match match = regex.Match(input);
            if (match.Success) // contains a mention
            {
                string substring = input.Substring(match.Index, match.Length);

                SocketUser user = message.MentionedUsers.First();


                returnString = input.Replace(substring, user.Username);
            }

            return returnString;
        }

        public static string ChannelMentionToName(string input, SocketUserMessage message)
        {
            string returnString = input;

            Regex regex = new Regex("<#[0-9]+>");
            Match match = regex.Match(input);
            if (match.Success) // contains a mention
            {
                string substring = input.Substring(match.Index, match.Length);

                var chan = message.MentionedChannels.First();


                returnString = input.Replace(substring, "#" + chan.Name);
            }

            return returnString;
        }

        // Converts <:emoji:23598052306> to :emoji:
        public static string EmojiToName(string input, SocketUserMessage message)
        {
            string returnString = input;

            Regex regex = new Regex("<:[A-Za-z0-9]+:[0-9]+>");
            Match match = regex.Match(input);
            if (match.Success) // contains a mention
            {
                string substring = input.Substring(match.Index, match.Length);
                string[] sections = substring.Split(':');

                returnString = input.Replace(substring, ":" + sections[1] + ":");
            }

            return returnString;
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
                        channel.SendMessageAsync(message);
                    }
                }
            }
        }

        public static SocketTextChannel FindChannel(SocketGuild guild, string text)
        {
            foreach (SocketTextChannel channel in guild.TextChannels)
            {
                if (channel.Name.Contains(text))
                {
                    return channel;
                }
            }

            return null;
        }
    }
}
