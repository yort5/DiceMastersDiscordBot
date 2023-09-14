using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Text.Json.Serialization;

namespace DiceMastersDiscordBot.Entities
{

    public class LtnSongInfo
    {
        public string name { get; set; }
        public string stationlogo { get; set; }
        public string stationlogodominantcolor { get; set; }
        public string[] genres { get; set; }
        public string website { get; set; }
        public string timezone { get; set; }
        public StreamUrls[] streamurls { get; set; }
        public string streamurl { get; set; }
        public string streamhlsurl { get; set; }
        public string description { get; set; }
        public string facebook { get; set; }
        public string twitter { get; set; }
        public string instagram { get; set; }
        [JsonProperty("current-track")]
        [JsonPropertyName("current-track")]
        public CurrentTrack currenttrack { get; set; }
        public LastPlayed[] lastplayed { get; set; }
        public string mountid { get; set; }
        public string cover { get; set; }
        public bool auto_dj_on { get; set; }
        public bool live_dj_on { get; set; }
        public string active_mount { get; set; }
        public bool is_playing { get; set; }
        public bool station_enabled { get; set; }
        public string slug { get; set; }
        public int listeners { get; set; }
        public string station_type { get; set; }
        public string cachetime { get; set; }
        public string cachehost { get; set; }
    }

    public class CurrentTrack
    {
        public string title { get; set; }
        public string artist { get; set; }
        public string art { get; set; }
        public string start { get; set; }
        public string played { get; set; }
        public string sync_offset { get; set; }
        public float duration { get; set; }
        public string end { get; set; }
        public string source { get; set; }
        public string status { get; set; }
    }

    public class StreamUrls
    {
        public string high_quality { get; set; }
        public string encoding { get; set; }
        public string low_quality { get; set; }
        public string hls { get; set; }
    }

    public class LastPlayed
    {
        public string title { get; set; }
        public string artist { get; set; }
        public string art { get; set; }
        public string start { get; set; }
        public string played { get; set; }
        public string sync_offset { get; set; }
        public string duration { get; set; }
        public string end { get; set; }
    }

}
