using System;
using System.Collections.Generic;

namespace DiceMastersDiscordBot.Entities
{
    public class EventManifest
    {
        public string EventName;
        public string EventSheetId;
        public string EventCode;
        public List<string> EventOrganizerIDList = new List<string>();
        public string ChallongeTournamentName;
    }
}
