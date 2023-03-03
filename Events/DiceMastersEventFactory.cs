using System;
using System.Collections.Generic;
using System.Linq;
using DiceMastersDiscordBot.Entities;

namespace DiceMastersDiscordBot.Events
{
    public class DiceMastersEventFactory
    {
        private readonly IServiceProvider serviceProvider;

        public DiceMastersEventFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public IDiceMastersEvent GetDiceMastersEvent(string eventSelection, List<EventManifest> eventList)
        {
            IDiceMastersEvent diceMastersEvent;
            var manifest = eventList.FirstOrDefault(m => m.EventName.ToLower() == eventSelection.ToLower());
            if (manifest != null)
            {
                if (manifest.EventCode == "WDA")
                    diceMastersEvent = (IDiceMastersEvent)serviceProvider.GetService(typeof(WeeklyDiceArenaEvent));
                else if (manifest.EventCode == "DF")
                    diceMastersEvent = (IDiceMastersEvent)serviceProvider.GetService(typeof(DiceFightEvent));
                else if (manifest.EventCode == "TOTM")
                    diceMastersEvent = (IDiceMastersEvent)serviceProvider.GetService(typeof(TeamOfTheMonthEvent));
                else if (manifest.EventCode == "DS")
                    diceMastersEvent = (IDiceMastersEvent)serviceProvider.GetService(typeof(DiceSocialEvent));
                else if (manifest.EventCode == "TTTD")
                    diceMastersEvent = (IDiceMastersEvent)serviceProvider.GetService(typeof(TwoTeamTakedown));
                else
                    diceMastersEvent = (IDiceMastersEvent)serviceProvider.GetService(typeof(StandaloneChallongeEvent));

                diceMastersEvent.Initialize(manifest);
            }
            else
            {
                diceMastersEvent = (IDiceMastersEvent)serviceProvider.GetService(typeof(NotFoundEvent));
            }

            return diceMastersEvent;
        }

    }
}
