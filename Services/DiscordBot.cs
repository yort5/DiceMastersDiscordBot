using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DiceMastersDiscordBot.Entities;
using DiceMastersDiscordBot.Properties;
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
        private readonly ILogger _logger;
        private readonly IAppSettings _settings;
        private DiscordSocketClient _client;
        private ChallongeEvent _challonge;
        //private CommandService _commands;


        private readonly DMSheetService _sheetService;

        private const string SUBMIT_STRING = "!submit";

        
        public DiscordBot(ILoggerFactory loggerFactory,
                            IAppSettings appSettings,
                            DMSheetService dMSheetService,
                            ChallongeEvent challonge)
        {
            _logger = loggerFactory.CreateLogger<DiscordBot>();
            _settings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _sheetService = dMSheetService;
            _challonge = challonge;
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DiscordBot Service is starting.");

            stoppingToken.Register(() => _logger.LogInformation("DiscordBot Service is stopping."));

            try
            {
                _client = new DiscordSocketClient();

                _client.Log += Log;

                //Initialize command handling.
                _client.MessageReceived += DiscordMessageReceived;
                //await InstallCommands();      

                // Connect the bot to Discord
                string token = _settings.GetDiscordToken();
                await _client.LoginAsync(TokenType.Bot, token);
                await _client.StartAsync();

                // Block this task until the program is closed.
                //await Task.Delay(-1, stoppingToken);

                while (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("ServiceA is doing background work.");

                    _sheetService.CheckSheets();

                    await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
                }
            } catch (Exception exc)
            {
                _logger.LogError(exc.Message);
            }

            _logger.LogInformation("ServiceA has stopped.");
        }

        private async Task DiscordMessageReceived(SocketMessage message)
        {
            if (message.Author.IsBot) return;
            var dmchannelID = await message.Author.GetOrCreateDMChannelAsync();
            if (message.Channel.Id == dmchannelID.Id)
            {
                if (message.Content.ToLower().StartsWith("submit") || message.Content.ToLower().StartsWith("!submit") || message.Content.ToLower().StartsWith(".submit"))
                {
                    try
                    {
                        var eventName = "undetermined";
                        var args = message.Content.Split(' ');

                        var inputCode = args[1].ToUpper();

                        if(inputCode == "WDA" || inputCode == "WEEKLY-DICE-ARENA")
                        {
                            eventName = _settings.GetWDAChannelName();
                        }
                        else if(inputCode == "DF" || inputCode == "DICE-FIGHT")
                        {
                            eventName = _settings.GetDiceFightChannelName();
                        }
                        else if(inputCode == "TOTM" || inputCode == "TEAM-OF-THE-MONTH")
                        {
                            eventName = _settings.GetTotMChannelName();
                        }
                        else if(inputCode == _settings.GetOneOffCode())
                        {
                            eventName = _settings.GetOneOffChannelName();
                        }

                        await message.Channel.SendMessageAsync(SubmitTeamLink(eventName, args[2], message));
                    }
                    catch (Exception exc)
                    {
                        Console.WriteLine($"Exception submitting team via DM: {exc.Message}");
                        await message.Channel.SendMessageAsync("Sorry, I was unable to determine which event you wanted to submit to.");
                        await message.Channel.SendMessageAsync("Type `.help` for more information.");
                        await message.Channel.SendMessageAsync("If you think you're doing it right and it still doesn't work, message Yort or post in #bot-uprising");
                    }
                }
                else if (message.Content.ToLower().StartsWith("!win") || message.Content.ToLower().StartsWith(".win"))
                {
                    try
                    {
                        var sheetsService = _sheetService.AuthorizeGoogleSheets();
                        string[] args = message.Content.Split(" ");
                        UserInfo userInfo = new UserInfo() { DiscordName = message.Author.Username, WINName = args[1].ToString() };
                        _sheetService.SendUserInfoToGoogle(userInfo);
                        // record it in the spreadsheet
                        //await message.Channel.SendMessageAsync(
                        //    _sheetService.SendLinkToGoogle(sheetsService, message, _settings.GetMasterSheetId(), "WINSheet", userInfo));
                        //await message.Channel.SendMessageAsync("Also, I hope to do this automatically soon, but I am already up to late, so would you mind terribly re-submitting your team so your WIN name gets recorded properly? Thanks, I reaZZZZZZZZZ");
                        // update current DiceFight sheet?

                    }
                    catch (Exception exc)
                    {
                        Console.WriteLine($"Exception in UpdateWinName: {exc.Message}");
                        await message.Channel.SendMessageAsync("Sorry, I was unable to record your WIN Name. Please contact Yort and tell him what went wrong");
                    }
                }
                else
                {
                    await message.Channel.SendMessageAsync("Sorry, I don't understand that command. I'm not actually that smart, you know.");
                }
                return;
            }
            if (message.Content == "!ping")
            {
                await message.Channel.SendMessageAsync("Pong!");
            }
            else if (message.Content.ToLower().StartsWith(SUBMIT_STRING) || message.Content.ToLower().StartsWith(".submit"))
            {
                // first delete the original message
#if DEBUG
                await message.Channel.DeleteMessageAsync(message);
#endif
                var teamlink = message.Content.TrimStart(SUBMIT_STRING.ToCharArray()).TrimStart(".submit".ToCharArray()).Trim();
                await message.Channel.SendMessageAsync(SubmitTeamLink(message.Channel.Name, teamlink, message));
            }
            else if (message.Content.ToLower().StartsWith(".format") || message.Content.ToLower().StartsWith("!format"))
            {
                await message.Channel.SendMessageAsync(GetCurrentFormat(message));
            }
            else if (message.Content.ToLower().StartsWith(".count") || message.Content.ToLower().StartsWith("!count"))
            {
                var playerList = _sheetService.GetCurrentPlayerCount(message.Channel.Name);
                await message.Channel.SendMessageAsync($"There are currently {playerList} humans registered (and no robots)");
            }
            else if (message.Content.ToLower().StartsWith(".list") || message.Content.ToLower().StartsWith("!list"))
            {
                var playerList = _sheetService.GetCurrentPlayerList(message.Channel.Name);
                StringBuilder playerListString = new StringBuilder();
                playerListString.AppendLine($"There are currently {playerList.Count} humans registered (and no robots):");
                foreach(var player in playerList)
                {
                    playerListString.AppendLine(player.DiscordName);
                }
                await message.Channel.SendMessageAsync(playerListString.ToString()) ;
            }
            else if (message.Content.ToLower().StartsWith(".here") || message.Content.ToLower().StartsWith("!here"))
            {
                EventUserInput eventUserInput = new EventUserInput() { DiscordName = message.Author.Username, EventName = message.Channel.Name };
                if(_sheetService.MarkPlayerHere(eventUserInput))
                {
                    await message.Channel.SendMessageAsync($"Player {eventUserInput.DiscordName} marked as HERE in the spreadsheet");
                }
                else
                {
                    await message.Channel.SendMessageAsync($"Sorry, could not mark {eventUserInput.DiscordName} as HERE as they were not found in the spreadsheet for this event.");
                }
            }
            else if (message.Content.ToLower().StartsWith(".drop") || message.Content.ToLower().StartsWith("!drop"))
            {
                EventUserInput eventUserInput = new EventUserInput() { DiscordName = message.Author.Username, EventName = message.Channel.Name };
                if (_sheetService.MarkPlayerDropped(eventUserInput))
                {
                    await message.Channel.SendMessageAsync($"Player {eventUserInput.DiscordName} marked as DROPPED in the spreadsheet");
                }
                else
                {
                    await message.Channel.SendMessageAsync($"Sorry, could not mark {eventUserInput.DiscordName} as DROPPED as they were not found in the spreadsheet for this event.");
                }
            }
            else if (message.Content.ToLower().StartsWith(".teams"))
            {
                //await message.Channel.SendMessageAsync(_sheetService.ListTeams(message));
            }
            else if (message.Content.ToLower().StartsWith(".help") || message.Content.ToLower().StartsWith("!help"))
            {
                await message.Channel.SendMessageAsync(_settings.GetBotHelpString());
            }
            else if (message.Content.StartsWith("!test"))
            {
                var participants = await _challonge.GetAllParticipantsAsync("3a5g0n90");
                _logger.LogDebug($"{participants.Count}");
            }
        }



        private string GetCurrentFormat(SocketMessage message)
        {
            var homeSheet = _sheetService.GetHomeSheet(message.Channel.Name);
            //var sheetsService = _sheetService.AuthorizeGoogleSheets();

                try
                {
                    if(homeSheet ==  null) return "No information found for this week yet";

                    var nl = Environment.NewLine;
                var eventName = homeSheet.EventName != null ? string.Format($"{homeSheet.EventName}{nl}") : string.Empty;
                    return $"{eventName}**{homeSheet.EventDate}**{nl}__Format__ - {homeSheet.FormatDescription}{nl}__Additional info:__{nl}{homeSheet.Info}";
                }
                catch (Exception exc)
                {
                    Console.WriteLine($"Exception in GetCurrentFormat: {exc.Message}");
                    return $"Ooops! Something went wrong! - please contact Yort (bot)";
                }
        }

         private string SubmitTeamLink(string eventName, string teamlink, SocketMessage message)
        {
            var sheetsService = _sheetService.AuthorizeGoogleSheets();
            EventUserInput eventUserInput = new EventUserInput();
            string response = string.Empty;
            HomeSheet homeSheet = _sheetService.GetHomeSheet(eventName);
            if (homeSheet == null) return "Sorry, there was an error getting the event info to submit to";

            eventUserInput.Here = DateTime.UtcNow.ToString();
            eventUserInput.DiscordName = message.Author.Username;
            eventUserInput.TeamLink = teamlink;

            response = _sheetService.SendLinkToGoogle(sheetsService, homeSheet.SheetId, homeSheet.SheetName, eventUserInput);

            if(string.IsNullOrEmpty(response))
            {
                return _settings.GetBotHelpString();
            }
            else
            {
                message.Author.SendMessageAsync($"The following team was successfully submitted for {eventName}");
                message.Author.SendMessageAsync(teamlink);
            }
            return response;
        }




        //public async Task InstallCommands()
        //{
        //    _client.MessageReceived += CommandHandler;
        //    await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), null);
        //}

        private Task Log(LogMessage msg)
        {
            _logger.LogDebug(msg.ToString());
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
