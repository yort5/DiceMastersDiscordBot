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
            if (manifest.EventCode == "WDA")
                diceMastersEvent = (IDiceMastersEvent)serviceProvider.GetService(typeof(WeeklyDiceArenaEvent));
            else if (manifest.EventCode == "DF")
                diceMastersEvent = (IDiceMastersEvent)serviceProvider.GetService(typeof(DiceFightEvent));
            else if (manifest.EventCode == "TOTM")
                diceMastersEvent = (IDiceMastersEvent)serviceProvider.GetService(typeof(TeamOfTheMonthEvent));
            else
                diceMastersEvent = (IDiceMastersEvent)serviceProvider.GetService(typeof(StandaloneChallongeEvent));

            diceMastersEvent.Initialize(manifest);
            return diceMastersEvent;
        }

    }
}
