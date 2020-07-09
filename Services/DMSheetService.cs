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
                var range = $"{sheet}!A:D";
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
                var range = $"{sheet}!A:D";

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
                var range = $"{sheet.SheetName}!A:D";
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

        internal string GetCurrentPlayerList(SocketMessage message, bool returnCountOnly = false)
        {
            var sheetsService = AuthorizeGoogleSheets();
            HomeSheet homesheet = GetHomeSheet(message.Channel.Name);
            int nameIndex = 1;
            try
            {
                // Define request parameters.
                var range = $"{homesheet.SheetName}!A:D";

                // load the data
                var sheetRequest = sheetsService.Spreadsheets.Values.Get(homesheet.SheetId, range);
                var sheetResponse = sheetRequest.Execute();
                var values = sheetResponse.Values;
                StringBuilder currentPlayerList = new StringBuilder();
                int currentPlayerCount = 0;

                for (int i = 1; i < values.Count; i++)
                {
                    if (!values[i][0].ToString().ToUpper().Equals("DROPPED"))
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
            string sheetId;
            string sheetName;
            if (channelName == "weekly-dice-arena")
            {
                sheetId = WDASheetId;
                sheetName = Refs.WdaSheetName;
            }
            else if (channelName == "dice-fight")
            {
                sheetId = DiceFightSheetId;
                sheetName = Refs.DiceFightSheetName;
            }
            else if (channelName == Refs.EVENT_ONEOFF)
            {
                sheetId = OneOffSheetId;
                sheetName = Refs.EVENT_ONEOFF;
            }
            else if (channelName == "team-of-the-month")
            {
                sheetId = TotMSheetId;
                sheetName = Refs.TotMSheetName;
            }
            else if (channelName == "monthly-one-shot-team-submission")
            {
                sheetId = CRGRM1SSheetId;
                sheetName = Refs.CRGRM1SSheetName;
            }
            else if (channelName == "tttd-team-submission")
            {
                sheetId = CRGRTTTDSheetId;
                sheetName = Refs.CRGRTTTDSheetName;
            }
            else
            {
                return null;
            }

            try
            {
                var range = $"HomeSheet!A:D";
                var sheetRequest = sheetsService.Spreadsheets.Values.Get(sheetId, range);
                var sheetResponse = sheetRequest.Execute();
                HomeSheet sheetInfo = new HomeSheet();
                
                try
                {
                    sheetInfo.EventName = sheetResponse.Values[0][1].ToString();
                }
                catch
                {
                    // no biggee if it isn't there 
                }

                sheetInfo.SheetId = sheetId;
                foreach (var row in sheetResponse.Values)
                {
                    if (row.Count >= 1 && row[0].ToString().ToLower() == sheetName.ToLower())
                    {
                        sheetInfo.EventDate = row[0] != null ? row[0].ToString() : string.Empty;
                        sheetInfo.SheetName = row[1] != null ? row[1].ToString() : string.Empty;
                        sheetInfo.FormatDescription = row.Count >= 3 ? row[2].ToString() : "No information for this event yet";
                        sheetInfo.Info = row.Count >= 4 ? row[3].ToString() : string.Empty;
                        return sheetInfo;
                    }
                }
            }
            catch (Exception exc)
            {
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