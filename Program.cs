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
                    services.AddSingleton<DMSheetService>();
                    services.AddHostedService<DiscordBot>();
                    services.AddHttpClient<ChallongeEvent>(c =>
                    {
                        c.BaseAddress = new Uri("https://api.challonge.com");
                    });
                    services.AddSingleton<IAppSettings, AppSettings>();
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
