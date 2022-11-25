using System;
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
    }
}
