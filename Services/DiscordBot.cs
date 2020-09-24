using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ChallongeSharp.Models.ViewModels;
using ChallongeSharp.Models.ViewModels.Types;
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
                        await message.Channel.SendMessageAsync(_sheetService.SendUserInfoToGoogle(userInfo));

                    }
                    catch (Exception exc)
                    {
                        Console.WriteLine($"Exception in UpdateWinName: {exc.Message}");
                        await message.Channel.SendMessageAsync("Sorry, I was unable to record your WIN Name. Please contact Yort and tell him what went wrong");
                    }
                }
                else if (message.Content.ToLower().StartsWith("!challonge") || message.Content.ToLower().StartsWith(".challonge"))
                {
                    try
                    {
                        var sheetsService = _sheetService.AuthorizeGoogleSheets();
                        string[] args = message.Content.Split(" ");
                        UserInfo userInfo = new UserInfo() { DiscordName = message.Author.Username, ChallongeName = args[1].ToString() };
                        await message.Channel.SendMessageAsync(_sheetService.SendUserInfoToGoogle(userInfo));
                    }
                    catch (Exception exc)
                    {
                        Console.WriteLine($"Exception in UpdateWinName: {exc.Message}");
                        await message.Channel.SendMessageAsync("Sorry, I was unable to record your Challonge Name. Please contact Yort and tell him what went wrong");
                    }
                }
                else if (message.Content.ToLower().StartsWith("!twitch") || message.Content.ToLower().StartsWith(".twitch"))
                {
                    try
                    {
                        var sheetsService = _sheetService.AuthorizeGoogleSheets();
                        string[] args = message.Content.Split(" ");
                        UserInfo userInfo = new UserInfo() { DiscordName = message.Author.Username, TwitchName = args[1].ToString() };
                        await message.Channel.SendMessageAsync(_sheetService.SendUserInfoToGoogle(userInfo));
                    }
                    catch (Exception exc)
                    {
                        Console.WriteLine($"Exception in UpdateWinName: {exc.Message}");
                        await message.Channel.SendMessageAsync("Sorry, I was unable to record your Twitch Name. Please contact Yort and tell him what went wrong");
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
                await message.Channel.DeleteMessageAsync(message);
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
                await GetCurrentPlayerList(message);
            }
            else if (message.Content.ToLower().StartsWith(".report") || message.Content.ToLower().StartsWith("!report"))
            {
                await RecordScore(message);
            }
            else if (message.Content.ToLower().StartsWith(".here") || message.Content.ToLower().StartsWith("!here"))
            {
                EventUserInput eventUserInput = new EventUserInput() { DiscordName = message.Author.Username, EventName = message.Channel.Name };
                if (message.Channel.Name == _settings.GetOneOffChannelName())
                {
                    var userInfo = _sheetService.GetUserInfoFromDiscord(message.Author.Username);
                    if (string.IsNullOrEmpty(userInfo.ChallongeName))
                    {
                        await message.Channel.SendMessageAsync($"Cannot check in Discord user {message.Author.Username} as there is no mapped Challonge ID. Please use `.challonge mychallongeusername` to tell the {_settings.GetBotName()} who you are in Challonge.");
                    }
                    else
                    {
                        var participants = await _challonge.GetAllParticipantsAsync(_settings.GetOneOffChallongeId());
                        var player = participants.SingleOrDefault(p => p.ChallongeUsername == userInfo.ChallongeName);
                        if(player == null)
                        {
                            await message.Channel.SendMessageAsync($"There was an error checking in Challonge User {userInfo.ChallongeName} - they were not returned as registered for this tournament.");
                        }
                        else
                        {
                            var resultParticipant = await _challonge.CheckInParticipantAsync(player.Id.ToString(), _settings.GetOneOffChallongeId());
                            if (resultParticipant.CheckedIn == true)
                            {
                                await message.Channel.SendMessageAsync($"Success! Challonge User {userInfo.ChallongeName} (Discord: {userInfo.DiscordName}) is checked in for the event!");
                            }
                            else
                            {
                                await message.Channel.SendMessageAsync($"There was an error checking in Challonge User {userInfo.ChallongeName} - please check in manually at Challonge.com");
                            }
                        }
                    }

                }
                else
                {
                    if (_sheetService.MarkPlayerHere(eventUserInput))
                    {
                        await message.Channel.SendMessageAsync($"Player {eventUserInput.DiscordName} marked as HERE in the spreadsheet");
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync($"Sorry, could not mark {eventUserInput.DiscordName} as HERE as they were not found in the spreadsheet for this event.");
                    }
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
            else if (message.Content.ToLower().StartsWith(".register"))
            {
                await message.Channel.SendMessageAsync("This event is not enabled for auto-registration. Please register manually.");
                //if(await RegisterForChallonge(message))
                //{
                //    await message.Channel.SendMessageAsync($"Thanks {message.Author.Username}, you are registered for the event in Challonge!");
                //}
            }
            else if (message.Content.ToLower().StartsWith("!win") || message.Content.ToLower().StartsWith(".win"))
            {
                try
                {
                    var sheetsService = _sheetService.AuthorizeGoogleSheets();
                    string[] args = message.Content.Split(" ");
                    UserInfo userInfo = new UserInfo() { DiscordName = message.Author.Username, WINName = args[1].ToString() };
                    await message.Channel.SendMessageAsync(_sheetService.SendUserInfoToGoogle(userInfo));

                }
                catch (Exception exc)
                {
                    Console.WriteLine($"Exception in UpdateWinName: {exc.Message}");
                    await message.Channel.SendMessageAsync("Sorry, I was unable to record your WIN Name. Please contact Yort and tell him what went wrong");
                }
            }
            else if (message.Content.ToLower().StartsWith("!challonge") || message.Content.ToLower().StartsWith(".challonge"))
            {
                try
                {
                    var sheetsService = _sheetService.AuthorizeGoogleSheets();
                    string[] args = message.Content.Split(" ");
                    UserInfo userInfo = new UserInfo() { DiscordName = message.Author.Username, ChallongeName = args[1].ToString() };
                    await message.Channel.SendMessageAsync(_sheetService.SendUserInfoToGoogle(userInfo));
                }
                catch (Exception exc)
                {
                    Console.WriteLine($"Exception in UpdateWinName: {exc.Message}");
                    await message.Channel.SendMessageAsync("Sorry, I was unable to record your Challonge Name. Please contact Yort and tell him what went wrong");
                }
            }
            else if (message.Content.ToLower().StartsWith("!twitch") || message.Content.ToLower().StartsWith(".twitch"))
            {
                try
                {
                    var sheetsService = _sheetService.AuthorizeGoogleSheets();
                    string[] args = message.Content.Split(" ");
                    UserInfo userInfo = new UserInfo() { DiscordName = message.Author.Username, TwitchName = args[1].ToString() };
                    await message.Channel.SendMessageAsync(_sheetService.SendUserInfoToGoogle(userInfo));
                }
                catch (Exception exc)
                {
                    Console.WriteLine($"Exception in UpdateWinName: {exc.Message}");
                    await message.Channel.SendMessageAsync("Sorry, I was unable to record your Twitch Name. Please contact Yort and tell him what went wrong");
                }
            }
            else if (message.Content.ToLower().StartsWith(".help") || message.Content.ToLower().StartsWith("!help"))
            {
                await message.Channel.SendMessageAsync(_settings.GetBotHelpString());
            }
            else if (message.Content.StartsWith("!test"))
            {
                var participants = await _challonge.GetAllParticipantsAsync(_settings.GetOneOffChallongeId());
                _logger.LogDebug($"{participants.Count}");
            }
        }

        private async Task RecordScore(SocketMessage message)
        {
            if (message.Channel.Name == _settings.GetOneOffChannelName())
            {
                try
                {

                    var scoreChannel = _client.GetChannel(_settings.GetScoresChannelId()) as IMessageChannel;
                    //await scoreChannel.SendMessageAsync(message.Content.Replace(".report ", ""));

                    var argOld = message.Content.Split(" ");

                    var args = System.Text.RegularExpressions.Regex.Split(message.Content, @"\s+");


                    if (args.Count() >= 5)
                    {
                        var firstPlayerArg = args[1];
                        var secondPlayerArg = args[3];
                        var score = args[4];

                        var firstPlayerDiscordUser = message.MentionedUsers.FirstOrDefault(u => u.Mention == firstPlayerArg);
                        var secondPlayerDiscordUser = message.MentionedUsers.FirstOrDefault(u => u.Mention == secondPlayerArg);

                        var firstPlayerInfo = _sheetService.GetUserInfoFromDiscord(firstPlayerDiscordUser != null ? firstPlayerDiscordUser.Username : firstPlayerArg);
                        var secondPlayerInfo = _sheetService.GetUserInfoFromDiscord(secondPlayerDiscordUser != null ? secondPlayerDiscordUser.Username : secondPlayerArg);

                        //await message.Channel.SendMessageAsync($"Attempting to report scores for Challonge users {firstPlayerInfo.ChallongeName} and {secondPlayerInfo.ChallongeName}...");

                        var allPlayersChallongeInfo = await _challonge.GetAllParticipantsAsync(_settings.GetOneOffChallongeId());
                        var firstPlayerChallongeInfo = allPlayersChallongeInfo.FirstOrDefault(p => p.ChallongeUsername == firstPlayerInfo.ChallongeName);
                        //var firstPlayerChallongeInfo = allPlayersChallongeInfo.FirstOrDefault(p => p.Name == firstPlayerInfo.ChallongeName);
                        var secondPlayerChallongeInfo = allPlayersChallongeInfo.FirstOrDefault(p => p.ChallongeUsername == secondPlayerInfo.ChallongeName);
                        //var secondPlayerChallongeInfo = allPlayersChallongeInfo.FirstOrDefault(p => p.Name == secondPlayerInfo.ChallongeName);
                        var allMatches = await _challonge.GetAllMatchesAsync(_settings.GetOneOffChallongeId());

                        var openMatches = allMatches.Where(m => m.State == "open");

                        if (openMatches.Count() > 1)
                        {
                            bool playerOneisOne = true;
                            var possibleMatch = allMatches.Where(m => m.Player1Id == firstPlayerChallongeInfo.Id && m.Player2Id == secondPlayerChallongeInfo.Id);
                            if (!possibleMatch.Any())
                            {
                                playerOneisOne = false;
                                possibleMatch = allMatches.Where(m => m.Player1Id == secondPlayerChallongeInfo.Id && m.Player2Id == firstPlayerChallongeInfo.Id);
                            }
                            if (possibleMatch.Any() && possibleMatch.Count() == 1)
                            {
                                int playerOneScore = 0;
                                int playerTwoScore = 0;

                                if (playerOneisOne)
                                {
                                    int.TryParse(score.First<char>().ToString(), out playerOneScore);
                                    int.TryParse(score.Last<char>().ToString(), out playerTwoScore);
                                }
                                else
                                {
                                    int.TryParse(score.First<char>().ToString(), out playerTwoScore);
                                    int.TryParse(score.Last<char>().ToString(), out playerOneScore);
                                }

                                var theMatch = possibleMatch.FirstOrDefault();
                                var result = await _challonge.UpdateMatchAsync(_settings.GetOneOffChallongeId(), theMatch.Id.GetValueOrDefault(), playerOneScore, playerTwoScore);

                                var confirmedWinner = allPlayersChallongeInfo.FirstOrDefault(p => p.Id == result.WinnerId);
                                var confirmedLoser = allPlayersChallongeInfo.FirstOrDefault(p => p.Id == result.LoserId);

                                if (confirmedWinner != null && confirmedLoser != null)
                                {
                                    await message.Channel.SendMessageAsync($"Received verification that Challonge user {confirmedWinner.ChallongeUsername} won over Challonge user {confirmedLoser.ChallongeUsername}");
                                }
                                else
                                {
                                    await message.Channel.SendMessageAsync($"Reported for {firstPlayerInfo.DiscordName} and {secondPlayerInfo.DiscordName}, apparently a tie?");
                                }
                            }
                            else
                            {
                                await message.Channel.SendMessageAsync($"Sorry, unable to retrieve the match information for this pair of players.");
                            }
                        }
                        else
                        {
                            // Because Challonge automatically populates the next bracket as soon as the last score is reported and then you can't change it
                            // don't autoreport the last score, instead send it to the TO to let them do manually
                            ulong toDiscordUserId;
                            ulong.TryParse(_settings.GetOneOffTODiscordID(), out toDiscordUserId);
                            var toDiscordUser = _client.GetUser(toDiscordUserId);
                            string roundString = "?";
                            try
                            {
                                // lazy way to make sure it doesn't blow
                                roundString = openMatches.FirstOrDefault().Round.ToString();
                            }
                            catch { }

                            await toDiscordUser.SendMessageAsync($"Reporting last match results for Round {roundString}:{Environment.NewLine}{message.Content}");
                            await scoreChannel.SendMessageAsync("-----------------");
                        }
                    }
                    else
                    {
                        StringBuilder errMsg = new StringBuilder();
                        errMsg.AppendLine("Sorry, score was not reported, could not parse the message. Please have the losing player report the score using the following format:");
                        errMsg.AppendLine("`.report @winnerDiscordName beat @loserDiscordName 2-1`");
                        await message.Channel.SendMessageAsync(errMsg.ToString());
                    }
                }
                catch (Exception exc)
                {
                    _logger.LogError($"Exception reporting scores: {exc.Message}");
                    await message.Channel.SendMessageAsync($"Aha! You've managed to trip the dreaded EXCEPTION. Don't get too excited, this is beta functionality, it's not that hard! TOs, please verify this score manually!");
                }
            }
            else
            {
                await message.Channel.SendMessageAsync($"Sorry, score reporting is not yet available for this channel.");
            }
        }

        private async Task<bool> RegisterForChallonge(SocketMessage message)
        {
            // first check if we have mapped Challonge info
            bool result = false;
            try
            {
                var userInfo = _sheetService.GetUserInfoFromDiscord(message.Author.Username);
                if (string.IsNullOrEmpty(userInfo.ChallongeName))
                {
                    await message.Author.SendMessageAsync("Please submit your Challonge username info by typing\n `.challonge mychallongeusername`");
                    return false;
                }
                else
                {
                    ParticipantVm challongeParticipant = new ParticipantVm();
                    challongeParticipant.ChallongeUsername = userInfo.ChallongeName;
                    challongeParticipant.Misc = $"Discord: {userInfo.DiscordName}";
                    challongeParticipant.ParticipantName = userInfo.DiscordName;
                    challongeParticipant.Seed = 1;

                    var participantInfo = await _challonge.AddParticipantAsync(challongeParticipant, _settings.GetOneOffChallongeId());
                    await message.Author.SendMessageAsync($"Challonge User {participantInfo.ChallongeUsername} added");
                    result = true;
                }
            }
            catch (Exception exc)
            {
                result = false;
                Console.Write(exc.Message);
            }
            return result;
        }

        private string GetCurrentFormat(SocketMessage message)
        {
            var homeSheet = _sheetService.GetHomeSheet(message.Channel.Name);

            try
            {
                if (homeSheet == null) return "No information found for this week yet";

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

            if (message.Channel.Name == _settings.GetDiceFightChannelName())
            {
                var userInfo = _sheetService.GetUserInfoFromDiscord(message.Author.Username);
                if (string.IsNullOrEmpty(userInfo.WINName))
                {
                    message.Author.SendMessageAsync($"You do not have a WIN name on file. Please submit your WIN login by typing:{Environment.NewLine}`.win mywinlogin`");
                }
                else
                {
                    eventUserInput.Misc = userInfo.WINName;
                }
            }
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


        private async Task GetCurrentPlayerList(SocketMessage message)
        {
            try
            {
                StringBuilder playerListString = new StringBuilder();
                if (message.Channel.Name == _settings.GetOneOffChannelName())
                {
                    await message.Channel.SendMessageAsync("Retrieving list of players registered in Challonge...");
                    // return Challonge list
                    List<UserInfo> userInfos = new List<UserInfo>();
                    var participants = await _challonge.GetAllParticipantsAsync(_settings.GetOneOffChallongeId());
                    var alphaParticipants = participants.OrderBy(p => p.ChallongeUsername);
                    playerListString.AppendLine($"There are currently {participants.Count} humans registered (and no robots):");
                    foreach (var person in alphaParticipants)
                    {
                        var newPerson = _sheetService.GetUserInfoFromChallonge(person.ChallongeUsername);
                        if (newPerson == null || string.IsNullOrEmpty(newPerson.DiscordName))
                        {
                            playerListString.AppendLine(person.ChallongeUsername);
                        }
                        else
                        {
                            playerListString.AppendLine($"{person.ChallongeUsername.PadRight(20)}  (Discord - {newPerson.DiscordName})");
                        }
                    }
                    playerListString.AppendLine("---");
                    playerListString.AppendLine("Note: the first column is the list of usernames from Challonge.");
                    playerListString.AppendLine("If your Challonge name does not have a Discord name in the second column,");
                    playerListString.AppendLine("the bot does not know what your Challonge name is, and will not be able to report your scores.");
                    playerListString.AppendLine("Please let the bot know who you are on Challonge with `.challonge mychallongename`)");
                }
                else
                {
                    var playerList = _sheetService.GetCurrentPlayerList(message.Channel.Name);
                    playerListString.AppendLine($"There are currently {playerList.Count} humans registered (and no robots):");
                    foreach (var player in playerList)
                    {
                        playerListString.AppendLine(player.DiscordName);
                    }
                }
                await message.Channel.SendMessageAsync(playerListString.ToString());
            }
            catch (Exception exc)
            {
                _logger.LogError($"Exception trying to get tournament list from Challonge: {exc.Message}");
                await message.Channel.SendMessageAsync("Sorry, there was an issue getting the player list from Challonge.");
            }
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
