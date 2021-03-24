using System;
namespace DiceMastersDiscordBot.Entities
{
    public class YouTubeResponse
    {
        public YouTubeResponse()
        {
        }

        public string ChannelName { get; set; }
        public string VideoId { get; set; }
        public string VideoTitle { get; set; }
        public string VideoDescription { get; set; }
        public string VideoLink
        {
            get
            {
                return @"https://www.youtube.com/watch?v=" + VideoId;
            }
         }
    }
}
