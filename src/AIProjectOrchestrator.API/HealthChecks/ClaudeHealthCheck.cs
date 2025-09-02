using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Infrastructure.AI;

namespace AIProjectOrchestrator.API.HealthChecks
{
    public class ClaudeHealthCheck : IHealthCheck
    {
        private readonly ClaudeClient _client;

        public ClaudeHealthCheck(ClaudeClient client)
        {
            _client = client;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var isHealthy = await _client.IsHealthyAsync(cancellationToken);
            
            if (isHealthy)
            {
                return HealthCheckResult.Healthy("Claude API is responding");
            }

            return HealthCheckResult.Unhealthy("Claude API is not responding");
        }
    }
}