using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace DiceMastersDiscordBot.Entities
{
    public class CommunityCardInfo
    {
        public CommunityCardInfo()
        {
        }

        public string TeamBuilderCode { get; set; } = string.Empty;
        public string SetCode { get; set; }
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
        //public string PlaceholderO_14 { get; set; }
        //public string PlaceholderP_15 { get; set; }

        //public string HaveNonFoilToSell { get; set; }
        //public string HaveNonFoilToTrade { get; set; }
        //public string HaveFoilToSell { get; set; }
        //public string HaveFoilToTrade { get; set; }
        //public string WantNonFoilToBuy { get; set; }
        //public string WantNonFoilForTrade { get; set; }
        //public string WantFoilToBuy { get; set; }
        //public string WantFoilForTrade { get; set; }

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
                    case "Super Rare":
                        return "SR";
                    default:
                        return Rarity;
                }
            }
        }

        public static string ParseRarityFromString(string rarityString)
        {
            var rs = rarityString.ToLower();
            if (rs == "common" || rs.Length == 1 && rs == "c")
                return "Common";
            else if (rs == "uncommon" || rs.Length == 2 && rs == "uc")
                return "Uncommon";
            else if (rs == "rare" || rs.Length == 1 && rs == "r")
                return "Rare";
            else if (rs == "super rare" || rs.Length == 2 && rs == "sr")
                return "Super Rare";
            else if (rs == "promo" || rs.Length == 1 && rs == "p")
                return "Promo";

            return rarityString;
        }

        public static string GetFormattedTeamBuilderCode(string teamBuilderString)
        {
            var digits = new string(teamBuilderString.Where(s => char.IsDigit(s)).ToArray());
            var letters = new string(teamBuilderString.Where(s => char.IsLetter(s)).ToArray());

            var teamBuilderCode = $"{letters}{digits.PadLeft(3, '0')}";
            return teamBuilderCode;
        }
    }
}
