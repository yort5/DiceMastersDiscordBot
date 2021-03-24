using System;
namespace DiceMastersDiscordBot.Entities
{
    public class YouTubeSubscription
    {
        public YouTubeSubscription()
        {
        }

        public string ChannelName { get; set; }
        public string ChannelId { get; set; }
        public DateTime DateLastChecked { get; set; }
    }
}
