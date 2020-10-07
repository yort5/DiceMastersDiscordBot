using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DiceMastersDiscordBot.Services;
using DiceMastersDiscordBot.Entities;
using Microsoft.Extensions.Configuration;
using DiceMastersDiscordBot.Properties;
using DiceMastersDiscordBot.Events;

namespace DiceMastersDiscordBot
{
    #region snippet_Program
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await CreateHostBuilder(args).Build().RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureAppConfiguration((context, config) =>
                {
                    var settings = config.Build();
                    config.AddAzureAppConfiguration(settings["AzureAppConfigurationEndpoint"]);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<IAppSettings, AppSettings>();
                    services.AddSingleton<DMSheetService>();
                    services.AddSingleton<DiceMastersEventFactory>();
                    services.AddTransient<StandaloneChallongeEvent>()
                        .AddTransient<IDiceMastersEvent, StandaloneChallongeEvent>(s => s.GetService<StandaloneChallongeEvent>());
                    services.AddTransient<WeeklyDiceArenaEvent>()
                        .AddTransient<IDiceMastersEvent, WeeklyDiceArenaEvent>(s => s.GetService<WeeklyDiceArenaEvent>());
                    services.AddTransient<DiceFightEvent>()
                        .AddTransient<IDiceMastersEvent, DiceFightEvent>(s => s.GetService<DiceFightEvent>());
                    services.AddTransient<DiceSocialEvent>()
                        .AddTransient<IDiceMastersEvent, DiceSocialEvent>(s => s.GetService<DiceSocialEvent>());
                    services.AddHttpClient<ChallongeEvent>(c =>
                    {
                        c.BaseAddress = new Uri("https://api.challonge.com");
                    });
                    services.AddHostedService<DiscordBot>();
                    //services.AddHostedService<TwitchBot>();
                })
                // Only required if the service responds to requests.
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
    #endregion
}
