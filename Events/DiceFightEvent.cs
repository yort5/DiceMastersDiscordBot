using System;
using DiceMastersDiscordBot.Entities;
using DiceMastersDiscordBot.Properties;
using DiceMastersDiscordBot.Services;
using Google.Apis.Http;
using Microsoft.Extensions.Logging;

namespace DiceMastersDiscordBot.Events
{
    public class DiceFightEvent : BaseDiceMastersEvent
    {

        public DiceFightEvent(ILoggerFactory loggerFactory,
                            IAppSettings appSettings,
                            DMSheetService dMSheetService,
                            ChallongeEvent challonge) : base(loggerFactory, appSettings, dMSheetService, challonge)
        { 
        }

        public override void Initialize(EventManifest manifest)
        {
            string weeklyEventName = GetWeeklyEventName();
            _homeSheet = _sheetService.GetHomeSheet(manifest.EventSheetId, weeklyEventName);
        }


        private string GetWeeklyEventName()
        {
            TimeZoneInfo localTimeZone = TimeZoneConverter.TZConvert.GetTimeZoneInfo("Central Europe Standard Time");
            DateTime today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, localTimeZone);
            DateTime nextDate;
            if (today.DayOfWeek == DayOfWeek.Thursday)
            {
                nextDate = today;
            }
            else
            {
                int diff = (7 + (today.DayOfWeek - DayOfWeek.Thursday)) % 7;
                nextDate = today.Date.AddDays(-1 * diff).AddDays(7).Date;
            }
            return $"{nextDate.Year}-{nextDate.ToString("MMMM")}-{nextDate.Day}";
        }
    }


}
