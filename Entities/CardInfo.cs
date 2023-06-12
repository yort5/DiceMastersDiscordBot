using System;
using System.Globalization;

namespace DiceMastersDiscordBot.Entities
{
    public class CardInfo
    {
        public CardInfo()
        {
        }

        public string TeamBuilderId { get; set; }
        public int DiceCount { get; set; }
        public CommunityCardInfo FullCardInfo { get; set; } = new CommunityCardInfo();

        public string Global
        {
            get
            {
                var indexOfGlobal = FullCardInfo.AbilityText.IndexOf("Global:");
                return (indexOfGlobal >= 0) ? FullCardInfo.AbilityText.Substring(indexOfGlobal) : string.Empty;
            }
        }
    }
}
