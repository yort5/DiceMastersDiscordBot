using DiceMastersDiscordBot.Entities;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Google.Apis.Sheets.v4.Data;
using System.Text;
using System.IO;
using System.Threading;
using Google.Apis.Util.Store;
using System.Linq;
using DiceMastersDiscordBot.Properties;

namespace DiceMastersDiscordBot.Services
{
    public class DMSheetService
    {
        private readonly ILogger _logger;
        private readonly IAppSettings _settings;

        private const string MasterUserSheetName = "UserSheet";

        public DMSheetService(ILoggerFactory loggerFactory, IAppSettings appSettings)
        {
            _logger = loggerFactory.CreateLogger<DMSheetService>(); ;
            _settings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        }
        public void CheckSheets()
        {
            _logger.LogDebug("Checking Sheets!");
            return;
        }

        private SheetsService AuthorizeGoogleSheets()
        {
            try
            {
                string[] Scopes = { SheetsService.Scope.Spreadsheets };
                string ApplicationName = _settings.GetBotName();
                string googleCredentialJson = _settings.GetGoogleToken();

                GoogleCredential credential;
                credential = GoogleCredential.FromJson(googleCredentialJson).CreateScoped(Scopes);
                //Reading Credentials File...
                //using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
                //{
                //    credential = GoogleCredential.FromStream(stream)
                //        .CreateScoped(Scopes);
                //}


                var service = new SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName,
                });

                return service;
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
                return null;
            }
        }

        public string SendLinkToGoogle(string SpreadsheetId, string sheet, EventUserInput eventUserInput)
        {
            try
            {
                var sheetsService = AuthorizeGoogleSheets();
                // Define request parameters.
                var userName = eventUserInput.DiscordName;
                var range = $"{sheet}!{_settings.GetColumnSpan()}";
                // strip off any <>s if people included them
                //userValue = userValue.TrimStart('<').TrimEnd('>');

                // first check to see if this person already has a submission
                var checkExistingRequest = sheetsService.Spreadsheets.Values.Get(SpreadsheetId, range);
                var existingRecords = checkExistingRequest.Execute();
                bool existingEntryFound = false;
                foreach (var record in existingRecords.Values)
                {
                    if (record.Contains(userName))
                    {
                        var index = existingRecords.Values.IndexOf(record);
                        range = $"{sheet}!A{index + 1}";
                        existingEntryFound = true;
                        break;
                    }
                }

                var oblist = new List<object>()
                    { eventUserInput.Here, eventUserInput.DiscordName, eventUserInput.TeamLink, eventUserInput.Misc};
                var valueRange = new ValueRange();
                valueRange.Values = new List<IList<object>> { oblist };

                if (existingEntryFound)
                {
                    // Performing Update Operation...
                    var updateRequest = sheetsService.Spreadsheets.Values.Update(valueRange, SpreadsheetId, range);
                    updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                    var appendReponse = updateRequest.Execute();
                    return $"Thanks {userName}, your info was updated!";
                }
                else
                {
                    // Append the above record...
                    var appendRequest = sheetsService.Spreadsheets.Values.Append(valueRange, SpreadsheetId, range);
                    appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
                    var appendReponse = appendRequest.Execute();
                    return $"Thanks {userName}, your info was added!";
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
                return "There was an error trying to add your info";
            }
        }

        internal List<EventManifest> LoadEventManifests()
        {
            var sheetsService = AuthorizeGoogleSheets();
            List<EventManifest> currentEvents = new List<EventManifest>();
            try
            {
                // Define request parameters.
                var range = $"DiscordEventSheet!{_settings.GetColumnSpan()}";

                // load the data
                var sheetRequest = sheetsService.Spreadsheets.Values.Get(_settings.GetMasterSheetId(), range);
                var sheetResponse = sheetRequest.Execute();
                var values = sheetResponse.Values;


                foreach (var record in values)
                {
                    try
                    {
                        EventManifest eventManifest = new EventManifest
                        {
                            EventName = (record.Count >= 1 && record[0] != null) ? record[0].ToString() : string.Empty,
                            EventSheetId = (record.Count >= 2 && record[1] != null) ? record[1].ToString() : string.Empty,
                            EventCode = (record.Count >= 3 && record[2] != null) ? record[2].ToString() : string.Empty,
                            ChallongeTournamentName = (record.Count >= 5 && record[4] != null) ? record[4].ToString() : string.Empty
                        };
                        if(record.Count >= 4 && record[3] != null)
                        {
                            // probably a slicker way to do this but there's no time!
                            var idList = record[3].ToString().Split(',').ToList();
                            foreach(var id in idList)
                            {
                                eventManifest.EventOrganizerIDList.Add(id.Trim());
                            }
                        }
                        currentEvents.Add(eventManifest);
                    }
                    catch (Exception exc)
                    {
                        _logger.LogError($"Exception loading eventManifests: {exc.Message}");
                    }
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
            }
            return currentEvents;

        }

        //internal string GetFormatFromGoogle(SheetsService sheetsService, string SpreadsheetId, string sheet)
        //{
        //    try
        //    {
        //        // Define request parameters.
        //        var range = $"{sheet}!{_settings.GetColumnSpan()}";

        //        // load the data
        //        var sheetRequest = sheetsService.Spreadsheets.Values.Get(SpreadsheetId, range);
        //        var sheetResponse = sheetRequest.Execute();
        //        var values = sheetResponse.Values;
        //        if (values != null && values.Count > 0)
        //        {
        //            return values[0][1].ToString();
        //        }
        //    }
        //    catch (Exception exc)
        //    {
        //        Console.WriteLine(exc.Message);
        //    }
        //    return "There was an error trying to retrieve the format information";
        //}

        internal int GetCurrentPlayerCount(HomeSheet homeSheet, string eventName)
        {
            return GetCurrentPlayerList(homeSheet, eventName).Count();
        }

        internal bool MarkPlayerHere(EventUserInput eventUserInput, HomeSheet sheet)
        {
            return MarkPlayer(eventUserInput, sheet, "HERE");
        }
        internal bool MarkPlayerDropped(EventUserInput eventUserInput, HomeSheet sheet)
        {
            return MarkPlayer(eventUserInput, sheet, "DROPPED");
        }

        private bool MarkPlayer(EventUserInput eventUserInput, HomeSheet sheet, string status)
        {
            try
            {
                var sheetService = AuthorizeGoogleSheets();
                // Define request parameters.
                var userName = eventUserInput.DiscordName;
                var range = $"{sheet.SheetName}!{_settings.GetColumnSpan()}";
                // strip off any <>s if people included them
                //userValue = userValue.TrimStart('<').TrimEnd('>');

                // first check to see if this person already has a submission
                var checkExistingRequest = sheetService.Spreadsheets.Values.Get(sheet.SheetId, range);
                var existingRecords = checkExistingRequest.Execute();
                foreach (var record in existingRecords.Values)
                {
                    foreach (var cell in record)
                    {
                        if (cell.ToString().ToLower().Trim().Equals(userName.ToLower().Trim()))
                        {

                            var index = existingRecords.Values.IndexOf(record);
                            range = $"{sheet.SheetName}!A{index + 1}";

                            eventUserInput.TeamLink = (record.Count >= 3 && record[2] != null) ? record[2].ToString() : string.Empty; ;
                            eventUserInput.Misc = (record.Count >= 4 && record[3] != null) ? record[3].ToString() : string.Empty; ;

                            var oblist = new List<object>()
                                { eventUserInput.Here, eventUserInput.DiscordName, eventUserInput.TeamLink, eventUserInput.Misc};
                            var valueRange = new ValueRange();
                            valueRange.Values = new List<IList<object>> { oblist };

                            // Performing Update Operation...
                            var updateRequest = sheetService.Spreadsheets.Values.Update(valueRange, sheet.SheetId, range);
                            updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                            var appendReponse = updateRequest.Execute();
                            return true;
                        }
                    }
                }
                return false;
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
                return false;
            }
        }

        //internal string ListTeams(EventUserInput eventUserInput)
        //{
        //    try
        //    {
        //        var sheetService = AuthorizeGoogleSheets();
        //        var sheet = GetHomeSheet(eventUserInput.EventName);
        //        if (sheet == null) return "No information found for this event";
        //        if (sheet.AuthorizedUsers.Count == 0) return "No users are authorized for this action";
        //        // Define request parameters.
        //        var userName = eventUserInput.DiscordName;

        //        if (!sheet.AuthorizedUsers.Contains(userName)) return "You are not authorized to do this action";

        //        // Define request parameters.
        //        var range = $"{sheet.SheetName}!{_settings.GetColumnSpan()}";

        //        // load the data
        //        var sheetRequest = sheetService.Spreadsheets.Values.Get(sheet.SheetId, range);
        //        var sheetResponse = sheetRequest.Execute();
        //        var values = sheetResponse.Values;
        //        StringBuilder teamList = new StringBuilder();
        //        int nameIndex = 1;
        //        int teamIndex = 2;

        //        for (int i = 1; i < values.Count; i++)
        //        {
        //            //if (values[i][0].ToString().ToUpper().Equals("HERE"))
        //            //{
        //                string name = values[i][nameIndex].ToString();
        //                string team = values[i][teamIndex].ToString();
        //                teamList.AppendLine(name);
        //                teamList.AppendLine(team);
        //            //}
        //        }

        //        return $"Here are the team links for players currently marked HERE:{Environment.NewLine}{teamList}";
        //    }
        //    catch (Exception exc)
        //    {
        //        Console.WriteLine(exc.Message);
        //        return "There was an error trying to add your info";
        //    }

        //}

        internal string SearchForCard(string cardId)
        {
            return "Not quite working yet";
        }

        internal List<UserInfo> GetCurrentPlayerList(HomeSheet homesheet, string eventName)
        {
            var sheetsService = AuthorizeGoogleSheets();
            int nameIndex = 1;
            List<UserInfo> currentPlayerList = new List<UserInfo>();
            try
            {
                // Define request parameters.
                var range = $"{homesheet.SheetName}!{_settings.GetColumnSpan()}";

                // load the data
                var sheetRequest = sheetsService.Spreadsheets.Values.Get(homesheet.SheetId, range);
                var sheetResponse = sheetRequest.Execute();
                var values = sheetResponse.Values;


                for (int i = 1; i < values.Count; i++)
                { 
                    if (values[i].Count > 0 && !values[i][0].ToString().ToUpper().Equals("DROPPED"))
                    {
                        UserInfo user = new UserInfo();
                        user.DiscordName = values[i][nameIndex].ToString();
                        currentPlayerList.Add(user);
                    }
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
            }
            return currentPlayerList;
        }

        #region Helper Methods
        internal HomeSheet GetHomeSheet(string sheetId, string eventName)
        {
            var sheetsService = AuthorizeGoogleSheets();
            HomeSheet sheetInfo = new HomeSheet();
            sheetInfo.SheetId = sheetId;
            sheetInfo.SheetName = eventName;

            try
            {
                var range = $"HomeSheet!{_settings.GetColumnSpan()}";
                var sheetRequest = sheetsService.Spreadsheets.Values.Get(sheetInfo.SheetId, range);
                var sheetResponse = sheetRequest.Execute();

                
                try
                {
                    sheetInfo.EventName = sheetResponse.Values[0][1].ToString();
                }
                catch
                {
                    // no biggee if it isn't there, just use the channel name
                    sheetInfo.EventName = eventName;
                }

                foreach (var row in sheetResponse.Values)
                {
                    if (row.Count >= 1 && row[0].ToString().ToLower() == sheetInfo.SheetName.ToLower())
                    {
                        sheetInfo.EventDate = row[0] != null ? row[0].ToString() : string.Empty;
                        sheetInfo.SheetName = row[1] != null ? row[1].ToString() : string.Empty;
                        sheetInfo.FormatDescription = row.Count >= 3 ? row[2].ToString() : "No information for this event yet";
                        sheetInfo.Info = row.Count >= 4 ? row[3].ToString() : string.Empty;
                        string authUserList = row.Count >= 5 ? row[4].ToString() : string.Empty;
                        if(!string.IsNullOrEmpty(authUserList))
                        {
                            sheetInfo.AuthorizedUsers = authUserList.Split(',').ToList();
                        }
                        return sheetInfo;
                    }
                }
            }
            catch (Exception exc)
            {
                _logger.LogError($"Exception getting HomeSheet: {exc.Message}");
                return null;
            }
            return null;
        }

        internal UserInfo GetUserInfoFromDiscord(string username)
        {
            var sheetService = AuthorizeGoogleSheets();
            var userInfo = GetUserInfo(sheetService, username, 0);
            if (string.IsNullOrEmpty(userInfo.DiscordName)) { userInfo.DiscordName = username; } 
            return userInfo;
        }

        internal UserInfo GetUserInfoFromWIN(string username)
        {
            var sheetService = AuthorizeGoogleSheets();
            var userInfo = GetUserInfo(sheetService, username, 1);
            if(string.IsNullOrEmpty(userInfo.WINName)) { userInfo.WINName = username; }
            return userInfo;
        }

        internal UserInfo GetUserInfoFromChallonge(string username)
        {
            var sheetService = AuthorizeGoogleSheets();
            var userInfo = GetUserInfo(sheetService, username, 2);
            if (string.IsNullOrEmpty(userInfo.ChallongeName)) { userInfo.ChallongeName = username; }
            return userInfo;
        }

        internal UserInfo GetUserInfoFromTwitch(string username)
        {
            var sheetService = AuthorizeGoogleSheets();
            var userInfo = GetUserInfo(sheetService, username, 3);
            if (string.IsNullOrEmpty(userInfo.TwitchName)) { userInfo.TwitchName = username; }
            return userInfo;
        }

        private UserInfo GetUserInfo(SheetsService sheetsService, string name, int nameIndex)
        {
            var range = $"{MasterUserSheetName}!A:D";
            var sheetRequest = sheetsService.Spreadsheets.Values.Get(_settings.GetMasterSheetId(), range);
            var sheetResponse = sheetRequest.Execute();
            UserInfo userInfo = new UserInfo();
            try
            {
                foreach (var row in sheetResponse.Values)
                {
                    if (row.Count > nameIndex)
                    {
                        if (row[nameIndex].ToString().ToLower() == name.ToLower())
                        {
                            userInfo.DiscordName = row[0].ToString();
                            userInfo.WINName = row.Count >= 2 ? row[1].ToString() : string.Empty;
                            userInfo.ChallongeName = row.Count >= 3 ? row[2].ToString() : string.Empty;
                            userInfo.TwitchName = row.Count >= 4 ? row[3].ToString() : string.Empty;
                            break;
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                _logger.LogError($"Exception trying to find a UserInfo: {exc.Message}");
            }
            return userInfo;
        }

        public string SendUserInfoToGoogle(UserInfo userInfo)
        {
            try
            {
                var sheetsService = AuthorizeGoogleSheets();
                // Define request parameters.
                var userName = userInfo.DiscordName;
                var range = $"{MasterUserSheetName}!{_settings.GetColumnSpan()}";
                // strip off any <>s if people included them
                //userValue = userValue.TrimStart('<').TrimEnd('>');

                // first check to see if this person already has a submission
                var checkExistingRequest = sheetsService.Spreadsheets.Values.Get(_settings.GetMasterSheetId(), range);
                var existingRecords = checkExistingRequest.Execute();
                bool existingEntryFound = false;
                foreach (var record in existingRecords.Values)
                {
                    if (record.Contains(userName))
                    {
                        var index = existingRecords.Values.IndexOf(record);
                        range = $"{MasterUserSheetName}!A{index + 1}";
                        existingEntryFound = true;
                        break;
                    }
                }

                var oblist = new List<object>()
                    { userInfo.DiscordName, userInfo.WINName, userInfo.ChallongeName, userInfo.TwitchName };
                var valueRange = new ValueRange();
                valueRange.Values = new List<IList<object>> { oblist };

                if (existingEntryFound)
                {
                    // Performing Update Operation...
                    var updateRequest = sheetsService.Spreadsheets.Values.Update(valueRange, _settings.GetMasterSheetId(), range);
                    updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                    var appendReponse = updateRequest.Execute();
                    return $"Thanks {userName}, your info was updated!";
                }
                else
                {
                    // Append the above record...
                    var appendRequest = sheetsService.Spreadsheets.Values.Append(valueRange, _settings.GetMasterSheetId(), range);
                    appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
                    var appendReponse = appendRequest.Execute();
                    return $"Thanks {userName}, your info was added!";
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
                return "There was an error trying to add your info";
            }
        }

        #endregion
    }
}