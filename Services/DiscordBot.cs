using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DiceMastersDiscordBot.Services
{
    public class DiscordBot : BackgroundService
    {
        private DiscordSocketClient _client;
        private CommandService _commands;
        private string _helpstring;

        private readonly string WDASheetId;
        private readonly string DiceFightSheetId;
        private readonly string TotMSheetId;

        private const string EVENT_WDA = "weekly-dice-arena";
        private const string EVENT_DICE_FIGHT = "dice-fight";
        private const string EVENT_TOTM = "team-of-the-month";

        private const string SUBMIT_STRING = "!submit";


        public DiscordBot(ILoggerFactory loggerFactory, IConfiguration config)
        {
            Logger = loggerFactory.CreateLogger<DiscordBot>();
            Config = config;

            WDASheetId = config["WeeklyDiceArenaSheetId"];
            DiceFightSheetId = config["DiceFightSheetId"];
            TotMSheetId = config["TeamOfTheMonthSheetId"];
        }

        public ILogger Logger { get; }
        public IConfiguration Config { get; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Logger.LogInformation("ServiceA is starting.");

            stoppingToken.Register(() => Logger.LogInformation("ServiceA is stopping."));

            try
            {
                _client = new DiscordSocketClient();

                _client.Log += Log;

                //Initialize command handling.
                _client.MessageReceived += DiscordMessageReceived;
                //await InstallCommands();      

                // Connect the bot to Discord
                string token = Config["DiscordToken"];
                await _client.LoginAsync(TokenType.Bot, token);
                await _client.StartAsync();

                // Block this task until the program is closed.
                await Task.Delay(-1);

                while (!stoppingToken.IsCancellationRequested)
                {
                    Logger.LogInformation("ServiceA is doing background work.");

                    await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
                }
            } catch (Exception exc)
            {
                Logger.LogError(exc.Message);
            }

            Logger.LogInformation("ServiceA has stopped.");
        }

        private async Task DiscordMessageReceived(SocketMessage message)
        {
            var dmchannelID = await message.Author.GetOrCreateDMChannelAsync();
            if (message.Channel.Id == dmchannelID.Id)
            {
                if (message.Content.ToLower().StartsWith("submit") || message.Content.ToLower().StartsWith("!submit"))
                {
                    try
                    {
                        var eventName = "undetermined";
                        var args = message.Content.Split(' ');

                        switch (args[1].ToUpper())
                        {
                            case "WDA":
                            case "WEEKLY-DICE-ARENA":
                                eventName = EVENT_WDA;
                                break;
                            case "DF":
                            case "DICE-FIGHT":
                                eventName = EVENT_DICE_FIGHT;
                                break;
                            case "TOTM":
                            case "TEAM-OF-THE-MONTH":
                                eventName = EVENT_TOTM;
                                break;
                            default:
                                break;
                        }

                        await message.Channel.SendMessageAsync(SubmitTeamLink(eventName, args[2], message));
                    }
                    catch (Exception exc)
                    {
                        Console.WriteLine($"Exception submitting team via DM: {exc.Message}");
                        await message.Channel.SendMessageAsync("Sorry, I was unable to determine which event you wanted to submit to.");
                        await message.Channel.SendMessageAsync(DMBotSubmitTeamHint);
                        await message.Channel.SendMessageAsync("If you think you're doing it right and it still doesn't work, message Yort or post in #bot-uprising");
                    }
                }
            }
            if (message.Content == "!ping")
            {
                await message.Channel.SendMessageAsync("Pong!");
            }
            else if(message.Content.StartsWith(SUBMIT_STRING))
            {
                // first delete the original message
                await message.Channel.DeleteMessageAsync(message);
                var teamlink = message.Content.TrimStart(SUBMIT_STRING.ToCharArray()).Trim();
                await message.Channel.SendMessageAsync(SubmitTeamLink(message.Channel.Name, teamlink, message));
            }
            else if (message.Content.StartsWith("!format"))
            {
                await message.Channel.SendMessageAsync(GetCurrentFormat(message));
            }
            else if (message.Content.StartsWith("!count"))
            {
                await message.Channel.SendMessageAsync(GetCurrentPlayerCount(message));
            }
            else if (message.Content.StartsWith("!help"))
            {
                await message.Channel.SendMessageAsync(DMBotCommandHelpString);
            }
            //else if(message.Content.StartsWith("!"))
            //{
            //    await message.Channel.SendMessageAsync("No matching command found");
            //}
        }

        private string GetCurrentPlayerCount(SocketMessage message)
        {
            var sheetsService = AuthorizeGoogleSheets();
            string sheetId;
            string sheetName;
            if (message.Channel.Name == "weekly-dice-arena")
            {
                sheetId = WDASheetId;
                sheetName = WdaSheetName;
            }
            else if (message.Channel.Name == "dice-fight")
            {
                sheetId = DiceFightSheetId;
                sheetName = DiceFightSheetName;
            }
            else if (message.Channel.Name == "team-of-the-month")
            {
                sheetId = TotMSheetId;
                sheetName = TotMSheetName;
            }
            else
            {
                return "Sorry, can't do that on this channel";
            }

            try
            {
                // Define request parameters.
                var range = $"{sheetName}!A:B";

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

        private string GetCurrentFormat(SocketMessage message)
        {
            var sheetsService = AuthorizeGoogleSheets();

            if (message.Channel.Name == "weekly-dice-arena")
            {
                return GetFormatFromGoogle(sheetsService, message, WDASheetId, WdaSheetName);
            }
            else if (message.Channel.Name == "dice-fight")
            {
                return GetFormatFromGoogle(sheetsService, message, DiceFightSheetId, DiceFightSheetName);
            }
            else if (message.Channel.Name == "team-of-the-month")
            {
                return GetFormatFromGoogle(sheetsService, message, TotMSheetId, TotMSheetName);
            }
            return "I can't accept team links on this channel";
        }

        private string GetFormatFromGoogle(SheetsService sheetsService, SocketMessage message, string SpreadsheetId, string sheet)
        {
            try
            {
                // Define request parameters.
                var userName = message.Author.Username;
                var range = $"{sheet}!A:B";

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

        private string SubmitTeamLink(string eventName, string teamlink, SocketMessage message)
        {
             var sheetsService = AuthorizeGoogleSheets();
            if(eventName == "weekly-dice-arena")
            {
                return SendLinkToGoogle(sheetsService, message, WDASheetId, WdaSheetName, teamlink);
            }
            else if (eventName == "dice-fight")
            {
                return SendLinkToGoogle(sheetsService, message, DiceFightSheetId, DiceFightSheetName, teamlink);
            }
            else if(eventName == "team-of-the-month")
            {
                return SendLinkToGoogle(sheetsService, message, TotMSheetId, TotMSheetName, teamlink);
            }
            return DMBotSubmitTeamHint;
        }

        private string SendLinkToGoogle(SheetsService sheetsService, SocketMessage message, string SpreadsheetId, string sheet, string teamlink)
        {
            try
            {
                // Define request parameters.
                var userName = message.Author.Username;
                var range = $"{sheet}!A:B";
                // strip off any <>s if people included them
                teamlink = teamlink.TrimStart('<').TrimEnd('>');

                // first check to see if this person already has a submission
                var checkExistingRequest = sheetsService.Spreadsheets.Values.Get(SpreadsheetId, range);
                var existingRecords = checkExistingRequest.Execute();
                bool existingEntryFound = false;
                foreach (var record in existingRecords.Values)
                {
                    if(record.Contains(userName))
                    {
                        var index = existingRecords.Values.IndexOf(record);
                        range = $"{sheet}!A{index+1}";
                        existingEntryFound = true;
                        break;
                    }
                }

                var oblist = new List<object>() { userName, teamlink };
                var valueRange = new ValueRange();
                valueRange.Values = new List<IList<object>> { oblist };

                if (existingEntryFound)
                {
                    // Performing Update Operation...
                    var updateRequest = sheetsService.Spreadsheets.Values.Update(valueRange, SpreadsheetId, range);
                    updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                    var appendReponse = updateRequest.Execute();
                    return $"Thanks {userName}, your team was updated!";
                }
                else
                {
                    // Append the above record...
                    var appendRequest = sheetsService.Spreadsheets.Values.Append(valueRange, SpreadsheetId, range);
                    appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
                    var appendReponse = appendRequest.Execute();
                    return $"Thanks {userName}, your team was added!";
                }
            } 
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
                return "There was an error trying to add your team";
            }
        }

        private SheetsService AuthorizeGoogleSheets()
        {
            try
            {
                string[] Scopes = { SheetsService.Scope.Spreadsheets };
                string ApplicationName = $"Dice Masters Online Helper Bot";
                string googleCredentialJson = Config["GoogleCredentials"];

                GoogleCredential credential;
                credential = GoogleCredential.FromJson(googleCredentialJson).CreateScoped(Scopes);
                //Reading Credentials File...
                //using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
                //{
                //    credential = GoogleCredential.FromStream(stream)
                //        .CreateScoped(Scopes);
                //}

                // Create Google Sheets API service.
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

        private string WdaSheetName
        {
            get
            {
                DateTime today = DateTime.Today;
                DateTime nextDate;
                if (today.DayOfWeek == DayOfWeek.Tuesday)
                {
                    nextDate = today;
                }
                else
                {
                    int diff = (7 + (today.DayOfWeek - DayOfWeek.Tuesday)) % 7;
                    nextDate = DateTime.Today.AddDays(-1 * diff).AddDays(7).Date;
                }
                return $"{today.Year}-{nextDate.ToString("MMMM")}-{nextDate.Day}";
            }
        }

        private string DiceFightSheetName
        {
            get
            {
                DateTime today = DateTime.Today;
                DateTime nextDate;
                if (today.DayOfWeek == DayOfWeek.Thursday)
                {
                    nextDate = today;
                }
                else
                {
                    int diff = (7 + (today.DayOfWeek - DayOfWeek.Thursday)) % 7;
                    nextDate = DateTime.Today.AddDays(-1 * diff).AddDays(7).Date;
                }
                return $"{today.Year}-{nextDate.ToString("MMMM")}-{nextDate.Day}";
            }
        }

        private string TotMSheetName
        {
            get
            {
                DateTime today = DateTime.Now;
                return $"{today.Year}-{today.ToString("MMMM")}";
            }
        }

        public string DMBotSubmitTeamHint 
        { 
            get
            {
                return $"Please send a Direct Message to the Dice Masters Bot with the format \"submit [event] [teambuilder link]\" where [event] is{Environment.NewLine}Weekly Dice Arena: WDA{Environment.NewLine}Dice Fight: DF{Environment.NewLine}Team of the Month: TOTM";
            }
        }

        public string DMBotCommandHelpString
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_helpstring))
                {
                    StringBuilder helpString = new StringBuilder();
                    var nl = Environment.NewLine;
                    helpString.Append("The Dice Masters Bot currently supports the following commands:");
                    helpString.Append($"{nl}WITHIN A CHANNEL:");
                    helpString.Append($"{nl}    !format - returns the current format for that channel's event");
                    helpString.Append($"{nl}    !submit <teambuilder link> - submits your team for the event. Your link will be immediately deleted so others can't see it.");
                    helpString.Append($"{nl}VIA DIRECT MESSAGE - you can also send the Dice Masters Bot a direct message");
                    helpString.Append($"{nl}    !submit/submit <event> <teambuilder link> - current supported events are wda (Weekly Dice Arena), df (Dice Fight) and totm (Team of the Month)");
                    helpString.Append($"{nl}If you have any problems or just general feedback, please DM Yort.");
                    _helpstring = helpString.ToString();
                }
                return _helpstring;
            }
        }

        //public async Task InstallCommands()
        //{
        //    _client.MessageReceived += CommandHandler;
        //    await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), null);
        //}

        private Task Log(LogMessage msg)
        {
            Logger.LogDebug(msg.ToString());
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
