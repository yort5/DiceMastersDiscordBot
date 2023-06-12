using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace DiceMastersDiscordBot.Entities
{
    public class EventManifest
    {
        public string EventName;
        public string EventSheetId;
        public string EventCode;
        public DateTime EventStartTime;
        public List<ulong> EventOrganizerIDList = new List<ulong>();
        public string ChallongeTournamentName;
        public ulong ScoreKeeperChannelId;
    }
}
