using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DiceMastersDiscordBot.Entities;
using DiceMastersDiscordBot.Properties;
using Discord;
using Discord.WebSocket;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DiceMastersDiscordBot.Services
{
    public class RallyMonitorService : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly IAppSettings _settings;
        private HttpClient _httpClient;
        private readonly DMSheetService _sheetService;

        private DiscordSocketClient _client;

        public RallyMonitorService(ILoggerFactory loggerFactory,
                            IAppSettings appSettings,
                            HttpClient httpClient,
                            DMSheetService dMSheetService)
        {
            _logger = loggerFactory.CreateLogger<RallyMonitorService>();
            _settings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _httpClient = httpClient;
            _sheetService = dMSheetService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DiscordBot Service is starting.");

            stoppingToken.Register(() => _logger.LogInformation("DiscordBot Service is stopping."));

            try
            {
                //_client = new DiscordSocketClient();

                //_client.Log += Log;

                ////Initialize command handling.
                //_client.MessageReceived += DiscordMessageReceived;
                ////await InstallCommands();      

                //// Connect the bot to Discord
                //string token = _settings.GetDiscordToken();
                //await _client.LoginAsync(TokenType.Bot, token);
                //await _client.StartAsync();

                // Block this task until the program is closed.
                //await Task.Delay(-1, stoppingToken);

                while (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("RallyMonitorService is doing background work.");

                    var coinPrice = await CheckPricesAsync();

                    _sheetService.SendRallyInfo(coinPrice);
                    Console.WriteLine(coinPrice.priceInUSD);

                    await Task.Delay(TimeSpan.FromHours(4), stoppingToken);
                }
            }
            catch (Exception exc)
            {
                _logger.LogError(exc.Message);
            }

            _logger.LogInformation("RallyMonitorService has stopped.");
        }

        private async Task<RallyCoinPrice> CheckPricesAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, @"https://api.rally.io/v1/creator_coins/crime/price");

            HttpResponseMessage response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return JsonSerializer.Deserialize<RallyCoinPrice>(await response.Content.ReadAsStringAsync());
        }

        private Task Log(LogMessage msg)
        {
            _logger.LogDebug(msg.ToString());
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
