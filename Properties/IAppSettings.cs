using System;
namespace DiceMastersDiscordBot.Properties
{
    public interface IAppSettings
    {
        public string GetDiscordToken();
        public string GetGoogleToken();
        public string GetChallongeToken();

        public string GetWDAChannelName();
        public string GetWDASheetId();
        public string GetWDASheetName();
        public string GetDiceFightChannelName();
        public string GetDiceFightSheetId();
        public string GetDiceFightSheetName();
        public string GetOneOffSheetId();
        public string GetOneOffSheetName();
        public string GetOneOffChannelName();
        public string GetOneOffCode();
        public string GetOneOffChallongeId();
        public string GetOneOffTODiscordID();
        public string GetTotMSheetId();
        public string GetTotMSheetName();
        public string GetTotMChannelName();
        public string GetMasterSheetId();
        public ulong GetScoresChannelId();

        public string GetColumnSpan();
        public string GetBotName();
        public string GetBotHelpString();

    }
}
