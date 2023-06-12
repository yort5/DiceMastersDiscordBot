using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DiceMastersDiscordBot.Entities;
using DiceMastersDiscordBot.Properties;
using DiceMastersDiscordBot.Services;
using Google.Apis.Http;
using Microsoft.Extensions.Logging;

namespace DiceMastersDiscordBot.Events
{
    public class TwoTeamTakedown : BaseDiceMastersEvent
    {

        public TwoTeamTakedown(ILoggerFactory loggerFactory,
                            IAppSettings appSettings,
                            DMSheetService dMSheetService,
                            ChallongeEvent challonge) : base(loggerFactory, appSettings, dMSheetService, challonge)
        { 
        }

        public override void Initialize(EventManifest manifest)
        {
            string weeklyEventName = GetNextEventName();
            _homeSheet = _sheetService.GetHomeSheet(manifest.EventSheetId, weeklyEventName);

            _channelCode = manifest.EventCode;
            _eventOrganizerDiscordIds = manifest.EventOrganizerIDList ?? new List<ulong>();
            _eventStartTime = manifest.EventStartTime != DateTime.MinValue ? manifest.EventStartTime : DateTime.MaxValue;
        }

        private string GetNextEventName()
        {
            return "two-team-takedown";
        }
    }


}
