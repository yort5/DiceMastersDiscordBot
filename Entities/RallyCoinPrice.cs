using System;
namespace DiceMastersDiscordBot.Entities
{
    public class RallyCoinPrice
    {
        public string symbol { get; set; }
        public float priceInUSD { get; set; }
        public float priceInRLY { get; set; }
    }
}
