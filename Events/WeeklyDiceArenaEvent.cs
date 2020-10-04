using System;
using DiceMastersDiscordBot.Entities;
using DiceMastersDiscordBot.Properties;
using DiceMastersDiscordBot.Services;
using Google.Apis.Http;
using Microsoft.Extensions.Logging;

namespace DiceMastersDiscordBot.Events
{
    public class WeeklyDiceArenaEvent : BaseDiceMastersEvent
    {

        public WeeklyDiceArenaEvent(ILoggerFactory loggerFactory,
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
            DateTime nextDate = DateTime.Now;
            try
            {
                TimeZoneInfo localTimeZone = TimeZoneConverter.TZConvert.GetTimeZoneInfo("Eastern Standard Time");
                DateTime today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, localTimeZone);

                if (today.DayOfWeek == DayOfWeek.Tuesday)
                {
                    nextDate = today;
                }
                else
                {
                    int diff = (7 + (today.DayOfWeek - DayOfWeek.Tuesday)) % 7;
                    nextDate = today.Date.AddDays(-1 * diff).AddDays(7).Date;
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
            }
            return $"{nextDate.Year}-{nextDate.ToString("MMMM")}-{nextDate.Day}";
        }
    }


}
