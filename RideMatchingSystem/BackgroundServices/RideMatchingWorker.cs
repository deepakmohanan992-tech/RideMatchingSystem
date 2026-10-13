using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using RideMatchingSystem.Services;

namespace RideMatchingSystem.BackgroundServices
{
    public class RideMatchingWorker : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<RideMatchingWorker> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromSeconds(10);

        public RideMatchingWorker(IServiceProvider services, ILogger<RideMatchingWorker> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("RideMatchingWorker started");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _services.CreateScope();
                    var matching = scope.ServiceProvider.GetRequiredService<MatchingService>();
                    var matched = await matching.MatchPendingRidesAsync(stoppingToken);
                    if (matched > 0)
                    {
                        _logger.LogInformation("Matched {Count} rides in background run", matched);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while running matching worker");
                }

                await Task.Delay(_interval, stoppingToken);
            }

            _logger.LogInformation("RideMatchingWorker stopping");
        }
    }
}

