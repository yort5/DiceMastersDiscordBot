using System;
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

        public string GetColumnSpan();
        public string GetBotName();
        public string GetBotHelpString();

        public string GetHackExceptionUser();

    }
}
