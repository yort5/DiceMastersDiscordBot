using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiceMastersDiscordBot.Entities
{
    public class HomeSheet
    {
        public string SheetId;
        public string SheetName;
        public String EventName;
        public EventDetails EventDetails;
        public List<string> AuthorizedUsers = new List<string>();
        public List<EventDetails> UpcomingEvents;
    }
}
