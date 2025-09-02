using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Services;

namespace AIProjectOrchestrator.API.HealthChecks
{
    public class ReviewHealthCheck : IHealthCheck
    {
        private readonly IReviewService _reviewService;

        public ReviewHealthCheck(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var isHealthy = await _reviewService.IsHealthyAsync(cancellationToken);
                
                if (isHealthy)
                {
                    return HealthCheckResult.Healthy("Review service is healthy");
                }
                
                return HealthCheckResult.Degraded("Review service is not healthy");
            }
            catch (System.Exception ex)
            {
                return HealthCheckResult.Unhealthy("Review service health check failed", ex);
            }
        }
    }
}