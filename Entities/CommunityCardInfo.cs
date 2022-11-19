using System;
namespace DiceMastersDiscordBot.Entities
{
    public class CommunityCardInfo
    {
        public CommunityCardInfo()
        {
        }

        public string TeamBuilderId { get; set; }
        public string CardTitle { get; set; }
        public string CardSubtitle { get; set; }
        public string PurchaseCost { get; set; }
        public string EnergyType { get; set; }
        public string Rarity { get; set; }
        public string Affiliation { get; set; }
        public string AbilityText { get; set; }
        public string StatLine { get; set; }
        public string CardImageUrl { get; set; }
        public string DiceImageUrl { get; set; }
        public string Nickname { get; set; }
        public string ImageFolder { get; set; }
        public string CardNumber { get; set; }
    }
}
