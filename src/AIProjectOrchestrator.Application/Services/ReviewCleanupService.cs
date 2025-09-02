using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AIProjectOrchestrator.Domain.Configuration;
using AIProjectOrchestrator.Domain.Services;

namespace AIProjectOrchestrator.Application.Services
{
    public class ReviewCleanupService : BackgroundService
    {
        private readonly ILogger<ReviewCleanupService> _logger;
        private readonly IOptions<ReviewSettings> _settings;
        private readonly IServiceProvider _serviceProvider;

        public ReviewCleanupService(
            ILogger<ReviewCleanupService> logger,
            IOptions<ReviewSettings> settings,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _settings = settings;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Review cleanup service started");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await CleanupExpiredReviewsAsync(stoppingToken);
                    
                    // Wait for the next cleanup interval
                    await Task.Delay(TimeSpan.FromMinutes(_settings.Value.CleanupIntervalMinutes), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Review cleanup service is stopping");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in review cleanup service");
            }

            _logger.LogInformation("Review cleanup service stopped");
        }

        private async Task CleanupExpiredReviewsAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Starting cleanup of expired reviews");

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var reviewService = scope.ServiceProvider.GetRequiredService<IReviewService>();
                
                var expiredCount = await reviewService.CleanupExpiredReviewsAsync(cancellationToken);
                
                _logger.LogInformation("Cleanup completed. {ExpiredReviewCount} expired reviews marked", expiredCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup of expired reviews");
            }
        }
    }
}