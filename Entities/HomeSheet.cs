using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiceMastersDiscordBot.Entities
{
    public class HomeSheet
    {
        // TODO: Add new model for the event information and a list for upcoming events
        public string EventName;
        public string SheetId;
        public string EventDate;
        public string SheetName;
        public string FormatDescription;
        public string Info;
        public List<string> AuthorizedUsers = new List<string>();
    }
}
