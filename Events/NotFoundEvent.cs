using System;
using DiceMastersDiscordBot.Entities;
using DiceMastersDiscordBot.Properties;
using DiceMastersDiscordBot.Services;
using Google.Apis.Http;
using Microsoft.Extensions.Logging;

namespace DiceMastersDiscordBot.Events
{
    public class NotFoundEvent : BaseDiceMastersEvent
    {

        public NotFoundEvent(ILoggerFactory loggerFactory,
                            IAppSettings appSettings,
                            DMSheetService dMSheetService,
                            ChallongeEvent challonge) : base(loggerFactory, appSettings, dMSheetService, challonge)
        { 
        }

        public override void Initialize(EventManifest manifest)
        {
            // no-op
        }

        public override string GetFormat(int numberEvents)
        {
            return "No Event Found";
        }
    }


}
