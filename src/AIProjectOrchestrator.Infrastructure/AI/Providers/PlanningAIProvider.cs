using System.Net.Http;
using AIProjectOrchestrator.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AIProjectOrchestrator.Infrastructure.AI.Providers
{
    /// <summary>
    /// AI provider specifically configured for Project Planning operations.
    /// Uses "ProjectPlanning" configuration from AIOperationSettings.
    /// </summary>
    public class PlanningAIProvider : ConfigurableAIProvider, IPlanningAIProvider
    {
        /// <summary>
        /// Creates a new PlanningAIProvider with operation-specific configuration.
        /// </summary>
        /// <param name="httpClientFactory">Factory for creating HTTP clients with Docker SSL support</param>
        /// <param name="settings">Operation-specific configuration</param>
        /// <param name="logger">Logger for diagnostics</param>
        /// <param name="providerConfigService">Service for runtime provider configuration</param>
        public PlanningAIProvider(
            IHttpClientFactory httpClientFactory,
            IOptions<AIOperationSettings> settings,
            ILogger<PlanningAIProvider> logger,
            ILoggerFactory loggerFactory,
            IProviderConfigurationService? providerConfigService = null,
            IServiceProvider? serviceProvider = null)
            : base("ProjectPlanning", httpClientFactory, settings, logger, loggerFactory, providerConfigService, serviceProvider)
        {
            // This provider is specifically configured for Project Planning operations
            // The operation type "ProjectPlanning" is used to look up configuration
        }
    }
}
