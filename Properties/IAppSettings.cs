using System;
using System.Collections.Generic;

namespace DiceMastersDiscordBot.Properties
{
    public interface IAppSettings
    {
        public const string SubmitString = "!submit";

        public string GetDiscordToken();
        public string GetGoogleToken();
        public string GetChallongeToken();

       
        public string GetMasterSheetId();
        public ulong GetScoresChannelId();
        public List<ulong> GetMediaChannelIds();

        public string GetColumnSpan();
        public string GetBotName();
        public string GetBotHelpString();

        public string GetHackExceptionUser();

    }
}
