namespace DiceMastersDiscordBot.Entities
{
    public class TradeInfo
    {
        public string TeamBuilderId { get; set; }
        public string CardName { get; set; }
        public bool NonFoil { get; set; }
        public bool Foil { get; set; }
        public bool SellOrBuy { get; set; }
        public bool Trade { get; set; }
        public string DiscordUsername { get; set; }
    }
}
