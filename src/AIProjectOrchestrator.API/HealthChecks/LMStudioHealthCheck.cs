using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Infrastructure.AI;

namespace AIProjectOrchestrator.API.HealthChecks
{
    public class LMStudioHealthCheck : IHealthCheck
    {
        private readonly LMStudioClient _client;

        public LMStudioHealthCheck(LMStudioClient client)
        {
            _client = client;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var isHealthy = await _client.IsHealthyAsync(cancellationToken);
            
            if (isHealthy)
            {
                return HealthCheckResult.Healthy("LM Studio is responding");
            }

            return HealthCheckResult.Unhealthy("LM Studio is not responding");
        }
    }
}