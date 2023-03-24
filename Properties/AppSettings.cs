using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DiceMastersDiscordBot.Properties
{
    public class AppSettings : IAppSettings
    {
        private const string _BotName = "Dice Masters Bot";

        private readonly ILogger _logger;
        private readonly IConfiguration _config;

        private readonly string _DiscordToken;
        private readonly string _TCCDiscordToken;
        private readonly string _GoogleToken;
        private readonly string _ChallongeToken;
        private readonly string _MasterSheetId;
        private readonly string _CommunitySheetId;
        private readonly string _CrimeSheetId;

        public AppSettings(ILoggerFactory loggerFactory, IConfiguration config)
        {
            _logger = loggerFactory.CreateLogger<AppSettings>(); ;
            _config = config ?? throw new ArgumentNullException(nameof(config));

            _DiscordToken = _config["DiscordToken"];
            _TCCDiscordToken = _config["CCDiscordToken"];
            _GoogleToken = _config["GoogleCredentials"];
            _ChallongeToken = _config["ChallongeToken"];

            _MasterSheetId = _config["MasterSheetId"];
            _CommunitySheetId = _config["CommunitySheetId"];
            _CrimeSheetId = _config["CrimeSheetId"];
        }

        public string GetDiscordToken()
        {
            return _DiscordToken;
        }

        public string GetTCCDiscordToken()
        {
            return _TCCDiscordToken;
        }

        public string GetGoogleToken()
        {
            return _GoogleToken;
        }
        public string GetChallongeToken()
        {
            return _ChallongeToken;
        }

        public string GetBotHelpString()
        {
            StringBuilder helpString = new StringBuilder();
            var nl = Environment.NewLine;
            helpString.Append($"{_BotName} currently supports the following commands:");
            helpString.Append($"{nl}WITHIN A CHANNEL:");
            helpString.Append($"{nl}    `.format` - returns the current format for that channel's event. Adding a number after (e.g. `.format 2`) will return upcoming event formats for that channel.");
            helpString.Append($"{nl}    `.submit http://tb.dicecoalition.com/yourteam` - submits your team for the event. Your link will be immediately deleted so others can't see it.");
            helpString.Append($"{nl}    `.list` - lists the current people signed up for the event.");
            helpString.Append($"{nl}    `.here` - marks a person as HERE in the spreadsheet for the event (only use at the time of the event).");
            helpString.Append($"{nl}    `.drop` - marks a person as DROPPED in spreadsheet for the event.");
            helpString.Append($"{nl}    `.report` @winnerDiscord over @loserDiscord 2-1 - reports scores for an event configured with Challonge integration.");
            //helpString.Append($"{nl}VIA DIRECT MESSAGE - you can also send the {_BotName} a direct message");
            //helpString.Append($"{nl}    .submit <event> <teambuilder link> - current regular events are: wda (Weekly Dice Arena), df (Dice Fight), totm (Team of the Month)");
            //// TODO helpString.Append($"{nl}                                       - current standa-alone: {GetOneOffCode()}");
            //helpString.Append($"{nl}Example: `.submit wda http://tb.dicecoalition.com/blahblah`");
            helpString.Append($"{nl}If you have any problems or just general feedback, please DM Yort.");
            return helpString.ToString();
        }

        public string GetBotHelpMoreString()
        {
            StringBuilder helpString = new StringBuilder();
            var nl = Environment.NewLine;
            helpString.Append($"{_BotName} additional commands:");
            helpString.Append($"{nl}    `.teams` - returns the list of teams registered for the event. You can only do this if you are authorized");
            helpString.Append($"{nl}    `.fellowship` - this submits your vote for fellowship to the tournament organizer");
            helpString.Append($"{nl}    `.win` - associates your WIN username with your Discord username");
            helpString.Append($"{nl}    `.challonge` - associates your Challonge username with your Discord username");
            helpString.Append($"{nl}If you have any problems or just general feedback, please DM Yort.");
            return helpString.ToString();
        }

        public string GetBotName()
        {
            return _BotName;
        }

        public string GetColumnSpan()
        {
            return "A:E";
        }

        public string GetMasterSheetId()
        {
            return _MasterSheetId;
        }

        public string GetCommunitySheetId()
        {
            return _CommunitySheetId;
        }

        public string GetTCCValueTrackerApiKey()
        {
            return _config["TCCValueTrackerApi"];
        }

        public string GetCrimeSheetId()
        {
            return _CrimeSheetId;
        }

        public ulong GetScoresChannelId()
        {
            ulong channelId = 0;
            ulong.TryParse(_config["ScoresChannelId"], out channelId);
            return channelId;
        }

        public List<ulong> GetDiceMastersMediaChannelIds()
        {
            string channelList = _config["DiceMastersMediaChannelIds"];
            return ParseIdsFromString(channelList);
        }

        public List<ulong> GetNonDiceMastersMediaChannelIds()
        {
            string channelList = _config["NonDiceMastersMediaChannelIds"];
            return ParseIdsFromString(channelList);
        }

        public string GetHackExceptionUser()
        {
            return _config["OneOffTODiscordID"];
        }

        public List<ulong> GetServersForSlashCommand(string slashCommandName)
        {
            var serverList = _config[$"SlashCommandFlag:{slashCommandName}"];
            return ParseIdsFromString(serverList);
        }

        public ulong GetCierraNotificationId()
        {
            ulong.TryParse(_config["CierraNotificationId"], out ulong cierraId);
            return cierraId;
        }
        public List<ulong> GetRelientKNotificationIds()
        {
            return ParseIdsFromString(_config["RelientKNotificationIds"]);
        }

        public List<ulong> ParseIdsFromString(string input)
        {
            List<ulong> ids = new List<ulong>();
            if (!string.IsNullOrEmpty(input))
            {
                foreach (var ulongString in input.Split(','))
                {
                    ulong.TryParse(ulongString, out ulong ulongId);
                    ids.Add(ulongId);
                }
            }
            return ids;
        }
    }
}
