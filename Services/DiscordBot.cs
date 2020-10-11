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
using DiceMastersDiscordBot.Events;
using DiceMastersDiscordBot.Properties;
using Discord;
using Discord.Commands;
using Discord.Net.Rest;
using Discord.Rest;
using Discord.WebSocket;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TwitchLib.Api.Core.RateLimiter;

namespace DiceMastersDiscordBot.Services
{
    public class DiscordBot : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly IAppSettings _settings;
        private DiscordSocketClient _client;
        private ChallongeEvent _challonge;
        private readonly DiceMastersEventFactory _eventFactory;
        //private CommandService _commands;


        private readonly DMSheetService _sheetService;
        private List<EventManifest> _currentEventList = new List<EventManifest>();


        public DiscordBot(ILoggerFactory loggerFactory,
                            IAppSettings appSettings,
                            DMSheetService dMSheetService,
                            DiceMastersEventFactory eventFactory,
                            ChallongeEvent challonge)
        {
            _logger = loggerFactory.CreateLogger<DiscordBot>();
            _settings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _sheetService = dMSheetService;
            _eventFactory = eventFactory;
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

                    LoadCurrentEvents();
                    _sheetService.CheckSheets();

                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            } catch (Exception exc)
            {
                _logger.LogError(exc.Message);
            }

            _logger.LogInformation("ServiceA has stopped.");
        }

        private void LoadCurrentEvents()
        {
            _currentEventList = _sheetService.LoadEventManifests();
        }

        private async Task DiscordMessageReceived(SocketMessage message)
        {
            if(message.Author.IsBot) return;
            if (!(message.Content.StartsWith(".") || message.Content.StartsWith("!"))) return;

            var dmchannelID = await message.Author.GetOrCreateDMChannelAsync();
            bool isDM = (message.Channel.Id == dmchannelID.Id);

            if (message.Content == "!ping")
            {
                await message.Channel.SendMessageAsync("Pong!");
            }
            else if (message.Content.ToLower().StartsWith(".submit") || message.Content.ToLower().StartsWith("!submit"))
            {
                // first delete the original message
                await message.Channel.DeleteMessageAsync(message);
                 await message.Channel.SendMessageAsync(SubmitTeamLink(message, isDM));
            }
            else if (message.Content.ToLower().StartsWith(".format") || message.Content.ToLower().StartsWith("!format"))
            {
                await message.Channel.SendMessageAsync(GetCurrentFormat(message));
            }
            else if (message.Content.ToLower().StartsWith(".count") || message.Content.ToLower().StartsWith("!count"))
            {
                var dmEvent = _eventFactory.GetDiceMastersEvent(message.Channel.Name, _currentEventList);
                int playerList = dmEvent.GetCurrentPlayerCount();
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

                var dmEvent = _eventFactory.GetDiceMastersEvent(message.Channel.Name, _currentEventList);
                var response = await dmEvent.MarkPlayerHereAsync(eventUserInput);
                await message.Channel.SendMessageAsync(response);
            }
            else if (message.Content.ToLower().StartsWith(".drop") || message.Content.ToLower().StartsWith("!drop"))
            {
                EventUserInput eventUserInput = new EventUserInput() { DiscordName = message.Author.Username, EventName = message.Channel.Name };

                var dmEvent = _eventFactory.GetDiceMastersEvent(message.Channel.Name, _currentEventList);
                if (!dmEvent.UsesChallonge)
                {
                    var response = dmEvent.MarkPlayerDropped(eventUserInput);
                    await message.Channel.SendMessageAsync(response);
                }
                else
                {
                    await message.Channel.SendMessageAsync($"Sorry, this event uses Challonge. You must drop yourself from the Challonge event manually");
                }
            }
            else if (message.Content.ToLower().StartsWith(".teams"))
            {
                SendTeams(message);
            }
            else if (message.Content.ToLower().StartsWith(".register"))
            {
                await message.Channel.SendMessageAsync("This event is not enabled for auto-registration. Please register manually.");
                //if(await RegisterForChallonge(message))
                //{
                //    await message.Channel.SendMessageAsync($"Thanks {message.Author.Username}, you are registered for the event in Challonge!");
                //}
            }
            else if (message.Content.ToLower().StartsWith(".fellowship"))
            {
                await message.Channel.DeleteMessageAsync(message);
                RecordFellowship(message);
            }
            else if (message.Content.ToLower().StartsWith("!win") || message.Content.ToLower().StartsWith(".win"))
            {
                try
                {
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
                    string[] args = message.Content.Split(" ");
                    UserInfo userInfo = new UserInfo() { DiscordName = message.Author.Username, ChallongeName = args[1].ToString() };
                    await message.Channel.SendMessageAsync(_sheetService.SendUserInfoToGoogle(userInfo));
                }
                catch (Exception exc)
                {
                    Console.WriteLine($"Exception in UpdateChallongeName: {exc.Message}");
                    await message.Channel.SendMessageAsync("Sorry, I was unable to record your Challonge Name. Please contact Yort and tell him what went wrong");
                }
            }
            else if (message.Content.ToLower().StartsWith("!twitch") || message.Content.ToLower().StartsWith(".twitch"))
            {
                try
                {
                    string[] args = message.Content.Split(" ");
                    UserInfo userInfo = new UserInfo() { DiscordName = message.Author.Username, TwitchName = args[1].ToString() };
                    await message.Channel.SendMessageAsync(_sheetService.SendUserInfoToGoogle(userInfo));
                }
                catch (Exception exc)
                {
                    Console.WriteLine($"Exception in UpdateTwitchName: {exc.Message}");
                    await message.Channel.SendMessageAsync("Sorry, I was unable to record your Twitch Name. Please contact Yort and tell him what went wrong");
                }
            }
            else if (message.Content.ToLower().StartsWith(".help") || message.Content.ToLower().StartsWith("!help"))
            {
                await message.Channel.SendMessageAsync(_settings.GetBotHelpString());
            }
            else if (message.Content.StartsWith("!test"))
            {
                //var participants = await _challonge.GetAllParticipantsAsync(_settings.GetOneOffChallongeId());
                //_logger.LogDebug($"{participants.Count}");
            }
        }

        private async Task RecordScore(SocketMessage message)
        {
            var dmEvent = _eventFactory.GetDiceMastersEvent(message.Channel.Name, _currentEventList);
            var dmManifest = _currentEventList.FirstOrDefault(e => e.EventName == message.Channel.Name);
            string hackTheException = string.Empty;
            if (dmEvent is StandaloneChallongeEvent)
            {
                try
                {
                    // TODO
                    string challongeTournamentName = dmManifest.ChallongeTournamentName;

                    var scoreChannel = _client.GetChannel(dmManifest.ScoreKeeperChannelId) as IMessageChannel;
                    if (scoreChannel != null)
                    {
                        await scoreChannel.SendMessageAsync(message.Content.Replace(".report ", ""));
                    }

                    var argOld = message.Content.Split(" ");

                    var args = System.Text.RegularExpressions.Regex.Split(message.Content, @"\s+");


                    if (args.Count() >= 4)
                    {
                        var firstPlayerArg = args[1];
                        var matchDescription = args[2];
                        var secondPlayerArg = args[3];
                        var score = "1-0";
                        if (matchDescription.ToLower() == "ties" || matchDescription.ToLower() == "tied") score = "1-1";
                        if (args.Count() >= 5)
                        {
                            score = args[4];
                        }

                        var firstPlayerInfo = GetUserFromMention(message, firstPlayerArg);
                        var secondPlayerInfo = GetUserFromMention(message, secondPlayerArg);

                        if (1 == 1) return;

                        //await message.Channel.SendMessageAsync($"Attempting to report scores for Challonge users {firstPlayerInfo.ChallongeName} and {secondPlayerInfo.ChallongeName}...");

                        var allPlayersChallongeInfo = await _challonge.GetAllParticipantsAsync(challongeTournamentName);
                        var firstPlayerChallongeInfo = allPlayersChallongeInfo.FirstOrDefault(p => p.ChallongeUsername == firstPlayerInfo.ChallongeName);
                        //var firstPlayerChallongeInfo = allPlayersChallongeInfo.FirstOrDefault(p => p.Name == firstPlayerInfo.ChallongeName);
                        var secondPlayerChallongeInfo = allPlayersChallongeInfo.FirstOrDefault(p => p.ChallongeUsername == secondPlayerInfo.ChallongeName);
                        //var secondPlayerChallongeInfo = allPlayersChallongeInfo.FirstOrDefault(p => p.Name == secondPlayerInfo.ChallongeName);
                        var allMatches = await _challonge.GetAllMatchesAsync(challongeTournamentName);

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
                                string[] scoreSplit = score.Split('-');
                                if (scoreSplit.Length != 2)
                                {
                                    await message.Channel.SendMessageAsync($"Unable to parse the score: {score}. Reporting Challonge player {firstPlayerInfo.ChallongeName} as winner. Please contact TO if this is incorrect.");
                                    scoreSplit = new string[] { "1", "0" };
                                }

                                if (playerOneisOne)
                                {
                                    int.TryParse(scoreSplit[0], out playerOneScore);
                                    int.TryParse(scoreSplit[1], out playerTwoScore);
                                }
                                else
                                {
                                    int.TryParse(scoreSplit[0], out playerTwoScore);
                                    int.TryParse(scoreSplit[1], out playerOneScore);
                                }

                                var theMatch = possibleMatch.FirstOrDefault();
                                var result = await _challonge.UpdateMatchAsync(challongeTournamentName, theMatch.Id.GetValueOrDefault(), playerOneScore, playerTwoScore);

                                var confirmedWinner = allPlayersChallongeInfo.FirstOrDefault(p => p.Id == result.WinnerId);
                                var confirmedLoser = allPlayersChallongeInfo.FirstOrDefault(p => p.Id == result.LoserId);

                                if (confirmedWinner != null && confirmedLoser != null)
                                {
                                    await message.Channel.SendMessageAsync($"Received verification that Challonge user {confirmedWinner.ChallongeUsername} won over Challonge user {confirmedLoser.ChallongeUsername}");
                                }
                                else
                                {
                                    await message.Channel.SendMessageAsync($"Reported for {firstPlayerInfo.DiscordName} and {secondPlayerInfo.DiscordName} to the tournament organizers to be entered manually in Challonge.");
                                }
                            }
                            else
                            {
                                await message.Channel.SendMessageAsync($"Sorry, unable to retrieve the match information for this pair of players.");
                            }
                        }
                        else
                        {
                            await message.Channel.SendMessageAsync($"Reporting last match of the round for {firstPlayerInfo.DiscordName} and {secondPlayerInfo.DiscordName} for Challonge reasons.");
                            // Because Challonge automatically populates the next bracket as soon as the last score is reported and then you can't change it
                            // don't autoreport the last score, instead send it to the TO to let them do manually
                            string roundString = "?";
                            try
                            {
                                // lazy way to make sure it doesn't blow
                                roundString = openMatches.FirstOrDefault().Round.ToString();
                            }
                            catch { }

                            foreach (var toId in dmManifest.EventOrganizerIDList)
                            {
                                ulong toDiscordUserId;
                                ulong.TryParse(toId, out toDiscordUserId);
                                var toDiscordUser = _client.GetUser(toDiscordUserId);
                                await toDiscordUser.SendMessageAsync($"Reporting last match results for Round {roundString}:{Environment.NewLine}{message.Content}");
                            }
                            if (scoreChannel != null)
                            {
                                await scoreChannel.SendMessageAsync("-----------------");
                            }
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
                    await message.Channel.SendMessageAsync($"Dangit, something done gone wrong. Mighta just been Challonge, don't you worry none. TOs, please verify this score manually!");
                    hackTheException = exc.Message;
                }
                try
                {
                    // hacking this up because I don't have time to get actual logging working
                    if (!string.IsNullOrEmpty(hackTheException))
                    {
                        ulong toDiscordUserId;
                        ulong.TryParse(_settings.GetHackExceptionUser(), out toDiscordUserId);
                        var toDiscordUser = _client.GetUser(toDiscordUserId);
                        await toDiscordUser.SendMessageAsync($"Reporting exception in score submission:{Environment.NewLine}{hackTheException}");
                    }
                }
                catch { }
            }
            else
            {
                await message.Channel.SendMessageAsync($"Sorry, score reporting is not yet available for this channel.");
            }
        }

        private async Task<bool> RegisterForChallonge(SocketMessage message)
        {
            bool result = false;
            try
            {
                var dmEvent = _eventFactory.GetDiceMastersEvent(message.Channel.Name, _currentEventList);
                var dmManifest = _currentEventList.FirstOrDefault(e => e.EventName == message.Channel.Name);
                if (dmEvent is StandaloneChallongeEvent)
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

                        var participantInfo = await _challonge.AddParticipantAsync(challongeParticipant, dmManifest.ChallongeTournamentName);
                        await message.Author.SendMessageAsync($"Challonge User {participantInfo.ChallongeUsername} added");
                        result = true;
                    }
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
            var dmEvent = _eventFactory.GetDiceMastersEvent(message.Channel.Name, _currentEventList);
            return dmEvent.GetFormat();
        }

        private string SubmitTeamLink(SocketMessage message, bool  isDM)
        {
            EventUserInput eventUserInput = new EventUserInput();
            string response = string.Empty;

            eventUserInput.Here = DateTime.UtcNow.ToString();
            eventUserInput.DiscordName = message.Author.Username;
            if (isDM)
            {
                var args = System.Text.RegularExpressions.Regex.Split(message.Content, @"\s+");
                var dmManifest = _currentEventList.FirstOrDefault(e => e.EventCode == args[1]);
                eventUserInput.TeamLink = args[2].Trim('<').Trim('>');
            }
            else
            {
                eventUserInput.TeamLink = message.Content.TrimStart("!submit".ToCharArray()).TrimStart(".submit".ToCharArray()).Trim().Trim('<').Trim('>');
            }

            var dmEvent = _eventFactory.GetDiceMastersEvent(message.Channel.Name, _currentEventList);
            eventUserInput.EventName = dmEvent.ChannelName;
            response = dmEvent.SubmitTeamLink(eventUserInput);

            if(string.IsNullOrEmpty(response))
            {
                return _settings.GetBotHelpString();
            }
            else
            {
                message.Author.SendMessageAsync($"The following team was successfully submitted for {eventUserInput.EventName}{Environment.NewLine}{eventUserInput.TeamLink}");
            }
            return response;
        }

        private async Task GetCurrentPlayerList(SocketMessage message)
        {
            try
            {
                var dmEvent = _eventFactory.GetDiceMastersEvent(message.Channel.Name, _currentEventList);
                
                StringBuilder playerListString = new StringBuilder();
                if(dmEvent.UsesChallonge)
                {
                    await message.Channel.SendMessageAsync("Retrieving list of players registered in Challonge...");
                    var participantList = await dmEvent.GetCurrentPlayerList();
                    playerListString.AppendLine($"There are currently {participantList.Count} humans registered (and no robots):");
                    foreach (var player in participantList.OrderBy(p => p.ChallongeName))
                    {
                        if (string.IsNullOrEmpty(player.DiscordName))
                        {
                            playerListString.AppendLine(player.ChallongeName);
                        }
                        else
                        {
                            playerListString.AppendLine($"{player.ChallongeName.PadRight(20)}  (Discord - {player.DiscordName})");
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
                    var participantList = await dmEvent.GetCurrentPlayerList();
                    playerListString.AppendLine($"There are currently {participantList.Count} humans registered (and no robots):");
                    foreach (var player in participantList.OrderBy(p => p.DiscordName))
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

        private async Task<RestUserMessage> SendTeams(SocketMessage message)
        {
            var dmEvent = _eventFactory.GetDiceMastersEvent(message.Channel.Name, _currentEventList);
            var dmManifest = _currentEventList.FirstOrDefault(e => e.EventName == message.Channel.Name);

            // check TO list
            foreach(var authTo in dmManifest.EventOrganizerIDList)
            {
                // simple check 
                if(message.Author.Id.ToString() == authTo)
                {
                    var teamList = dmEvent.GetTeamLists();
                    if(teamList.Any())
                    {
                        StringBuilder teamListOutput = new StringBuilder();
                        teamListOutput.AppendLine($"Here are the team lists for {dmManifest.EventName}:");
                        foreach(var team in teamList)
                        {
                            teamListOutput.AppendLine($"{team.DiscordName}: {team.TeamLink}");
                        }
                        return await message.Channel.SendMessageAsync(teamListOutput.ToString());

                    }
                }
            }

            // no authorized TO found
            return await message.Channel.SendMessageAsync("Sorry, you are not authorized to list teams for this event.");
        }

        async Task<IUserMessage> RecordFellowship(SocketMessage message)
        {
            var dmEvent = _eventFactory.GetDiceMastersEvent(message.Channel.Name, _currentEventList);
            var dmManifest = _currentEventList.FirstOrDefault(e => e.EventName == message.Channel.Name);

            string fellowshipString = $"FELLOWSHIP {dmManifest.EventName}:{Environment.NewLine}User {message.Author.Username} votes for {message.Content.TrimStart(".fellowship".ToCharArray())}";
            foreach (var toId in dmManifest.EventOrganizerIDList)
            {
                ulong toDiscordUserId;
                ulong.TryParse(toId, out toDiscordUserId);
                var toDiscordUser = _client.GetUser(toDiscordUserId);
                await toDiscordUser.SendMessageAsync(fellowshipString);
            }
            await message.Channel.SendMessageAsync($"Thank you, {message.Author.Username}, your fellowship vote was sent!");
            return await message.Author.SendMessageAsync($"Fellowship vote for {dmManifest.EventName} recorded for: {message.Content.Trim(".fellowship".ToCharArray())}");
        }

        //public async Task InstallCommands()
        //{
        //    _client.MessageReceived += CommandHandler;
        //    await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), null);
        //}

        private UserInfo GetUserFromMention(SocketMessage message, string mentionUserString)
        {
            UserInfo userFromMention = new UserInfo();

            var playerDiscordUser = message.MentionedUsers.FirstOrDefault(u => u.Mention == mentionUserString);
            // sometimes there is a ! in the mention string, sometimes there isn't? Not sure why, so just going to check both scenarios
            if(playerDiscordUser == null)
            {
                var hackMentionString = mentionUserString.Replace("@", "@!");
                playerDiscordUser = message.MentionedUsers.FirstOrDefault(u => u.Mention == mentionUserString);
            }

            // hopefully if we are here we either have a SocketUser, or the string wasn't actually a Discord @mention but just the text of the username
            userFromMention = _sheetService.GetUserInfoFromDiscord(playerDiscordUser != null ? playerDiscordUser.Username : mentionUserString.TrimStart('@'));

            return userFromMention;
        }

        private Task Log(LogMessage msg)
        {
            _logger.LogDebug(msg.ToString());
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
