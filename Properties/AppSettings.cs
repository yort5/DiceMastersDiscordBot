using System;
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
        private readonly string _GoogleToken;
        private readonly string _ChallongeToken;
        private readonly string _MasterSheetId;

        public AppSettings(ILoggerFactory loggerFactory, IConfiguration config)
        {
            _logger = loggerFactory.CreateLogger<AppSettings>(); ;
            _config = config ?? throw new ArgumentNullException(nameof(config));

            _DiscordToken = _config["DiscordToken"];
            _GoogleToken = _config["GoogleCredentials"];
            _ChallongeToken = _config["ChallongeToken"];

            _MasterSheetId = _config["MasterSheetId"];
        }

        public string GetDiscordToken()
        {
            return _DiscordToken;
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
            helpString.Append($"{nl}    .format - returns the current format for that channel's event");
            helpString.Append($"{nl}    .submit http://tb.dicecoalition.com/yourteam - submits your team for the event. Your link will be immediately deleted so others can't see it.");
            helpString.Append($"{nl}    .list - lists the current people signed up for the event.");
            helpString.Append($"{nl}    .here - marks a person as HERE in the spreadsheet for the event (only use at the time of the event).");
            helpString.Append($"{nl}    .drop - marks a person as DROPPED in spreadsheet for the event.");
            helpString.Append($"{nl}    .report @winnerDiscord over @loserDiscord 2-1 - reports scores for an event configured with Challonge integration.");
            helpString.Append($"{nl}VIA DIRECT MESSAGE - you can also send the {_BotName} a direct message");
            helpString.Append($"{nl}    .submit <event> <teambuilder link> - current regular events are: wda (Weekly Dice Arena), df (Dice Fight), totm (Team of the Month)");
            // TODO helpString.Append($"{nl}                                       - current standa-alone: {GetOneOffCode()}");
            helpString.Append($"{nl}Example: `.submit wda http://tb.dicecoalition.com/blahblah`");
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

        public ulong GetScoresChannelId()
        {
            ulong channelId = 0;
            ulong.TryParse(_config["ScoresChannelId"], out channelId);
            return channelId;
        }
    }
}
