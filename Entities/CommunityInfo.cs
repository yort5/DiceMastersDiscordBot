using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DiceMastersDiscordBot.Entities
{
    public class CommunityInfo
    {
        public List<CommunityCardInfo> Cards;
        public List<SetInfo> Sets;

        internal CommunityCardInfo GetCardFromTraits(string teamBuilderCode, string setCode, string cardName, string rarity, StringComparer comparer, ILogger logger)
        {
            var formattedTeamBuilderCode = CommunityCardInfo.GetFormattedTeamBuilderCode(teamBuilderCode);
            var teamBuilderMatch = this.Cards.Where(c => comparer.Equals(formattedTeamBuilderCode, c.TeamBuilderCode));
            if (teamBuilderMatch.Any())
            {
                return teamBuilderMatch.FirstOrDefault();
            }

            if (setCode == "OP") setCode = "PROMO";

            var findMatchingCharacters = this.Cards.Where(c => comparer.Equals(cardName, c.CardTitle));

            // if we didn't find a straight-up match, try without special characters. i.e., "Spider-man" vs "Spiderman"
            if (!findMatchingCharacters.Any())
            {
                findMatchingCharacters = this.Cards.Where(c => comparer.Equals(Regex.Replace(cardName, @"[^\w\d]", ""), Regex.Replace(c.CardTitle, @"[^\w\d]", "")));
            }

            // if we STILL haven't found a match, try a "contains". This can produce false positives, but will catch when extra text has been
            // added, like "Magneto (no die)".
            if (!findMatchingCharacters.Any())
            {
                findMatchingCharacters = this.Cards.Where(c => cardName.ToLower().Contains(c.CardTitle.ToLower()));
            }

            // first try to match on set code
            var setInfo = this.Sets.FirstOrDefault(s => s.SetCode.ToLower().Contains(setCode.ToLower()));
            // If that doesn't match, try to match on the full name
            if (setInfo == null)
            {
                setInfo = this.Sets.FirstOrDefault(s => s.SetName.ToLower().Contains(setCode.ToLower()));
            }
            // If THAT doesn't work, try without special characters, i.e., "Avengers Vs X-Men" vs "Avengers Vs. X-Men"
            if (setInfo == null)
            {
                setInfo = this.Sets.FirstOrDefault(s => comparer.Equals(Regex.Replace(s.SetName, @"[^\w\d]", ""), Regex.Replace(setCode, @"[^\w\d]", "")));
            }

            if (setInfo == null) return new CommunityCardInfo();

            var matchSetAndRarity = findMatchingCharacters.Where(f => f.SetCode == setInfo.SetCode
                                                                && (comparer.Equals(rarity, f.Rarity) || comparer.Equals(rarity, f.RarityAbbreviation)));

            if (!matchSetAndRarity.Any())
            {
                logger.LogInformation($"Couldn't find match for set = {setCode}, rarity = {rarity}, charcter = {cardName}");
                return new CommunityCardInfo();
            }
            var isThisYourCard = matchSetAndRarity.FirstOrDefault();
            return isThisYourCard;
        }
    }
}
