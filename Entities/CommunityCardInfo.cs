using System;
namespace DiceMastersDiscordBot.Entities
{
    public class CommunityCardInfo
    {
        public CommunityCardInfo()
        {
        }

        public string TeamBuilderId { get; set; } = string.Empty;
        public string CardTitle { get; set; } = string.Empty;
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

        public string RarityAbbreviation 
        { 
            get
            {
                switch(Rarity)
                {
                    case "Common":
                        return "C";
                    case "Uncommon":
                        return "UC";
                    case "Rare":
                        return "R";
                    case "Super":
                        return "SR";
                    default:
                        return Rarity;
                }
            }
        }
    }
}
