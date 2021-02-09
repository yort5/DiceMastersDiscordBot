using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DiceMastersDiscordBot.Entities;
using DiceMastersDiscordBot.Properties;
using DiceMastersDiscordBot.Services;
using Microsoft.Extensions.Logging;

namespace DiceMastersDiscordBot.Events
{
    public class StandaloneChallongeEvent : BaseDiceMastersEvent
    {
        private string _challongeTournamentName;

        public StandaloneChallongeEvent(ILoggerFactory loggerFactory,
                            IAppSettings appSettings,
                            DMSheetService dMSheetService,
                            ChallongeEvent challonge) : base(loggerFactory, appSettings, dMSheetService, challonge)
        {
            _useChallonge = true;
        }

        public override void Initialize(EventManifest manifest)
        {
            if(!string.IsNullOrEmpty(manifest.ChallongeTournamentName))
            {
                _challongeTournamentName = manifest.ChallongeTournamentName;
            }
            base.Initialize(manifest);
        }

        public async override Task<string> MarkPlayerHereAsync(EventUserInput eventUserInput)
        {
            string response = $"There was an error checking in Discord User {eventUserInput.DiscordName} - please check in manually at Challonge.com";
            try
            {
                var userInfo = _sheetService.GetUserInfoFromDiscord(eventUserInput.DiscordName);

                if (string.IsNullOrEmpty(userInfo.ChallongeName))
                {
                    response = $"Cannot check in Discord user {eventUserInput.DiscordName} as there is no mapped Challonge ID. Please use `.challonge mychallongeusername` to tell the {_settings.GetBotName()} who you are in Challonge.";
                }
                else
                {
                    // check that tournament has a check-in
                    var tournament = await _challonge.GetTournamentAsync(_challongeTournamentName);
                    if(tournament.Count > 1)
                    {
                        return $"Sorry, apparently there is more than one tournament named {_challongeTournamentName}?";
                    }
                    if(tournament.FirstOrDefault().CheckInDuration == null)
                    {
                        // check in via spreadsheet instead.
                        
                    }

                    var participants = await _challonge.GetAllParticipantsAsync(_challongeTournamentName);
                    var player = participants.SingleOrDefault(p => p.ChallongeUsername == userInfo.ChallongeName);
                    if (player == null)
                    {
                        response = $"There was an error checking in Challonge User {userInfo.ChallongeName} - they were not returned as registered for this tournament.";
                    }
                    else
                    {
                        var resultParticipant = await _challonge.CheckInParticipantAsync(player.Id.ToString(), _challongeTournamentName);
                        if (resultParticipant.CheckedIn == true)
                        {
                            response = $"Success! Challonge User {userInfo.ChallongeName} (Discord: {userInfo.DiscordName}) is checked in for the event!";
                        }
                        else
                        {
                            response = $"There was an error checking in Challonge User {userInfo.ChallongeName} - please check in manually at Challonge.com";
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                _logger.LogError(exc.Message);
            }
            return response;
        }

        public async override Task<List<UserInfo>> GetCurrentPlayerList()
        {
            var participantList = await _challonge.GetAllParticipantsAsync(_challongeTournamentName);
            List<UserInfo> userList = new List<UserInfo>();
            foreach(var player in participantList)
            {
                var user = _sheetService.GetUserInfoFromChallonge(player.ChallongeUsername);
                if (user == null)
                {
                    user = new UserInfo() { ChallongeName = player.ChallongeUsername };
                }
                userList.Add(user);
            }
            return userList;
        }
    }
}
