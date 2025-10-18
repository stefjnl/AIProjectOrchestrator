using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Infrastructure.AI;

namespace AIProjectOrchestrator.API.HealthChecks
{
    public class OpenRouterHealthCheck : IHealthCheck
    {
        private readonly OpenRouterClient _client;

        public OpenRouterHealthCheck(OpenRouterClient client)
        {
            _client = client;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var isHealthy = await _client.IsHealthyAsync(cancellationToken).ConfigureAwait(false);
            
            if (isHealthy)
            {
                return HealthCheckResult.Healthy("OpenRouter is responding");
            }

            return HealthCheckResult.Unhealthy("OpenRouter is not responding");
        }
    }
}