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

namespace DiceMastersDiscordBot.Services
{
    public class DMSheetService
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _config;

        private readonly string _WDASheetId;
        private readonly string _DiceFightSheetId;
        private readonly string _OneOffSheetId;
        private readonly string _TotMSheetId;
        private readonly string _CRGRM1SSheetId;
        private readonly string _CRGRTTTDSheetId;
        private readonly string _MasterSheetId;

        public DMSheetService(ILoggerFactory loggerFactory, IConfiguration config)
        {
            _logger = loggerFactory.CreateLogger<DMSheetService>(); ;
            _config = config ?? throw new ArgumentNullException(nameof(config));

            _WDASheetId = config["WeeklyDiceArenaSheetId"];
            _DiceFightSheetId = config["DiceFightSheetId"];
            _TotMSheetId = config["TeamOfTheMonthSheetId"];
            _CRGRM1SSheetId = config["CRGRMonthlyOneShotSheetId"];
            _CRGRTTTDSheetId = config["CRGRTwoTeamTakeDownSheetId"];
            _OneOffSheetId = config["OneOffSheetId"];
            _MasterSheetId = config["MasterSheetId"];
        }
        public void CheckSheets()
        {
            _logger.LogDebug("Checking Sheets!");
            return;
        }

        public string WDASheetId { get { return _WDASheetId; } }
        public string DiceFightSheetId { get { return _DiceFightSheetId; } }
        public string TotMSheetId { get { return _TotMSheetId; } }
        public string CRGRM1SSheetId { get { return _CRGRM1SSheetId; } }
        public string CRGRTTTDSheetId { get { return _CRGRTTTDSheetId; } }
        public string OneOffSheetId { get { return _OneOffSheetId; } }


        public SheetsService AuthorizeGoogleSheets()
        {
            try
            {
                string[] Scopes = { SheetsService.Scope.Spreadsheets };
                string ApplicationName = Refs.BOT_NAME;
                string googleCredentialJson = _config["GoogleCredentials"];

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

        public string SendLinkToGoogle(SheetsService sheetsService, SocketMessage message, string SpreadsheetId, string sheet, ColumnInput columnInput)
        {
            try
            {
                // Define request parameters.
                var userName = message.Author.Username;
                var range = $"{sheet}!{Refs.COL_SPAN}";
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
                    { columnInput.Column1Value, columnInput.Column2Value, columnInput.Column3Value, columnInput.Column4Value};
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

        internal string GetFormatFromGoogle(SheetsService sheetsService, SocketMessage message, string SpreadsheetId, string sheet)
        {
            try
            {
                // Define request parameters.
                var userName = message.Author.Username;
                var range = $"{sheet}!A:E";

                // load the data
                var sheetRequest = sheetsService.Spreadsheets.Values.Get(SpreadsheetId, range);
                var sheetResponse = sheetRequest.Execute();
                var values = sheetResponse.Values;
                if (values != null && values.Count > 0)
                {
                    return values[0][1].ToString();
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
            }
            return "There was an error trying to retrieve the format information";
        }

        internal string GetCurrentPlayerCount(SocketMessage message)
        {
            return GetCurrentPlayerList(message, true);
        }

        internal string MarkPlayerHere(SocketMessage message)
        {
            return MarkPlayer(message, "HERE");
        }
        internal string MarkPlayerDropped(SocketMessage message)
        {
            return MarkPlayer(message, "DROPPED");
        }

        internal string MarkPlayer(SocketMessage message, string status)
        {
            try
            {
                var sheetService = AuthorizeGoogleSheets();
                var sheet = GetHomeSheet(message.Channel.Name);
                // Define request parameters.
                var userName = message.Author.Username;
                var range = $"{sheet.SheetName}!A:E";
                // strip off any <>s if people included them
                //userValue = userValue.TrimStart('<').TrimEnd('>');

                // first check to see if this person already has a submission
                var checkExistingRequest = sheetService.Spreadsheets.Values.Get(sheet.SheetId, range);
                var existingRecords = checkExistingRequest.Execute();
                ColumnInput columnInput = null;
                foreach (var record in existingRecords.Values)
                {
                    foreach (var cell in record)
                    {
                        if (cell.ToString().ToLower().Trim().Equals(userName.ToLower().Trim()))
                        {

                            var index = existingRecords.Values.IndexOf(record);
                            range = $"{sheet.SheetName}!A{index + 1}";

                            columnInput = new ColumnInput();
                            columnInput.Column1Value = status;
                            columnInput.Column2Value = (record.Count >= 2 && record[1] != null) ? record[1].ToString() : string.Empty; ;
                            columnInput.Column3Value = (record.Count >= 3 && record[2] != null) ? record[2].ToString() : string.Empty; ;
                            columnInput.Column4Value = (record.Count >= 4 && record[3] != null) ? record[3].ToString() : string.Empty; ;

                            var oblist = new List<object>()
                            { columnInput.Column1Value, columnInput.Column2Value, columnInput.Column3Value, columnInput.Column4Value};
                            var valueRange = new ValueRange();
                            valueRange.Values = new List<IList<object>> { oblist };

                            // Performing Update Operation...
                            var updateRequest = sheetService.Spreadsheets.Values.Update(valueRange, sheet.SheetId, range);
                            updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                            var appendReponse = updateRequest.Execute();
                            return $"Player {message.Author.Username} marked as {status} in the spreadsheet";
                        }
                    }
                }
                return $"Sorry, could not mark {message.Author.Username} as {status} as they were not found in the spreadsheet for this event.";
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
                return "There was an error trying to add your info";
            }
        }

        internal string ListTeams(SocketMessage message)
        {
            try
            {
                var sheetService = AuthorizeGoogleSheets();
                var sheet = GetHomeSheet(message.Channel.Name);
                if (sheet == null) return "No information found for this event";
                if (sheet.AuthorizedUsers.Count == 0) return "No users are authorized for this action";
                // Define request parameters.
                var userName = message.Author.Username;

                if (!sheet.AuthorizedUsers.Contains(userName)) return "You are not authorized to do this action";

                // Define request parameters.
                var range = $"{sheet.SheetName}!{Refs.COL_SPAN}";

                // load the data
                var sheetRequest = sheetService.Spreadsheets.Values.Get(sheet.SheetId, range);
                var sheetResponse = sheetRequest.Execute();
                var values = sheetResponse.Values;
                StringBuilder teamList = new StringBuilder();
                int nameIndex = 1;
                int teamIndex = 2;

                for (int i = 1; i < values.Count; i++)
                {
                    //if (values[i][0].ToString().ToUpper().Equals("HERE"))
                    //{
                        string name = values[i][nameIndex].ToString();
                        string team = values[i][teamIndex].ToString();
                        teamList.AppendLine(name);
                        teamList.AppendLine(team);
                    //}
                }

                return $"Here are the team links for players currently marked HERE:{Environment.NewLine}{teamList}";
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
                return "There was an error trying to add your info";
            }

        }

        internal string ListTeams(string channelName, string userName)
        {
            try
            {
                var sheetService = AuthorizeGoogleSheets();
                var sheet = GetHomeSheet(channelName);
                if (sheet == null) return "No information found for this event";
                if (sheet.AuthorizedUsers.Count == 0) return "No users are authorized for this action";
                // Define request parameters

                if (!sheet.AuthorizedUsers.Contains(userName)) return "You are not authorized to do this action";

                // Define request parameters.
                var range = $"{sheet.SheetName}!{Refs.COL_SPAN}";

                // load the data
                var sheetRequest = sheetService.Spreadsheets.Values.Get(sheet.SheetId, range);
                var sheetResponse = sheetRequest.Execute();
                var values = sheetResponse.Values;
                StringBuilder teamList = new StringBuilder();
                int nameIndex = 1;
                int teamIndex = 2;

                for (int i = 1; i < values.Count; i++)
                {
                    //if (values[i][0].ToString().ToUpper().Equals("HERE"))
                    //{
                    string name = values[i][nameIndex].ToString();
                    string team = values[i][teamIndex].ToString();
                    teamList.AppendLine(name);
                    teamList.AppendLine(team);
                    //}
                }

                return $"Here are the team links for players currently marked HERE:{Environment.NewLine}{teamList}";
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
                return "There was an error trying to add your info";
            }

        }

        internal string SearchForCard(string cardId)
        {
            return "Not quite working yet";
        }

        internal string GetCurrentPlayerList(SocketMessage message, bool returnCountOnly = false)
        {
            var sheetsService = AuthorizeGoogleSheets();
            HomeSheet homesheet = GetHomeSheet(message.Channel.Name);
            int nameIndex = 1;
            try
            {
                // Define request parameters.
                var range = $"{homesheet.SheetName}!A:E";

                // load the data
                var sheetRequest = sheetsService.Spreadsheets.Values.Get(homesheet.SheetId, range);
                var sheetResponse = sheetRequest.Execute();
                var values = sheetResponse.Values;
                StringBuilder currentPlayerList = new StringBuilder();
                int currentPlayerCount = 0;

                for (int i = 1; i < values.Count; i++)
                { 
                    if (values[i].Count > 0 && !values[i][0].ToString().ToUpper().Equals("DROPPED"))
                    {
                        string name = values[i][nameIndex].ToString();
                        currentPlayerList.AppendLine(name);
                        currentPlayerCount++;
                    }
                }

                if (returnCountOnly)
                {
                    return $"There are currently {currentPlayerCount} humans registered (and no robots)";
                }
                else
                {
                    return $"There are currently {currentPlayerCount} humans registered (and no robots):{Environment.NewLine}{currentPlayerList}"; 
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
                return "Sorry, wasn't able to determine player list";
            }

        }

        #region Helper Methods
        internal HomeSheet GetHomeSheet(string channelName)
        {
            var sheetsService = AuthorizeGoogleSheets();
            HomeSheet sheetInfo = new HomeSheet();

            if (channelName == "weekly-dice-arena")
            {
                sheetInfo.SheetId = WDASheetId;
                sheetInfo.SheetName = Refs.WdaSheetName;
        }
            else if (channelName == "dice-fight")
            {
                sheetInfo.SheetId = DiceFightSheetId;
                sheetInfo.SheetName = Refs.DiceFightSheetName;
            }
            else if (channelName == Refs.EVENT_ONEOFF)
            {
                sheetInfo.SheetId = OneOffSheetId;
                sheetInfo.SheetName = Refs.EVENT_ONEOFF;
            }
            else if (channelName == "team-of-the-month")
            {
                sheetInfo.SheetId = TotMSheetId;
                sheetInfo.SheetName = Refs.TotMSheetName;
            }
            else if (channelName == "monthly-one-shot-team-submission")
            {
                sheetInfo.SheetId = CRGRM1SSheetId;
                sheetInfo.SheetName = Refs.CRGRM1SSheetName;
            }
            else if (channelName == "tttd-team-submission")
            {
                sheetInfo.SheetId = CRGRTTTDSheetId;
                sheetInfo.SheetName = Refs.CRGRTTTDSheetName;
            }
            else
            {
                return null;
            }

            try
            {
                var range = $"HomeSheet!A:E";
                var sheetRequest = sheetsService.Spreadsheets.Values.Get(sheetInfo.SheetId, range);
                var sheetResponse = sheetRequest.Execute();

                
                try
                {
                    sheetInfo.EventName = sheetResponse.Values[0][1].ToString();
                }
                catch
                {
                    // no biggee if it isn't there 
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

        internal string GetWINName(SheetsService sheetsService, string sheetId, string discordName)
        {
            var range = $"WINSheet!A:B";
            var sheetRequest = sheetsService.Spreadsheets.Values.Get(sheetId, range);
            var sheetResponse = sheetRequest.Execute();
            string winName = string.Empty;
            foreach (var row in sheetResponse.Values)
            {
                if (row.Count >= 2)
                {
                    if (row[0].ToString() == discordName)
                    {
                        winName = row[1].ToString();
                        break;
                    }
                }
            }
            return winName;
        }
        #endregion
    }
}