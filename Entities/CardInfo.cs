using System;
namespace DiceMastersDiscordBot.Entities
{
    public class CardInfo
    {
        public CardInfo()
        {
        }

        public string TeamBuilderId { get; set; }
        public string CardTitle { get; set; }
        public string CardSubtitle { get; set; }
        public string PurchaseCost { get; set; }
        public int DiceCount { get; set; }
        public int MaxDice { get; set; }
    }
}
