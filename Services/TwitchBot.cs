using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace DiceMastersDiscordBot.Services
{
    public class TwitchBot : BackgroundService
    {
        TwitchClient twitchClient;
        private readonly DMSheetService _sheetService;

        public TwitchBot(ILoggerFactory loggerFactory, IConfiguration config, DMSheetService dMSheetService)
        {
            Logger = loggerFactory.CreateLogger<TwitchBot>();
            Config = config;
            _sheetService = dMSheetService;
        }

        public ILogger Logger { get; }
        public IConfiguration Config { get; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Logger.LogInformation("TwitchBot is starting.");

            string twitchUserName = Config["TwitchUsername"];
            string twitchToken = Config["TwitchToken"];
            string twitchChannel = Config["TwitchChannel"];


            ConnectionCredentials credentials = new ConnectionCredentials(twitchUserName, twitchToken);
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };
            WebSocketClient customClient = new WebSocketClient(clientOptions);
            twitchClient = new TwitchClient(customClient);
            twitchClient.Initialize(credentials, twitchChannel);

            twitchClient.OnLog += Client_OnLog;
            twitchClient.OnJoinedChannel += Client_OnJoinedChannel;
            twitchClient.OnMessageReceived += Client_OnMessageReceived;
            twitchClient.OnWhisperReceived += Client_OnWhisperReceived;
            twitchClient.OnNewSubscriber += Client_OnNewSubscriber;
            twitchClient.OnConnected += Client_OnConnected;

            twitchClient.Connect();

            stoppingToken.Register(() => Logger.LogInformation("TwitchBot is stopping."));

            while (!stoppingToken.IsCancellationRequested)
            {
                Logger.LogInformation("TwitchBot is doing background work.");

                await Task.Delay(TimeSpan.FromSeconds(25), stoppingToken);
            }

            Logger.LogInformation("TwitchBot has stopped.");
        }

        private void Client_OnLog(object sender, OnLogArgs e)
        {
            Console.WriteLine($"{e.DateTime.ToString()}: {e.BotUsername} - {e.Data}");
        }

        private void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            Console.WriteLine($"Connected to {e.AutoJoinChannel}");
        }

        private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            Console.WriteLine("Hey guys! I am a bot connected via TwitchLib!");
            twitchClient.SendMessage(e.Channel, "Hey guys! I am DiceMastersBot! I do not play marbles, sorry.");
        }

        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            if (e.ChatMessage.Message.Contains("BadRolls"))
                twitchClient.SendMessage(e.ChatMessage.Channel, "Did I hear someone say BadRolls?");
            if (e.ChatMessage.Message.StartsWith("!teams"))
                twitchClient.SendMessage(e.ChatMessage.Channel, _sheetService.ListTeams(e.ChatMessage.Channel, e.ChatMessage.Username));
        }

        private void Client_OnWhisperReceived(object sender, OnWhisperReceivedArgs e)
        {
            if (e.WhisperMessage.Username == "my_friend")
                twitchClient.SendWhisper(e.WhisperMessage.Username, "Hey! Whispers are so cool!!");
        }

        private void Client_OnNewSubscriber(object sender, OnNewSubscriberArgs e)
        {
            if (e.Subscriber.SubscriptionPlan == SubscriptionPlan.Prime)
                twitchClient.SendMessage(e.Channel, $"Welcome {e.Subscriber.DisplayName} to the substers! You just earned 500 points! So kind of you to use your Twitch Prime on this channel!");
            else
                twitchClient.SendMessage(e.Channel, $"Welcome {e.Subscriber.DisplayName} to the substers! You just earned 500 points!");
        }
    }
}
