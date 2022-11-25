using System;
namespace DiceMastersDiscordBot.Entities
{
    public class TeamListCharacterStats
    {
        public TeamListCharacterStats()
        {
        }

        public CardInfo Card { get; set; }
        public int TotalCount { get; set; }

        public string SummaryOutput()
        {
            if(string.IsNullOrEmpty(Card.FullCardInfo.CardTitle)) 
                return Card.TeamBuilderId.ToString();
            else
                return $"{Card.FullCardInfo?.RarityAbbreviation} {Card.FullCardInfo?.CardTitle}";
        }
    }
}
