using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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

        public DiscordBot(ILoggerFactory loggerFactory, IConfiguration config)
        {
            Logger = loggerFactory.CreateLogger<DiscordBot>();
            Config = config;
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
            if (message.Content == "!ping")
            {
                await message.Channel.SendMessageAsync("Pong!");
            }
            else if(message.Content.StartsWith("!teamlink"))
            {
                await message.Channel.SendMessageAsync(SubmitTeamLink(message));
            }
            else if(message.Content.StartsWith("!"))
            {
                await message.Channel.SendMessageAsync("No matching command found");
            }
        }

        private string SubmitTeamLink(SocketMessage message)
        {
             var sheetsService = AuthorizeGoogleSheets(message.Channel.Name);
            if(message.Channel.Name == "weekly-dice-arena")
            {
                String wdasheetid = Config["WeeklyDiceArenaSheetId"];
                DateTime today = DateTime.Now;
                int diff = (7 + (DateTime.Now.DayOfWeek - DayOfWeek.Friday)) % 7;
                int week = (today.AddDays(diff).Date.DayOfYear / 7);
                string weekSheet = $"{today.Year}-Week{week}";
                string sheet = $"{weekSheet}";
                return SendLinkToGoogle(sheetsService, message, wdasheetid, sheet);
            }
            else if(message.Channel.Name == "team-of-the-month")
            {
                String wdasheetid = Config["TeamOfTheMonthSheetId"];
                DateTime today = DateTime.Now;
                string sheet = $"{today.Year}-{today.ToString("MMMM")}";
                return SendLinkToGoogle(sheetsService, message, wdasheetid, sheet);
            }
            return "No logic found for that team link";
        }

        private string SendLinkToGoogle(SheetsService sheetsService, SocketMessage message, string SpreadsheetId, string sheet)
        {
            try
            {
                // Define request parameters.
                var userName = message.Author.Username;
                var range = $"{sheet}!A:B";

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

                string trimString = "!teamlink";
                var teamlink = message.Content.TrimStart(trimString.ToCharArray());
                var oblist = new List<object>() { userName, teamlink };
                var valueRange = new ValueRange();
                valueRange.Values = new List<IList<object>> { oblist };

                if (existingEntryFound)
                {
                    // Performing Update Operation...
                    var updateRequest = sheetsService.Spreadsheets.Values.Update(valueRange, SpreadsheetId, range);
                    updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                    var appendReponse = updateRequest.Execute();
                    return "Team updated!";
                }
                else
                {
                    // Append the above record...
                    var appendRequest = sheetsService.Spreadsheets.Values.Append(valueRange, SpreadsheetId, range);
                    appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
                    var appendReponse = appendRequest.Execute();
                    return "Team added!";
                }
            } 
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
                return "There was an error trying to add your team";
            }
        }

        private SheetsService AuthorizeGoogleSheets(string channel)
        {
            try
            {
                string[] Scopes = { SheetsService.Scope.Spreadsheets };
                string ApplicationName = $"Dice Masters Online {channel}";
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
