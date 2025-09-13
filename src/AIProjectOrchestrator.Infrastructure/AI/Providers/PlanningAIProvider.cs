using System.Net.Http;
using AIProjectOrchestrator.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AIProjectOrchestrator.Infrastructure.AI.Providers
{
    /// <summary>
    /// AI provider specifically configured for Project Planning operations.
    /// Uses "ProjectPlanning" configuration from AIProviderSettings.
    /// </summary>
    public class PlanningAIProvider : ConfigurableAIProvider, IPlanningAIProvider
    {
        /// <summary>
        /// Creates a new PlanningAIProvider with operation-specific configuration.
        /// </summary>
        /// <param name="httpClientFactory">Factory for creating HTTP clients with Docker SSL support</param>
        /// <param name="settings">Operation-specific configuration</param>
        /// <param name="logger">Logger for diagnostics</param>
        public PlanningAIProvider(
            IHttpClientFactory httpClientFactory,
            IOptions<AIProviderSettings> settings,
            ILogger<PlanningAIProvider> logger)
            : base("ProjectPlanning", httpClientFactory, settings, logger)
        {
            // This provider is specifically configured for Project Planning operations
            // The operation type "ProjectPlanning" is used to look up configuration
        }
    }
}