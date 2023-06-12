﻿using System;
using System.Collections.Generic;
using DiceMastersDiscordBot.Entities;
using DiceMastersDiscordBot.Properties;
using DiceMastersDiscordBot.Services;
using Google.Apis.Http;
using Microsoft.Extensions.Logging;

namespace DiceMastersDiscordBot.Events
{
    public class DiceSocialEvent : BaseDiceMastersEvent
    {

        public DiceSocialEvent(ILoggerFactory loggerFactory,
                            IAppSettings appSettings,
                            DMSheetService dMSheetService,
                            ChallongeEvent challonge) : base(loggerFactory, appSettings, dMSheetService, challonge)
        { 
        }

        public override void Initialize(EventManifest manifest)
        {
            string weeklyEventName = GetMonthlyEventName();
            _homeSheet = _sheetService.GetHomeSheet(manifest.EventSheetId, weeklyEventName);

            _channelCode = manifest.EventCode;
            _eventOrganizerDiscordIds = manifest.EventOrganizerIDList ?? new List<ulong>();
            _eventStartTime = manifest.EventStartTime != DateTime.MinValue ? manifest.EventStartTime : DateTime.MaxValue;
        }


        private string GetMonthlyEventName()
        {
            DateTime today = DateTime.Now;
            return $"{today.Year}-{today.ToString("MMMM")}";
        }
    }


}
