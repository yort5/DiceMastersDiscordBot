using System;
using System.Collections.Generic;
using DiceMastersDiscordBot.Entities;
using DiceMastersDiscordBot.Properties;
using DiceMastersDiscordBot.Services;
using Google.Apis.Http;
using Microsoft.Extensions.Logging;

namespace DiceMastersDiscordBot.Events
{
    public class DiceFightEvent : BaseDiceMastersEvent
    {
        private const string TimeZoneString = "Central Europe Standard Time";

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

            _channelCode = manifest.EventCode;
            _eventOrganizerDiscordIds = manifest.EventOrganizerIDList ?? new List<ulong>();
            _eventStartTime = manifest.EventStartTime != DateTime.MinValue ? manifest.EventStartTime : DateTime.MaxValue;
        }


        private string GetWeeklyEventName()
        {
            var nextDate = GetNextEventTime();
            return $"{nextDate.Year}-{nextDate.ToString("MMMM")}-{nextDate.Day}";
        }

        public override List<EventUserInput> GetTeamLists(ulong userId)
        {
            var nextEventTimeLocal = GetNextEventTime();
            TimeZoneInfo localTimeZone = TimeZoneConverter.TZConvert.GetTimeZoneInfo(TimeZoneString);
            DateTime nextEventTime = TimeZoneInfo.ConvertTimeToUtc(nextEventTimeLocal, localTimeZone);

            if (_eventOrganizerDiscordIds.Contains(userId)
                || (nextEventTime < DateTime.UtcNow && DateTime.UtcNow < nextEventTime.AddDays(1)))
            {
                return _sheetService.GetTeams(_homeSheet);
            }
            else
            {
                return new List<EventUserInput>();
            }
        }

        private DateTime GetNextEventTime()
        {
            DateTime nextDate = DateTime.MaxValue;
            try
            {
                TimeZoneInfo localTimeZone = TimeZoneConverter.TZConvert.GetTimeZoneInfo(TimeZoneString);
                DateTime today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, localTimeZone);
                if (today.DayOfWeek == DayOfWeek.Thursday)
                {
                    nextDate = today;
                }
                else
                {
                    int diff = (7 + (today.DayOfWeek - DayOfWeek.Thursday)) % 7;
                    nextDate = today.Date.AddDays(-1 * diff).AddDays(7).Date;
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
            }
            return nextDate.Date.AddHours(22);
        }
    }


}
