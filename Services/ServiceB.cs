using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DiceMastersDiscordBot.Services
{
    public class ServiceB : BackgroundService
    {
        public ServiceB(ILoggerFactory loggerFactory)
        {
            Logger = loggerFactory.CreateLogger<ServiceB>();
        }

        public ILogger Logger { get; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Logger.LogInformation("ServiceB is starting.");

            stoppingToken.Register(() => Logger.LogInformation("ServiceB is stopping."));

            while (!stoppingToken.IsCancellationRequested)
            {
                Logger.LogInformation("ServiceB is doing background work.");

                await Task.Delay(TimeSpan.FromSeconds(25), stoppingToken);
            }

            Logger.LogInformation("ServiceB has stopped.");
        }
    }
}
