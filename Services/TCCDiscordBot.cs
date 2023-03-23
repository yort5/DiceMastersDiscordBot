using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using DiceMastersDiscordBot.Properties;
using Discord.Net;
using DiceMastersDiscordBot.Entities;

namespace DiceMastersDiscordBot.Services
{
    public class TCCDiscordBot : BackgroundService
    {
        private readonly ILogger<TCCDiscordBot> _logger;
        private readonly IAppSettings _settings;
        private readonly IHostEnvironment _environment;
        private Random _random;
        private DateTime _relientKLastMentioned = DateTime.MinValue;

        private readonly TCCSheetService _sheetService;
        private DiscordSocketClient _client;

        private string hellyeahpath = Path.Combine("Images", "hellyeah.jpg");

        public TCCDiscordBot(ILogger<TCCDiscordBot> logger,
                            IAppSettings appSettings,
                            IHostEnvironment environment,
                            IHttpClientFactory httpClientFactory,
                            TCCSheetService gSheetService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _sheetService = gSheetService;

            _random = new Random();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DiscordBot Service is starting.");

            stoppingToken.Register(() => _logger.LogInformation("DiscordBot Service is stopping."));

            try
            {
                var discordConfig = new DiscordSocketConfig { GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent | GatewayIntents.GuildMembers | GatewayIntents.GuildPresences };
                _client = new DiscordSocketClient(discordConfig);

                _client.Log += Log;

                //Initialize command handling.
                _client.Ready += Client_Ready;
                _client.MessageReceived += DiscordMessageReceived;
                _client.SlashCommandExecuted += SlashCommandHandler;
                _client.ModalSubmitted += ModalResponseHandler;

                // Connect the bot to Discord
                string token = _settings.GetTCCDiscordToken();
                await _client.LoginAsync(TokenType.Bot, token);
                await _client.StartAsync();

                // give the service a chance to start before moving on to computational things
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);

                var lastUpdatedTicks = DateTime.MinValue.ToUniversalTime().Ticks;
                while (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("DiscordBot is doing background work.");

                    // only do these once a day
                    var currentDayTicks = DateTime.Today.ToUniversalTime().Ticks;
                    if (lastUpdatedTicks < currentDayTicks)
                    {
                        lastUpdatedTicks = currentDayTicks;
                    }

                    await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
                }
            }
            catch (Exception exc)
            {
                _logger.LogError(exc.Message);
            }

            _logger.LogInformation("DiscordService has stopped.");
        }

        private async Task Client_Ready()
        {
            var referralCommand = new SlashCommandBuilder()
                .WithName("referral")
                .WithDescription("Refer an company, service, app, etc with a code or link for mutual benefit.")
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("list")
                    .WithDescription("Print out a list of the referrals available.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                )
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("get")
                    .WithDescription("Get a random referral code for a particular company.")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("company", ApplicationCommandOptionType.String, "Name of the company for which you want a referral.", isRequired: true)
                )
                .AddOption(new SlashCommandOptionBuilder()
                .WithName("add")
                .WithDescription("Add your referral for a particular company.")
                .WithType(ApplicationCommandOptionType.SubCommand)
                );
            await RegisterCommand(referralCommand, true);

        }

        private async Task RegisterCommand(SlashCommandBuilder command, bool devOverride = false)
        {
            // Running the registration locally seems to break that registration in the deployed version.
            // Add a switch to turn off registration locally, but allow an override for testing new functionality.
            if (!_environment.IsDevelopment() || devOverride)
            {
                var guildList = _settings.GetServersForSlashCommand(command.Name);
                foreach (var guildId in guildList)
                {
                    try
                    {
                        var guild = _client.GetGuild(guildId);
                        await guild.CreateApplicationCommandAsync(command.Build());
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
                case "referral":
                    await HandleReferralCommand(command);
                    break;
            }
        }

        private async Task HandleReferralCommand(SocketSlashCommand command)
        {
            var haveOrWant = command.Data.Options.First().Name;
            var cardCode = command.Data.Options.First().Options.First().Value.ToString();
            var isFoil = command.Data.Options.Where(o => o.Name == "foil");
            var type = command.Data.Options.Where(o => o.Name == "type");

            switch (haveOrWant)
            {
                case "have":
                    {

                    }
                    break;
                case "want":
                    {

                    }
                    break;
            }
        }

        private async Task ModalResponseHandler(SocketModal modal)
        {
            switch (modal.Data.CustomId)
            {
                case "team_submit":
                    // await HandleTeamSubmitModalResponseAsync(modal);
                    break;
                    //case "card_have":
                    //    await HandleCardHaveModalResponseAsync(modal);
                    //    break;
                    //case "card_want":
                    //    await HandleCardWantModalResponseAsync(modal);
                    //    break;
            }
        }

        private async Task DiscordMessageReceived(SocketMessage message)
        {
            if (message.Author.IsBot) return;

            try
            {
                if (IsHellYeah(message))
                {
                    await message.Channel.SendFileAsync(hellyeahpath);
                }

                if (TagCierra(message))
                {
                    var cee = await message.Channel.GetUserAsync(_settings.GetCierraNotificationId());
                    await message.Channel.SendMessageAsync($"Did someone tag {cee.Mention}?");
                }

                if (SomeoneSaidRelientK(message))
                {
                    List<string> relientKNotificationMentions = new List<string>();
                    foreach (var notifyId in _settings.GetRelientKNotificationIds())
                    {
                        var rkUser = await message.Channel.GetUserAsync(notifyId);
                        relientKNotificationMentions.Add(rkUser.Mention);
                    }
                    var relientKUserMentions = string.Join(", ", relientKNotificationMentions);
                    await message.Channel.SendMessageAsync($"Discussion of Relient K detected, notifying the following: {relientKUserMentions}");
                }
            }
            catch (Exception exc)
            {
                // just making sure an exception above doesn't tank the whole thing
            }
        }

        private async Task HandleReferralRequest(SocketMessage message)
        {
            var args = message.Content.Split(' ');
            if (args.Length >= 2)
            {
                var request = args[1];
                if (request.ToLower() == "add")
                {
                    string brand = string.Empty;
                    string code = string.Empty;
                    string link = string.Empty;
                    if (args.Length >= 3) brand = args[2].ToString();
                    if (args.Length >= 4) code = args[3].ToString();
                    if (args.Length >= 5) link = args[4].ToString();

                    ReferralInfo newReferral = new ReferralInfo()
                    {
                        ReferralDiscordName = message.Author.Username,
                        ReferralBrand = brand,
                        ReferralCode = code,
                        ReferralLink = link
                    };

                    var response = await _sheetService.AddReferral(newReferral);
                    await message.Channel.SendMessageAsync(response);
                    return;
                }
                else if (request.ToLower() == "get")
                {
                    if (args.Length >= 3)
                    {
                        var brandRequested = args[2].ToLower();
                        var referrals = await _sheetService.GetAllReferrals();
                        var brandReferrals = referrals.Where(r => r.ReferralBrand.ToLower() == brandRequested).ToList();

                        var index = _random.Next(0, brandReferrals.Count);
                        var winner = brandReferrals[index];

                        await message.Channel.SendMessageAsync($"Referral for {winner.ReferralBrand}:{Environment.NewLine}{winner.ReferralCode}");
                    }
                }
                else if (request.ToLower() == "list")
                {
                    var referrals = await _sheetService.GetAllReferrals();
                    var codeList = referrals.Select(code =>
                      new { ReferralBrand = code.ReferralBrand.ToLower() }).ToList().Distinct();
                    StringBuilder codeResponse = new StringBuilder();
                    codeResponse.AppendLine($"Here is the list of available referrals:");
                    foreach (var code in codeList) { codeResponse.AppendLine(code.ReferralBrand); }
                    await message.Channel.SendMessageAsync(codeResponse.ToString());
                }
            }

        }

        private static bool IsHellYeah(SocketMessage message)
        {
            // probably should change this to a string list compare
            if (message.Content.ToLower().Contains("hell yeah", StringComparison.CurrentCultureIgnoreCase))
            {
                return true;
            }

            if (message.Content.ToLower().Contains("hellz yeah", StringComparison.CurrentCultureIgnoreCase))
            {
                return true;
            }

            if (message.Content.ToLower().Contains("peephole", StringComparison.CurrentCultureIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private static bool TagCierra(SocketMessage message)
        {
            if (message.Content.ToLower().Contains(" cee", StringComparison.CurrentCultureIgnoreCase)
                || message.Content.ToLower().StartsWith("cee ")
                || message.Content.ToLower().StartsWith("cee?")
                || message.Content.ToLower().StartsWith("cee!")
            )
            {
                return true;
            }
            return false;
        }


        private bool SomeoneSaidRelientK(SocketMessage message)
        {
            bool notifyRelientK = false;
            if (message.Content.ToLower().Contains("relient k", StringComparison.CurrentCultureIgnoreCase) ||
                message.Content.ToLower().Contains(" rk ")
            )
            {
                if (DateTime.UtcNow.AddMinutes(-15) > _relientKLastMentioned)
                {
                    notifyRelientK = true;
                }
                _relientKLastMentioned = DateTime.UtcNow;
            }
            return notifyRelientK;
        }

        private Task Log(LogMessage msg)
        {
            _logger.LogDebug(msg.ToString());
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
