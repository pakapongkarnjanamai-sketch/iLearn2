using iLearn.Application.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace iLearn.API.Services
{
    public sealed class DeadlineDigestHostedService : BackgroundService
    {
        private static readonly TimeSpan ScheduledRunTime = TimeSpan.FromHours(8);
        private static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(1);

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DeadlineDigestHostedService> _logger;

        public DeadlineDigestHostedService(
            IServiceProvider serviceProvider,
            ILogger<DeadlineDigestHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("{Worker} started.", nameof(DeadlineDigestHostedService));

            var delayBeforeRun = TimeSpan.Zero;
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (delayBeforeRun > TimeSpan.Zero)
                    {
                        await Task.Delay(delayBeforeRun, stoppingToken);
                    }

                    using var scope = _serviceProvider.CreateScope();
                    var digestService = scope.ServiceProvider.GetRequiredService<IDeadlineDigestService>();
                    var createdCount = await digestService.RunOnceAsync(stoppingToken);
                    _logger.LogDebug("Deadline digest run completed with {CreatedCount} notifications.", createdCount);

                    var dateTime = scope.ServiceProvider.GetRequiredService<IDateTime>();
                    delayBeforeRun = GetDelayUntilNextRun(dateTime.Now);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{Worker} failed; retrying in {RetryDelay}.", nameof(DeadlineDigestHostedService), RetryDelay);
                    delayBeforeRun = RetryDelay;
                }
            }

            _logger.LogInformation("{Worker} stopped.", nameof(DeadlineDigestHostedService));
        }

        private static TimeSpan GetDelayUntilNextRun(DateTime now)
        {
            var nextRun = now.Date.Add(ScheduledRunTime);
            if (nextRun <= now)
            {
                nextRun = nextRun.AddDays(1);
            }

            return nextRun - now;
        }
    }
}