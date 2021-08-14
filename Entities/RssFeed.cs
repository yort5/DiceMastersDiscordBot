using System;
namespace DiceMastersDiscordBot.Entities
{
    public class RssFeed
    {
        public RssFeed()
        {
        }

        public string SiteName { get; set; }
        public string SiteUrl { get; set; }
        public DateTime DateLastChecked { get; set; }
    }
}
