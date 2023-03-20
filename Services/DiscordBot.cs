using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.ServiceModel.Syndication;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Azure;
using ChallongeSharp.Models.ViewModels;
using ChallongeSharp.Models.ViewModels.Types;
using DiceMastersDiscordBot.Entities;
using DiceMastersDiscordBot.Events;
using DiceMastersDiscordBot.Properties;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.Net.Rest;
using Discord.Rest;
using Discord.WebSocket;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;

namespace DiceMastersDiscordBot.Services
{
    public class DiscordBot : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly IAppSettings _settings;
        private readonly IWebHostEnvironment _environment;
        private readonly DMSheetService _sheetService;
        private readonly DiceMastersEventFactory _eventFactory;
        private YouTubeMonitorService _youTubeService;
        private ChallongeEvent _challonge;
        //private CommandService _commands;


        private List<EventManifest> _currentEventList = new List<EventManifest>();
        private CommunityInfo _communityInfo = new CommunityInfo();
        private TradeLists _tradeLists = new TradeLists();

        private DiscordSocketClient _client;

        public DiscordBot(ILoggerFactory loggerFactory,
                            IAppSettings appSettings,
                            IWebHostEnvironment environment,
                            DMSheetService dMSheetService,
                            DiceMastersEventFactory eventFactory,
                            YouTubeMonitorService youTubeService,
                            ChallongeEvent challonge)
        {
            _logger = loggerFactory.CreateLogger<DiscordBot>();
            _settings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _sheetService = dMSheetService;
            _eventFactory = eventFactory;
            _youTubeService = youTubeService;
            _challonge = challonge;
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DiscordBot Service is starting.");

            stoppingToken.Register(() => _logger.LogInformation("DiscordBot Service is stopping."));

            try
            {
                var discordConfig = new DiscordSocketConfig { GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent };
                _client = new DiscordSocketClient(discordConfig);

                _client.Log += Log;

                //Initialize command handling.
                _client.Ready += Client_Ready;
                _client.MessageReceived += DiscordMessageReceived;
                _client.SlashCommandExecuted += SlashCommandHandler;
                _client.ModalSubmitted += ModalResponseHandler;

                // Connect the bot to Discord
                string token = _settings.GetDiscordToken();
                await _client.LoginAsync(TokenType.Bot, token);
                await _client.StartAsync();

                // give the service a chance to start before moving on to computational things
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

                var lastUpdatedTicks = DateTime.MinValue.ToUniversalTime().Ticks;
                while (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("DiscordBot is doing background work.");

                    LoadCurrentEvents();
                    CheckRSSFeeds();
                    CheckYouTube();

                    // only do these once a day
                    var currentDayTicks = DateTime.Today.ToUniversalTime().Ticks;
                    if (lastUpdatedTicks < currentDayTicks)
                    {
                        lastUpdatedTicks = currentDayTicks;
                        await LoadCommunityInfo();
                        await LoadTradeLists();
                    }

                    await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
                }
            }
            catch (Exception exc)
            {
                _logger.LogError(exc.Message);
            }

            _logger.LogInformation("ServiceA has stopped.");
        }

        private async Task Client_Ready()
        {
            var cardCommand = new SlashCommandBuilder()
                .WithName("card")
                .WithDescription("Lists out the info for a Dice Masters card.")
                .AddOption("code", ApplicationCommandOptionType.String, "The Team Builder code of the card", isRequired: true);
            await RegisterCommand(cardCommand);

            var formatCommand = new SlashCommandBuilder()
                .WithName("format")
                .WithDescription("Lists out the format for the next event in this channel.")
                .AddOption("number", ApplicationCommandOptionType.String, "How many events in the future you want information for (default is 1)", isRequired: false);
            await RegisterCommand(formatCommand);

            var reportCommand = new SlashCommandBuilder()
                .WithName("report")
                .WithDescription("Report the results of a match.")
                .AddOption("winner", ApplicationCommandOptionType.User, "Discord name of winning player", isRequired: true)
                .AddOption("loser", ApplicationCommandOptionType.User, "Discord name of losing player", isRequired: true);
            await RegisterCommand(reportCommand);

            var submitCommand = new SlashCommandBuilder()
                .WithName("submit")
                .WithDescription("Submit a team for an event.");
            await RegisterCommand(submitCommand);

            var listCommand = new SlashCommandBuilder()
                .WithName("list")
                .WithDescription("List the players registered for an event.");
            await RegisterCommand(listCommand);

            var teamListCommand = new SlashCommandBuilder()
                .WithName("teams")
                .WithDescription("List the teams of players registered for an event.");
            // await RegisterCommand(teamListCommand);

            var tradeCommand = new SlashCommandBuilder()
                .WithName("trade")
                .WithDescription("List a card you have available for trade OR want to trade for.")
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("want")
                    .WithDescription("List a card that you WANT and will trade for or buy.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("code", ApplicationCommandOptionType.String, "The Team Builder code of the card")
                    //.AddOption("foil", ApplicationCommandOptionType.Boolean, "Is this a foil version of the card?")
                    //.AddOption(new SlashCommandOptionBuilder()
                    //    .WithName("type")
                    //    .WithDescription("Are you looking to trade, buy, or either?")
                    //    .AddChoice("Trade", 1)
                    //    .AddChoice("Buy", 2)
                    //    .AddChoice("Either", 3)
                    //    .WithType(ApplicationCommandOptionType.Integer))
                )
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("have")
                    .WithDescription("List a card that you HAVE and want to trade or sell.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("code", ApplicationCommandOptionType.String, "The Team Builder code of the card", isRequired: true)
                    .AddOption("foil", ApplicationCommandOptionType.Boolean, "Is this a foil version of the card?")
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("type")
                        .WithDescription("Are you looking to trade, sell, or either?")
                        .AddChoice("Trade", 1)
                        .AddChoice("Sell", 2)
                        .AddChoice("Either", 3)
                        .WithType(ApplicationCommandOptionType.Integer))
                );
            await RegisterCommand(tradeCommand);

        }

        private async Task RegisterCommand(SlashCommandBuilder cardCommand, bool devOverride = false)
        {
            // Running the registration locally seems to break that registration in the deployed version.
            // Add a switch to turn off registration locally, but allow an override for testing new functionality.
            if (!_environment.IsDevelopment() || devOverride)
            {
                var guildList = _settings.GetServersForSlashCommand(cardCommand.Name);
                foreach (var guildId in guildList)
                {
                    try
                    {
                        var guild = _client.GetGuild(guildId);
                        await guild.CreateApplicationCommandAsync(cardCommand.Build());
                    }
                    catch (HttpException exc)
                    {
                        Console.WriteLine(exc.Message);
                    }
                }
            }
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            switch (command.Data.Name)
            {
                case "card":
                    await HandleCardCommandAsync(command);
                    break;
                case "format":
                    await HandleFormatCommandAsync(command);
                    break;
                case "report":
                    await HandleReportCommandAsync(command);
                    break;
                case "submit":
                    await HandleSubmitCommandAsync(command);
                    break;
                case "list":
                    await HandleListPlayersCommandAsync(command);
                    break;
                case "trade":
                    await HandleTradeCommandAsync(command);
                    break;
            }
        }

        #region Slash Command Handlers
        private async Task HandleCardCommandAsync(SocketSlashCommand command, bool imageOnly = false)
        {
            try
            {
                var cardCode = command.Data.Options.First().Value.ToString();

                var digits = new string(cardCode.Where(s => char.IsDigit(s)).ToArray());
                var letters = new string(cardCode.Where(s => char.IsLetter(s)).ToArray());

                var teamBuilderId = $"{letters}{digits.PadLeft(3, '0')}";

                var communityCardInfo = _communityInfo.Cards.Where(c => c.TeamBuilderId.ToLower() == teamBuilderId.ToLower()).FirstOrDefault();
                var quickanddirty = $"http://dicecoalition.com/cardservice/Image.php?set={letters}&cardnum={digits.TrimStart('0')}";

                if (!imageOnly || communityCardInfo != null)
                {
                    var cardEmbed = new EmbedBuilder
                    {
                        Title = $"{communityCardInfo.CardTitle}",
                        ThumbnailUrl = quickanddirty
                    };

                    cardEmbed
                        .AddField("SubTitle", communityCardInfo.CardSubtitle)
                        .AddField("Ability Text", communityCardInfo.AbilityText)
                        .AddField("Affiliations", communityCardInfo.Affiliation)
                        .AddField("Rarity", communityCardInfo.Rarity, true)
                        //.AddField("Die stats", communityCardInfo.StatLine)
                        .WithFooter(footer => footer.Text = communityCardInfo.TeamBuilderId);

                    await command.RespondAsync(embed: cardEmbed.Build());
                }
                else
                {
                    await command.RespondAsync(quickanddirty);
                }
            }
            catch (Exception exc)
            {
                await command.RespondAsync("Sorry, unable to figure out that card");
            }
        }

        private async Task HandleFormatCommandAsync(SocketSlashCommand command)
        {
            var numberString = command.Data.Options.Any() ? command.Data.Options.First().Value.ToString() : "1";
            var dmEvent = _eventFactory.GetDiceMastersEvent(command.Channel.Name, _currentEventList);
            var numberEvents = 0;
            int.TryParse(numberString, out numberEvents);
            var info = dmEvent.GetFormat(numberEvents);
            await command.RespondAsync(info);
        }

        private async Task HandleSubmitCommandAsync(SocketSlashCommand command)
        {
            var mb = new ModalBuilder()
                .WithTitle("Submit Team")
                .WithCustomId("team_submit")
                .AddTextInput("The TeamBuilder link for your team.", "team_link", placeholder: "http://tb.dicecoalition.com/");

            await command.RespondWithModalAsync(mb.Build());
        }

        private async Task HandleListPlayersCommandAsync(SocketSlashCommand command)
        {
            try
            {
                var dmEvent = _eventFactory.GetDiceMastersEvent(command.Channel.Name, _currentEventList);

                StringBuilder playerListString = new StringBuilder();
                if (dmEvent.UsesChallonge)
                {
                    await command.RespondAsync("Retrieving list of players registered in Challonge...");
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
                await command.RespondAsync(playerListString.ToString());
            }
            catch (Exception exc)
            {
                _logger.LogError($"Exception trying to get tournament list from Challonge: {exc.Message}");
                await command.RespondAsync("Sorry, there was an issue getting the player list from Challonge.");
            }
        }

        private async Task HandleTradeCommandAsync(SocketSlashCommand command)
        {
            var haveOrWant = command.Data.Options.First().Name;
            var cardCode = command.Data.Options.First().Options.First().Value.ToString();
            var isFoil = command.Data.Options.Where(o => o.Name == "foil");
            var type = command.Data.Options.Where(o => o.Name == "type");
            var fullCardInfo = _communityInfo.Cards.FirstOrDefault(c => c.TeamBuilderId.ToLower() == cardCode.ToLower());

            switch (haveOrWant)
            {
                case "have":
                    {
                        var possibleMatches = _tradeLists.Wants.Where(c => c.TeamBuilderId.ToLower() == cardCode.ToLower());
                        if (possibleMatches.Any())
                        {
                            StringBuilder possibleUserMatches = new StringBuilder();
                            var users = string.Join(",", possibleMatches.Select(c => c.DiscordUsername));
                            await command.RespondAsync($"We found the following users may be possible matches: {Environment.NewLine}{users}");
                        }
                        else
                        {
                            await command.RespondAsync("Sorry, no matches found");
                        }
                    }
                    break;
                case "want":
                    {
                        var possibleMatches = _tradeLists.Haves.Where(c => c.TeamBuilderId.ToLower() == cardCode.ToLower());
                        if (possibleMatches.Any())
                        {
                            try
                            {
                                StringBuilder possibleUserMatches = new StringBuilder();
                                var usersString = string.Join(",", possibleMatches.Select(c => c.DiscordUsername));
                                StringBuilder responseString = new StringBuilder();
                                responseString.AppendLine($"{command.User.Username} is looking for {fullCardInfo.TeamBuilderId} - {fullCardInfo.Rarity} {fullCardInfo.CardTitle}");
                                responseString.AppendLine("We found the following users may be possible matches:");
                                foreach (var user in possibleMatches)
                                {
                                    string foil;
                                    if (user.Foil && user.NonFoil) foil = "both foil and non-foil";
                                    else foil = user.Foil ? "foil" : "non-foil";
                                    string trade;
                                    if (user.Trade && user.SellOrBuy) trade = "either trade or sell.";
                                    else trade = user.Trade ? "trade" : "sell";
                                    responseString.AppendLine($"{user.DiscordUsername} has a {foil} they are willing to {trade}");
                                }
                                await command.RespondAsync(responseString.ToString());
                            }
                            catch(Exception exc)
                            {
                                _logger.LogError(exc.Message);
                            }
                        }
                        else
                        {
                            await command.RespondAsync("Sorry, no matches found");
                        }
                    }
                    break;
            }
        }

        private async Task HandleReportCommandAsync(SocketSlashCommand command)
        {
            try
            {
                if(!command.Data.Options.Any() || command.Data.Options.Count < 2)
                {
                    await command.RespondAsync("Error: unable to parse options.");
                }

                var firstPlayerDiscordInfo = (SocketGuildUser)command.Data.Options.First().Value;
                var secondPlayerDiscordInfo = (SocketGuildUser)command.Data.Options.Skip(1).First().Value;

                var dmEvent = _eventFactory.GetDiceMastersEvent(command.Channel.Name, _currentEventList);
                var dmManifest = _currentEventList.FirstOrDefault(e => e.EventName == command.Channel.Name);
                string hackTheException = string.Empty;
                var genericReportString = $"First player (winner) {firstPlayerDiscordInfo.Username}, Second player (loser) {secondPlayerDiscordInfo.Username}";

                if (dmEvent is StandaloneChallongeEvent)
                {
                    // yes, this is hacky, but trying to play and debug the bot at the same time. ;)
                    string lastMark = "Start";
                    string challongeTournamentName = dmManifest.ChallongeTournamentName;

                    var scoreChannel = _client.GetChannel(dmManifest.ScoreKeeperChannelId) as IMessageChannel;
                    if (scoreChannel != null)
                    {
                        await scoreChannel.SendMessageAsync(genericReportString);
                    }

                    var firstPlayerInfo = _sheetService.GetUserInfoFromDiscord(firstPlayerDiscordInfo.Username);
                    var secondPlayerInfo = _sheetService.GetUserInfoFromDiscord(secondPlayerDiscordInfo.Username);

                    var allPlayersChallongeInfo = await _challonge.GetAllParticipantsAsync(challongeTournamentName);
                    var firstPlayerChallongeInfo = allPlayersChallongeInfo.FirstOrDefault(p => p.ChallongeUsername.ToLower() == firstPlayerInfo.ChallongeName.ToLower());
                    var secondPlayerChallongeInfo = allPlayersChallongeInfo.FirstOrDefault(p => p.ChallongeUsername.ToLower() == secondPlayerInfo.ChallongeName.ToLower());
                    lastMark = "Players sorted";

                    var allMatches = await _challonge.GetAllMatchesAsync(challongeTournamentName);
                    var openMatches = allMatches.Where(m => m.State == "open").ToList();

                    lastMark = "Find open match";
                    if (openMatches.Count() > 1)
                    {
                        lastMark = "More than one open match returned";
                        bool playerOneisOne = true;
                        var possibleMatch = allMatches.Where(m => m.Player1Id == firstPlayerChallongeInfo.Id && m.Player2Id == secondPlayerChallongeInfo.Id).ToList();
                        lastMark = "1st possibleMatch";
                        if (!possibleMatch.Any())
                        {
                            lastMark = "1st no possibleMatch";
                            playerOneisOne = false;
                            possibleMatch = allMatches.Where(m => m.Player1Id == secondPlayerChallongeInfo.Id && m.Player2Id == firstPlayerChallongeInfo.Id).ToList();
                            lastMark = "2nd possibleMatch";
                        }
                        if (possibleMatch.Any() && possibleMatch.Count() == 1)
                        {
                            lastMark = "Found the match";
                            var theMatch = possibleMatch.FirstOrDefault();
                            var result = await _challonge.UpdateMatchAsync(challongeTournamentName, theMatch.Id.GetValueOrDefault(), 1, 0);
                            lastMark = "Reported the match";

                            var confirmedWinner = allPlayersChallongeInfo.FirstOrDefault(p => p.Id == result.WinnerId);
                            var confirmedLoser = allPlayersChallongeInfo.FirstOrDefault(p => p.Id == result.LoserId);

                            if (confirmedWinner != null && confirmedLoser != null)
                            {
                                await command.RespondAsync($"Received verification that Challonge user {confirmedWinner.ChallongeUsername} won over Challonge user {confirmedLoser.ChallongeUsername}");
                            }
                            else
                            {
                                await command.RespondAsync($"Reported for {firstPlayerInfo.DiscordName} and {secondPlayerInfo.DiscordName} to the tournament organizers to be entered manually in Challonge.");
                            }
                            lastMark = "Winner and loser reported";
                        }
                        else
                        {
                            await command.RespondAsync($"Sorry, unable to retrieve the match information for this pair of players.");
                        }
                    }
                    else
                    {
                        await command.RespondAsync($"Reporting last match of the round for {firstPlayerInfo.DiscordName} and {secondPlayerInfo.DiscordName} for Challonge reasons.");
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
                            await toDiscordUser.SendMessageAsync($"Reporting last match results for Round {roundString}:{Environment.NewLine}{genericReportString}");
                        }
                        if (scoreChannel != null)
                        {
                            await scoreChannel.SendMessageAsync("-----------------");
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                await command.RespondAsync("Dag Nabbit - there was an error reporting the score. Most of the time it's just Challonge being grouchy, but the TOs will sort it out.");

            }
        }
        #endregion

        #region Modal Response Handlers
        private async Task ModalResponseHandler(SocketModal modal)
        {
            switch (modal.Data.CustomId)
            {
                case "team_submit":
                    await HandleTeamSubmitModalResponseAsync(modal);
                    break;
                //case "card_have":
                //    await HandleCardHaveModalResponseAsync(modal);
                //    break;
                //case "card_want":
                //    await HandleCardWantModalResponseAsync(modal);
                //    break;
            }
        }
        
        private async Task HandleTeamSubmitModalResponseAsync(SocketModal modal)
        {
            List<SocketMessageComponentData> modalComponents = modal.Data.Components.ToList();
            string teamLink = modalComponents.First(x => x.CustomId == "team_link").Value;

            EventUserInput eventUserInput = new EventUserInput();
            eventUserInput.Here = DateTime.UtcNow.ToString();
            eventUserInput.EventName = modal.Channel.Name;
            eventUserInput.DiscordName = modal.User.Username;
            string response = string.Empty;

            eventUserInput.TeamLink = teamLink;

            var dmEvent = _eventFactory.GetDiceMastersEvent(eventUserInput.EventName, _currentEventList);
            response = dmEvent.SubmitTeamLink(eventUserInput);

            await modal.RespondAsync(response);
            await modal.User.SendMessageAsync($"The following team was successfully submitted for {eventUserInput.EventName}{Environment.NewLine}{eventUserInput.TeamLink}");
        }
        /*
        private async Task HandleCardHaveModalResponseAsync(SocketModal modal)
        {
            List<SocketMessageComponentData> modalComponents = modal.Data.Components.ToList();
            string cardCode = modalComponents.First(x => x.CustomId == "card_code").Value;
            string cardFoil = modalComponents.First(x => x.CustomId == "card_foil").Value;
            string cardMethod = modalComponents.First(x => x.CustomId == "card_method").Value;

            var cardInfo = GetCommunityCardInfoFromCodeString(cardCode);
            if(!cardInfo.HaveFoilToSell.Contains(modal.User.Username))
            {
                cardInfo.HaveFoilToSell = string.IsNullOrEmpty(cardInfo.HaveFoilToSell) ? modal.User.Username : $"{cardInfo.HaveFoilToSell},{modal.User.Username}";
            }
            // modify
            var result = _sheetService.UpdateCommunityCard(cardInfo);

            await modal.RespondAsync($"User {modal.User.Username} has a {cardInfo.TeamBuilderId} for sale/trade!");
            if (string.IsNullOrEmpty(cardInfo.WantFoilForTrade))
            {
                await modal.RespondAsync($"Found these users that have a FOIL for TRADE: {cardInfo.WantFoilForTrade}");
            }
        }

        private async Task HandleCardWantModalResponseAsync(SocketModal modal)
        {
            List<SocketMessageComponentData> modalComponents = modal.Data.Components.ToList();
            string cardCode = modalComponents.First(x => x.CustomId == "card_code").Value;
            string cardFoil = modalComponents.First(x => x.CustomId == "card_foil").Value;
            string cardMethod = modalComponents.First(x => x.CustomId == "card_method").Value;
        }
        */
        #endregion

        private async Task LoadTradeLists()
        {
            _tradeLists = await _sheetService.LoadAllTrades(_communityInfo);
        }

        private async Task DiscordMessageReceived(SocketMessage message)
        {
            if (message.Author.IsBot) return;
            if (!(message.Content.StartsWith(".") || message.Content.StartsWith("!"))) return;

            var dmchannelID = await message.Author.CreateDMChannelAsync();
            bool isDM = (message.Channel.Id == dmchannelID.Id);

            if (message.Content == "!ping")
            {
                await message.Channel.SendMessageAsync("Pong!");
            }
            else if (message.Content.ToLower().StartsWith(".submit") || message.Content.ToLower().StartsWith("!submit"))
            {
                if (!isDM)
                {
                    // if this is a public channel, first delete the original message, unless we are running in dev in which case we'd interfere with the Prod version
                    if (!_environment.IsDevelopment())
                    {
                        await message.Channel.DeleteMessageAsync(message);
                    }
                }
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
            else if (message.Content.ToLower().StartsWith(".report") || message.Content.ToLower().StartsWith("!report")
                        || message.Content.ToLower().StartsWith(".result") || message.Content.ToLower().StartsWith("!result"))
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
                await SendTeams(message);
            }
            else if (message.Content.ToLower().StartsWith(".stats"))
            {
                await GetStats(message);
            }
            else if (message.Content.ToLower().StartsWith(".register"))
            {
                //await message.Channel.SendMessageAsync("This event is not enabled for auto-registration. Please register manually.");
                if (await RegisterForEvent(message))
                {
                    await message.Channel.SendMessageAsync($"Thanks {message.Author.Username}, you are registered for the event!");
                }
            }
            else if (message.Content.ToLower().StartsWith(".fellowship"))
            {
                await message.Channel.DeleteMessageAsync(message);
                await RecordFellowship(message);
            }
            else if (message.Content.ToLower().StartsWith(".card"))
            {
                await CardLookup(message);
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
                if (message.Content.ToLower().StartsWith(".help more") || message.Content.ToLower().StartsWith("!help more"))
                {
                    await message.Channel.SendMessageAsync(_settings.GetBotHelpMoreString());
                }
                else
                {
                    await message.Channel.SendMessageAsync(_settings.GetBotHelpString());
                }
            }
            else if (message.Content.StartsWith("!test"))
            {
                //var participants = await _challonge.GetAllParticipantsAsync(_settings.GetOneOffChallongeId());
                //_logger.LogDebug($"{participants.Count}");
            }
        }

        private async Task CardLookup(SocketMessage message, bool imageOnly = false)
        {
            try
            {
                var args = Regex.Split(message.Content, @"\s+");

                var digits = new string(args[1].Where(s => char.IsDigit(s)).ToArray());
                var letters = new string(args[1].Where(s => char.IsLetter(s)).ToArray());

                var teamBuilderId = $"{letters}{digits.PadLeft(3, '0')}";

                var communityCardInfo = _communityInfo.Cards.Where(c => c.TeamBuilderId.ToLower() == teamBuilderId.ToLower()).FirstOrDefault();
                var quickanddirty = $"http://dicecoalition.com/cardservice/Image.php?set={letters}&cardnum={digits.TrimStart('0')}";

                if (!imageOnly || communityCardInfo != null)
                {
                    var cardEmbed = new EmbedBuilder
                    {
//                        Title = $"{communityCardInfo.CardTitle} : {communityCardInfo.CardSubtitle}",
                        Title = $"{communityCardInfo.CardTitle}",
                       // Description = communityCardInfo.CardSubtitle,
                       // ImageUrl = quickanddirty
                       ThumbnailUrl = quickanddirty
                    };

                    cardEmbed
                        .AddField("SubTitle", communityCardInfo.CardSubtitle)
                        .AddField("Ability Text", communityCardInfo.AbilityText)
                        .AddField("Affiliations", communityCardInfo.Affiliation)
                        .AddField("Rarity", communityCardInfo.Rarity, true)
                        //.AddField("Die stats", communityCardInfo.StatLine)
                        .WithFooter(footer => footer.Text = communityCardInfo.TeamBuilderId);

                    await message.Channel.SendMessageAsync(embed: cardEmbed.Build());

                    //await message.Channel.SendMessageAsync(quickanddirty);
                    //StringBuilder cardDescription = new StringBuilder();
                    //cardDescription.AppendLine($"Card Name: {communityCardInfo.CardTitle}");
                    //cardDescription.AppendLine($"Card Ability");
                    //cardDescription.AppendLine($"`{communityCardInfo.AbilityText}`");
                    //cardDescription.AppendLine($"Affiliation: {communityCardInfo.Affiliation}");
                    //cardDescription.AppendLine($"Rarity: {communityCardInfo.Rarity}");
                    //cardDescription.AppendLine($"");
                    //await message.Channel.SendMessageAsync(cardDescription.ToString());
                }
                else
                {
                    await message.Channel.SendMessageAsync(quickanddirty);
                }
            }
            catch (Exception exc)
            {
                await message.Channel.SendMessageAsync("Sorry, unable to figure out that card");
            }
        }

        private async Task RecordScore(SocketMessage message)
        {
            var dmEvent = _eventFactory.GetDiceMastersEvent(message.Channel.Name, _currentEventList);
            var dmManifest = _currentEventList.FirstOrDefault(e => e.EventName == message.Channel.Name);
            string hackTheException = string.Empty;
            if (dmEvent is StandaloneChallongeEvent)
            {
                // yes, this is hacky, but trying to play and debug the bot at the same time. ;)
                string lastMark = "Start";
                try
                {
                    // TODO
                    string challongeTournamentName = dmManifest.ChallongeTournamentName;

                    var scoreChannel = _client.GetChannel(dmManifest.ScoreKeeperChannelId) as IMessageChannel;
                    if (scoreChannel != null)
                    {
                        await scoreChannel.SendMessageAsync(message.Content.Replace(".report ", "").Replace("!report ", "").Replace(".result ", "").Replace("!result ", ""));
                    }
                    lastMark = "ScoreChannel found";

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

                        lastMark = "Scores processed, determine players";
                        var firstPlayerInfo = GetUserFromMention(message, firstPlayerArg);
                        var secondPlayerInfo = GetUserFromMention(message, secondPlayerArg);

                        //await message.Channel.SendMessageAsync($"Attempting to report scores for Challonge users {firstPlayerInfo.ChallongeName} and {secondPlayerInfo.ChallongeName}...");

                        var allPlayersChallongeInfo = await _challonge.GetAllParticipantsAsync(challongeTournamentName);
                        var firstPlayerChallongeInfo = allPlayersChallongeInfo.FirstOrDefault(p => p.ChallongeUsername.ToLower() == firstPlayerInfo.ChallongeName.ToLower());
                        if (firstPlayerChallongeInfo == null)
                        {
                            //firstPlayerChallongeInfo = allPlayersChallongeInfo.FirstOrDefault(p => p.Name == firstPlayerInfo.ChallongeName);
                        }
                        var secondPlayerChallongeInfo = allPlayersChallongeInfo.FirstOrDefault(p => p.ChallongeUsername.ToLower() == secondPlayerInfo.ChallongeName.ToLower());
                        //var secondPlayerChallongeInfo = allPlayersChallongeInfo.FirstOrDefault(p => p.Name == secondPlayerInfo.ChallongeName);
                        lastMark = "Players sorted";

                        var allMatches = await _challonge.GetAllMatchesAsync(challongeTournamentName);
                        var openMatches = allMatches.Where(m => m.State == "open").ToList();

                        lastMark = "Find open match";
                        if (openMatches.Count() > 1)
                        {
                            lastMark = "More than one open match returned";
                            bool playerOneisOne = true;
                            var possibleMatch = allMatches.Where(m => m.Player1Id == firstPlayerChallongeInfo.Id && m.Player2Id == secondPlayerChallongeInfo.Id).ToList();
                            lastMark = "1st possibleMatch";
                            if (!possibleMatch.Any())
                            {
                                lastMark = "1st no possibleMatch";
                                playerOneisOne = false;
                                possibleMatch = allMatches.Where(m => m.Player1Id == secondPlayerChallongeInfo.Id && m.Player2Id == firstPlayerChallongeInfo.Id).ToList();
                                lastMark = "2nd possibleMatch";
                            }
                            if (possibleMatch.Any() && possibleMatch.Count() == 1)
                            {
                                lastMark = "one possible match found";
                                int playerOneScore = 0;
                                int playerTwoScore = 0;
                                string[] scoreSplit = score.Split('-');
                                lastMark = "split the score";
                                if (scoreSplit.Length != 2)
                                {
                                    await message.Channel.SendMessageAsync($"Unable to parse the score: {score}. Reporting Challonge player {firstPlayerInfo.ChallongeName} as winner. Please contact TO if this is incorrect.");
                                    scoreSplit = new string[] { "1", "0" };
                                }

                                if (playerOneisOne)
                                {
                                    lastMark = "playerOneisOne";
                                    int.TryParse(scoreSplit[0], out playerOneScore);
                                    int.TryParse(scoreSplit[1], out playerTwoScore);
                                }
                                else
                                {
                                    lastMark = "playerTwoIsOne";
                                    int.TryParse(scoreSplit[0], out playerTwoScore);
                                    int.TryParse(scoreSplit[1], out playerOneScore);
                                }

                                lastMark = "Found the match";
                                var theMatch = possibleMatch.FirstOrDefault();
                                var result = await _challonge.UpdateMatchAsync(challongeTournamentName, theMatch.Id.GetValueOrDefault(), playerOneScore, playerTwoScore);
                                lastMark = "Reported the match";

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
                                lastMark = "Winner and loser reported";
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
                    hackTheException = $"{exc.Message}";
                }
                try
                {
                    // hacking this up because I don't have time to get actual logging working
                    if (!string.IsNullOrEmpty(hackTheException))
                    {
                        ulong toDiscordUserId;
                        ulong.TryParse(_settings.GetHackExceptionUser(), out toDiscordUserId);
                        var toDiscordUser = _client.GetUser(toDiscordUserId);
                        await toDiscordUser.SendMessageAsync($"Reporting exception in score submission:{Environment.NewLine}{lastMark}{Environment.NewLine}{hackTheException}");
                    }
                }
                catch { }
            }
            else
            {
                await message.Channel.SendMessageAsync($"Sorry, score reporting is not yet available for this channel.");
            }
        }

        private async Task<bool> RegisterForEvent(SocketMessage message)
        {
            bool result = false;
            try
            {
                var dmEvent = _eventFactory.GetDiceMastersEvent(message.Channel.Name, _currentEventList);
                var dmManifest = _currentEventList.FirstOrDefault(e => e.EventName == message.Channel.Name);
                if (dmEvent is StandaloneChallongeEvent)
                {
                    await message.Channel.SendMessageAsync($"For this tournament, please register at Challonge.com via the link provided in `.format`.");
                    // var userInfo = _sheetService.GetUserInfoFromDiscord(message.Author.Username);
                    // if (string.IsNullOrEmpty(userInfo.ChallongeName))
                    // {
                    //     await message.Author.SendMessageAsync("Please submit your Challonge username info by typing\n `.challonge mychallongeusername`");
                    //     return false;
                    // }
                    // else
                    // {
                    //     ParticipantVm challongeParticipant = new ParticipantVm();
                    //     challongeParticipant.ChallongeUsername = userInfo.ChallongeName;
                    //     challongeParticipant.Misc = $"Discord: {userInfo.DiscordName}";
                    //     challongeParticipant.ParticipantName = userInfo.DiscordName;
                    //     challongeParticipant.Seed = 1;

                    //     var participantInfo = await _challonge.AddParticipantAsync(challongeParticipant, dmManifest.ChallongeTournamentName);
                    //     await message.Author.SendMessageAsync($"Challonge User {participantInfo.ChallongeUsername} added");
                    //     result = true;
                    // }
                }
                else
                {
                    // if this is a public channel, first delete the original message, unless we are running in dev in which case we'd interfere with the Prod version
                    if (!_environment.IsDevelopment())
                    {
                        await message.Channel.DeleteMessageAsync(message);
                    }

                    var response = SubmitTeamLink(message, false);

                    if (string.IsNullOrEmpty(response))
                    {
                        await message.Channel.SendMessageAsync($"Sorry, something went wrong with the registration.");
                        result = false;
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync($"Buckle up, {message.Author.Username}: you are registered for {dmManifest.EventName}!");
                    }
                }
            }
            catch (Exception exc)
            {
                result = false;
                await message.Channel.SendMessageAsync($"Sorry, something went wrong with the registration. Possibly the event is ready yet?");
                Console.Write(exc.Message);
            }
            return result;
        }

        private string GetCurrentFormat(SocketMessage message)
        {
            var dmEvent = _eventFactory.GetDiceMastersEvent(message.Channel.Name, _currentEventList);
            var numberEvents = 0;
            int.TryParse(message.Content.Split(" ").LastOrDefault(), out numberEvents);
            return dmEvent.GetFormat(numberEvents);
        }

        private string SubmitTeamLink(SocketMessage message, bool isDM)
        {
            EventUserInput eventUserInput = new EventUserInput();
            string response = string.Empty;

            eventUserInput.Here = DateTime.UtcNow.ToString();
            eventUserInput.DiscordName = message.Author.Username;
            if (isDM)
            {
                var args = System.Text.RegularExpressions.Regex.Split(message.Content, @"\s+");
                var dmManifest = _currentEventList.FirstOrDefault(e => e.EventCode == args[1]);
                if (dmManifest == null)
                {
                    return "Sorry, I was unable to determine which tournament you were trying to submit a team for.";
                }
                var indexOfTheRest = message.Content.IndexOf(args[2]);
                eventUserInput.TeamLink = message.Content.Substring(indexOfTheRest).Replace("<", "").Replace(">", "");
                eventUserInput.EventName = dmManifest.EventName;
            }
            else
            {
                eventUserInput.TeamLink = message.Content
                                            .Replace("||", string.Empty)
                                            .TrimStart("!submit".ToCharArray())
                                            .TrimStart(".submit".ToCharArray())
                                            .TrimStart("!register".ToCharArray())
                                            .TrimStart(".register".ToCharArray())
                                            .Trim().Trim('<').Trim('>');
                eventUserInput.EventName = message.Channel.Name;
            }

            var dmEvent = _eventFactory.GetDiceMastersEvent(eventUserInput.EventName, _currentEventList);
            response = dmEvent.SubmitTeamLink(eventUserInput);

            if (string.IsNullOrEmpty(response))
            {
                return _settings.GetBotHelpString();
            }
            else
            {
                message.Author.SendMessageAsync($"The following team was successfully submitted for {eventUserInput.EventName}{Environment.NewLine}{eventUserInput.TeamLink}");
                if(dmEvent is TwoTeamTakedown)
                {
                    var teams = _sheetService.GetTTTDTeams(dmEvent.SheetId);

                    var thisTeam = teams.FirstOrDefault(t => t.DiscordName == eventUserInput.DiscordName);
                    StringBuilder statusResponse = new StringBuilder();
                    //statusResponse.AppendLine(response);
                    statusResponse.AppendLine("The status of your submission is:");
                    bool teamGood = true;
                    if(thisTeam.CardStatus != "Valid")
                    {
                        statusResponse.AppendLine($"You have a repeated card title among your teams, please fix and resubmit");
                        teamGood = false;
                    }
                    if (thisTeam.SetStatus != "Valid")
                    {
                        statusResponse.AppendLine($"You have repeated a set among your teams, please fix and resubmit");
                        teamGood = false;
                    }
                    if(teamGood)
                    {
                        statusResponse.AppendLine($"Your teams meet the 20x20 format challenge!");
                    }
                    message.Author.SendMessageAsync(statusResponse.ToString());
                }
            }
            return response;
        }

        private async Task GetCurrentPlayerList(SocketMessage message)
        {
            try
            {
                var dmEvent = _eventFactory.GetDiceMastersEvent(message.Channel.Name, _currentEventList);

                StringBuilder playerListString = new StringBuilder();
                if (dmEvent.UsesChallonge)
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
            foreach (var authTo in dmManifest.EventOrganizerIDList)
            {
                // simple check
                if (message.Author.Id.ToString() == authTo)
                {
                    var teamList = dmEvent.GetTeamLists();
                    int count = 0;
                    if (teamList.Any())
                    {
                        StringBuilder teamListOutput = new StringBuilder();
                        teamListOutput.AppendLine($"Here are the team lists for {dmManifest.EventName}:");
                        foreach (var team in teamList.OrderBy(t => t.DiscordName))
                        {
                            count++;
                            teamListOutput.AppendLine($"{team.DiscordName}: {team.TeamLink}");

                            if(count % 5 == 0) // every five teams, go ahead and send the message so we don't exceed Discord's 2000 character limit for a message
                            {
                                RequestOptions request = new RequestOptions();
                                await message.Channel.SendMessageAsync(teamListOutput.ToString());
                                teamListOutput.Clear();
                            }
                        }
                        return await message.Channel.SendMessageAsync(teamListOutput.ToString());

                    }
                }
            }

            // no authorized TO found
            return await message.Channel.SendMessageAsync("Sorry, you are not authorized to list teams for this event.");
        }

        private async Task<RestUserMessage> GetStats(SocketMessage message)
        {
            List<TeamListCharacterStats> teamListCharacterStats = new List<TeamListCharacterStats>();

            var dmEvent = _eventFactory.GetDiceMastersEvent(message.Channel.Name, _currentEventList);
            var dmManifest = _currentEventList.FirstOrDefault(e => e.EventName == message.Channel.Name);

            // check TO list
            var authorized = dmManifest.EventOrganizerIDList.Where(to => to == message.Author.Id.ToString());
            if (authorized.Any())
            {
                var teamList = dmEvent.GetTeamLists();
                if (teamList.Any())
                {
                    foreach (var team in teamList)
                    {
                        var cardsInTeam = ParseTeamList(team);
                        foreach (var card in cardsInTeam)
                        {
                            var cardExists = teamListCharacterStats.Where(c => c.Card.TeamBuilderId == card.TeamBuilderId).FirstOrDefault();
                            if (cardExists != null)
                            {
                                cardExists.TotalCount++;
                            }
                            else
                            {
                                teamListCharacterStats.Add(new TeamListCharacterStats { Card = card, TotalCount = 1 });
                            }
                        }
                    }

                    int count = 0;
                    if (teamListCharacterStats.Any())
                    {
                        StringBuilder teamListOutput = new StringBuilder();
                        teamListOutput.AppendLine($"Here are the card stats for {dmManifest.EventName}:");
                        foreach (var cardInfo in teamListCharacterStats.Where(t => t.TotalCount >= 5).OrderByDescending(o => o.TotalCount))
                        {
                            count++;
                            if (cardInfo.Card.FullCardInfo == null)
                            {
                                teamListOutput.AppendLine($"Card {cardInfo.Card.TeamBuilderId} appears {cardInfo.TotalCount} times.");
                            }
                            else
                            {
                                teamListOutput.AppendLine($"Card {cardInfo.Card.TeamBuilderId}: {cardInfo.Card.FullCardInfo.RarityAbbreviation} {cardInfo.Card.FullCardInfo.CardTitle} appears {cardInfo.TotalCount} times.");
                            }

                            if (count % 5 == 0) // every five cards, go ahead and send the message so we don't exceed Discord's 2000 character limit for a message
                            {
                                RequestOptions request = new RequestOptions();
                                await message.Channel.SendMessageAsync(teamListOutput.ToString());
                                teamListOutput.Clear();
                            }
                        }

                        for(int i=4; i > 0; i--)
                        {
                            teamListOutput.AppendLine($"The following cards were in {i} teams:");
                            int cardindex = 0;
                            var countcards = teamListCharacterStats.Where(t => t.TotalCount == i).OrderBy(o => o.Card.FullCardInfo.CardTitle).ToList();

                            var batch = string.Join(",", countcards.Skip(cardindex).Take(10).Select(c => c.SummaryOutput()).ToArray());

                            while (!string.IsNullOrEmpty(batch))
                            {
                                cardindex += 10;
                                count++;
                                teamListOutput.AppendLine(batch);

                                if (count % 5 == 0) // every five cards, go ahead and send the message so we don't exceed Discord's 2000 character limit for a message
                                {
                                    RequestOptions request = new RequestOptions();
                                    await message.Channel.SendMessageAsync(teamListOutput.ToString());
                                    teamListOutput.Clear();
                                }

                                batch = string.Join(",", countcards.Skip(cardindex).Take(10).Select(c => c.SummaryOutput()).ToArray());
                            }
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
            var strippedMentionString = mentionUserString.Replace("@", "").Replace(">", "").Replace("<", "");

            var playerDiscordUser = message.MentionedUsers.FirstOrDefault(u => u.Id.ToString() == mentionUserString);
            // Discord changed their format, so giving this a try.
            if (playerDiscordUser == null)
            {
                playerDiscordUser = message.MentionedUsers.FirstOrDefault(u => u.Id.ToString() == strippedMentionString);
            }

            // hopefully if we are here we either have a SocketUser, or the string wasn't actually a Discord @mention but just the text of the username
            userFromMention = _sheetService.GetUserInfoFromDiscord(playerDiscordUser != null ? playerDiscordUser.Username : mentionUserString.TrimStart('@'));

            // if we are still null, try to look up the Challonge user
            if (userFromMention == null)
            {
                userFromMention = _sheetService.GetUserInfoFromChallonge(strippedMentionString);
            }

            return userFromMention;
        }

        private void CheckYouTube()
        {
            var newVideos = _youTubeService.CheckForNewVideos();
            foreach (var video in newVideos)
            {
                var messageString = $"Channel: {video.ChannelName}, Video: {video.VideoTitle}{Environment.NewLine}{video.VideoLink}";
                var channelsToPost = video.IsDiceMasters ? _settings.GetDiceMastersMediaChannelIds() : _settings.GetNonDiceMastersMediaChannelIds();
                foreach (var channelId in channelsToPost)
                {
                    var discordChannel = _client.GetChannel(channelId) as IMessageChannel;
                    discordChannel.SendMessageAsync(messageString);
                    Console.WriteLine(messageString);
                }
            }
        }
        private void CheckRSSFeeds()
        {
            var rssFeeds = _sheetService.LoadRSSFeedInfo();
            foreach (var feed in rssFeeds)
            {
                if (!string.IsNullOrEmpty(feed.SiteUrl))
                {
                    try
                    {
                        XmlReader reader = XmlReader.Create(feed.SiteUrl);
                        SyndicationFeed sFeed = SyndicationFeed.Load(reader);
                        reader.Close();

                        foreach (SyndicationItem item in sFeed.Items)
                        {
                            if (item.PublishDate.UtcDateTime > feed.DateLastChecked)
                            {
                                var links = string.Join(Environment.NewLine, item.Links.Select(l => l.Uri.ToString()));
                                //                                var messageString = $"Site {feed.SiteName} posted:{Environment.NewLine}{item.Summary.Text}{Environment.NewLine}{links}";
                                // need to clean up summary if we want to include it
                                var messageString = $"Site {feed.SiteName} posted:{Environment.NewLine}{links}";

                                List<ulong> channelIds = new List<ulong>();
                                if (!string.IsNullOrEmpty(feed.ChannelIds))
                                {
                                    channelIds = _settings.ParseIdsFromString(feed.ChannelIds);
                                }
                                else
                                {
                                    channelIds = _settings.GetDiceMastersMediaChannelIds();
                                }
                                foreach (var channelId in channelIds)
                                {
                                    var discordChannel = _client.GetChannel(channelId) as IMessageChannel;
                                    discordChannel.SendMessageAsync(messageString);
                                    Console.WriteLine(messageString);
                                }
                            }
                        }
                    }
                    catch (Exception exc)
                    {
                        _logger.LogError($"Exception attempting to read an RSS feed: {exc.Message}");
                    }
                }
            }
            _sheetService.UpdateRssFeedInfo();
        }

        private void LoadCurrentEvents()
        {
            _currentEventList = _sheetService.LoadEventManifests();
        }

        private async Task LoadCommunityInfo()
        {
            _communityInfo = await _sheetService.LoadCommunityInfo();
        }

        private List<CardInfo> ParseTeamList(EventUserInput team)
        {
            List<CardInfo> cardList = new List<CardInfo>();

            if (!team.TeamLink.Contains("http")) return cardList;   // if we have no http, this isn't a valid team link

            // pull out URLs if we have multiple
            var urls = team.TeamLink.Split("http");

            foreach (var url in urls)
            {
                try
                {
                    if (url.Length == 0) continue;
                    var cardStrings = url.Substring(url.IndexOf("=")+1);
                    int nameIndex = cardStrings.IndexOf("&name");
                    if (nameIndex > 0)
                    {
                        cardStrings = cardStrings.Remove(nameIndex);
                    }
                    var cardIdArray = cardStrings.Split(";");
                    foreach (var cardString in cardIdArray)
                    {
                        if (!string.IsNullOrEmpty(cardString))
                        {
                            CardInfo card = ParseCardInfoString(cardString);
                            cardList.Add(card);
                        }
                    }
                } catch { }
            }

            return cardList;
        }

        private CardInfo ParseCardInfoString(string cardString)
        {
            int diceCountIndex = cardString.IndexOf("x");
            string diceCountstring = cardString.Substring(0, diceCountIndex);
            string diceIdString = cardString.Substring(diceCountIndex + 1);
            int.TryParse(diceCountstring, out int diceCount);

            var digits = new string(diceIdString.Where(s => char.IsDigit(s)).ToArray());
            var letters = new string(diceIdString.Where(s => char.IsLetter(s)).ToArray());
            var teamBuilderId = $"{letters}{digits.PadLeft(3, '0')}";
            var fullCardInfo = _communityInfo.Cards.Where(c => c.TeamBuilderId.ToLower() == teamBuilderId.ToLower()).FirstOrDefault();

            return new CardInfo { TeamBuilderId = diceIdString, DiceCount = diceCount, FullCardInfo = fullCardInfo ?? new CommunityCardInfo { TeamBuilderId = teamBuilderId } };
        }

        private CommunityCardInfo GetCommunityCardInfoFromCodeString(string codeString)
        {
            var digits = new string(codeString.Where(s => char.IsDigit(s)).ToArray());
            var letters = new string(codeString.Where(s => char.IsLetter(s)).ToArray());

            var teamBuilderId = $"{letters}{digits.PadLeft(3, '0')}";
            
            return _communityInfo.Cards.Single(c => c.TeamBuilderId.ToUpper() == teamBuilderId.ToUpper());
        }

        private Task Log(LogMessage msg)
        {
            _logger.LogDebug(msg.ToString());
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
