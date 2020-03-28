using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DiceMastersDiscordBot.Services;
using Microsoft.Extensions.Configuration;

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
                    services.AddHostedService<DiscordBot>();
                    //services.AddHostedService<ServiceB>();
                })
                // Only required if the service responds to requests.
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
    #endregion
}
