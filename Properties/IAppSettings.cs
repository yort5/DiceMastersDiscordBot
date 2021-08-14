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
        public string GetCrimeSheetId();
        public ulong GetScoresChannelId();
        public List<ulong> GetDiceMastersMediaChannelIds();
        public List<ulong> GetNonDiceMastersMediaChannelIds();

        public string GetColumnSpan();
        public string GetBotName();
        public string GetBotHelpString();
        public string GetBotHelpMoreString();

        public string GetHackExceptionUser();

    }
}
