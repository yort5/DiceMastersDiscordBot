using System;
namespace DiceMastersDiscordBot.Entities
{
    public class SetInfo
    {
        public SetInfo()
        {
        }

        public string SetCode { get; set; }
        public string SetName { get; set; }
        public string IP { get; set; }
        public string DateReleased { get; set; }
        public bool IsModern { get; set; } = false;
        public bool IsSilver { get; set; } = false;
    }
}
