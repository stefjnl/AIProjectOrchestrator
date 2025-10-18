using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Services;
using AIProjectOrchestrator.Infrastructure.AI;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AIProjectOrchestrator.API.HealthChecks
{
    public class NanoGptHealthCheck : IHealthCheck
    {
        private readonly IAIClientFactory _aiClientFactory;

        public NanoGptHealthCheck(IAIClientFactory aiClientFactory)
        {
            _aiClientFactory = aiClientFactory;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var client = _aiClientFactory.GetClient("NanoGpt");
                if (client == null)
                {
                    return HealthCheckResult.Unhealthy("NanoGpt client not available");
                }

                var isHealthy = await client.IsHealthyAsync(cancellationToken).ConfigureAwait(false);
                return isHealthy 
                    ? HealthCheckResult.Healthy("NanoGpt is healthy") 
                    : HealthCheckResult.Unhealthy("NanoGpt is unhealthy");
            }
            catch (System.Exception ex)
            {
                return HealthCheckResult.Unhealthy($"NanoGpt health check failed: {ex.Message}");
            }
        }
    }
}