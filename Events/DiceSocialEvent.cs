using System;
using DiceMastersDiscordBot.Entities;
using DiceMastersDiscordBot.Properties;
using DiceMastersDiscordBot.Services;
using Google.Apis.Http;
using Microsoft.Extensions.Logging;

namespace DiceMastersDiscordBot.Events
{
    public class DiceSocialEvent : BaseDiceMastersEvent
    {

        public TeamOfTheMonthEvent(ILoggerFactory loggerFactory,
                            IAppSettings appSettings,
                            DMSheetService dMSheetService,
                            ChallongeEvent challonge) : base(loggerFactory, appSettings, dMSheetService, challonge)
        { 
        }

        public override void Initialize(EventManifest manifest)
        {
            string weeklyEventName = GetMonthlyEventName();
            _homeSheet = _sheetService.GetHomeSheet(manifest.EventSheetId, weeklyEventName);
        }


        private string GetMonthlyEventName()
        {
            DateTime today = DateTime.Now;
            return $"{today.Year}-{today.ToString("MMMM")}";
        }
    }


}
