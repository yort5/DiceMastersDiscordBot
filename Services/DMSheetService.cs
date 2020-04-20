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
        private readonly string _TotMSheetId;

        public DMSheetService(ILoggerFactory loggerFactory, IConfiguration config)
        {
            _logger = loggerFactory.CreateLogger<DMSheetService>(); ;
            _config = config ?? throw new ArgumentNullException(nameof(config));

            _WDASheetId = config["WeeklyDiceArenaSheetId"];
            _DiceFightSheetId = config["DiceFightSheetId"];
            _TotMSheetId = config["TeamOfTheMonthSheetId"];
        }
        public void CheckSheets()
        {
            _logger.LogDebug("Checking Sheets!");
            return;
        }

        public string WDASheetId { get { return _WDASheetId; } }
        public string DiceFightSheetId { get { return _DiceFightSheetId; } }
        public string TotMSheetId { get { return _TotMSheetId; } }

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
            var sheetsService = AuthorizeGoogleSheets();
            string sheetId;
            string sheetName;
            if (message.Channel.Name == "weekly-dice-arena")
            {
                sheetId = WDASheetId;
                sheetName = Refs.WdaSheetName;
            }
            else if (message.Channel.Name == "dice-fight")
            {
                DiceFightHomeSheet df = GetDiceFightHomeSheet(sheetsService);
                sheetId = DiceFightSheetId;
                sheetName = df.SheetName;
            }
            else if (message.Channel.Name == "team-of-the-month")
            {
                sheetId = TotMSheetId;
                sheetName = Refs.TotMSheetName;
            }
            else
            {
                return "Sorry, can't do that on this channel";
            }

            try
            {
                // Define request parameters.
                var range = $"{sheetName}!A:D";

                // load the data
                var sheetRequest = sheetsService.Spreadsheets.Values.Get(sheetId, range);
                var sheetResponse = sheetRequest.Execute();
                var values = sheetResponse.Values;
                return $"There are currently {values.Count - 1} humans signed up for this week's event!";
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
                return "Sorry, wasn't able to determine player count";
            }

        }

        internal void MarkPlayerHere(SocketMessage message)
        {
            throw new NotImplementedException();
        }

        internal string GetCurrentPlayerList(SocketMessage message)
        {
            var sheetsService = AuthorizeGoogleSheets();
            string sheetId;
            string sheetName;
            int nameIndex = 0;
            if (message.Channel.Name == "weekly-dice-arena")
            {
                sheetId = WDASheetId;
                sheetName = Refs.WdaSheetName;
            }
            else if (message.Channel.Name == "dice-fight")
            {
                DiceFightHomeSheet df = GetDiceFightHomeSheet(sheetsService);
                sheetId = DiceFightSheetId;
                sheetName = df.SheetName;
                nameIndex = 1;
            }
            else if (message.Channel.Name == "team-of-the-month")
            {
                sheetId = TotMSheetId;
                sheetName = Refs.TotMSheetName;
            }
            else
            {
                return "Sorry, can't do that on this channel";
            }

            try
            {
                // Define request parameters.
                var range = $"{sheetName}!A:D";

                // load the data
                var sheetRequest = sheetsService.Spreadsheets.Values.Get(sheetId, range);
                var sheetResponse = sheetRequest.Execute();
                var values = sheetResponse.Values;
                StringBuilder currentPlaylerList = new StringBuilder();
                currentPlaylerList.AppendLine($"There are currently {values.Count - 1} humans registered:");
                for (int i = 1; i < values.Count; i++)
                {
                    string name = values[i][nameIndex].ToString();
                    currentPlaylerList.AppendLine(name);
                }
                return currentPlaylerList.ToString();
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
                return "Sorry, wasn't able to determine player count";
            }

        }

        #region Helper Methods
        internal DiceFightHomeSheet GetDiceFightHomeSheet(SheetsService sheetsService)
        {
            var range = $"HomeSheet!A:D";
            var sheetRequest = sheetsService.Spreadsheets.Values.Get(DiceFightSheetId, range);
            var sheetResponse = sheetRequest.Execute();
            StringBuilder format = new StringBuilder();
            foreach (var row in sheetResponse.Values)
            {
                if (row.Count >= 3 && row[0].ToString().ToLower() == Refs.DiceFightSheetName.ToLower())
                {
                    DiceFightHomeSheet sheetInfo = new DiceFightHomeSheet()
                    {
                        EventDate = row[0].ToString(),
                        SheetName = row[1].ToString(),
                        FormatDescription = row[2].ToString(),
                        Info = row[3].ToString()
                    };
                    return sheetInfo;
                }
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