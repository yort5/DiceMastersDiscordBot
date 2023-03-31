namespace DiceMastersDiscordBot.Entities
{
    public class TradeInfo
    {
        public CommunityCardInfo CardInfo { get; set; }
        public bool NonFoil { get; set; }
        public bool Foil { get; set; }
        public bool SellOrBuy { get; set; }
        public bool Trade { get; set; }
        public string Promo { get; set; }
        public string DiscordUsername { get; set; }
    }
}
