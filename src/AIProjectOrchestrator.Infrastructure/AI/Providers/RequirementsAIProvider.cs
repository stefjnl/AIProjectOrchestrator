using System.Net.Http;
using AIProjectOrchestrator.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AIProjectOrchestrator.Infrastructure.AI.Providers
{
    /// <summary>
    /// AI provider specifically configured for Requirements Analysis operations.
    /// Uses "RequirementsAnalysis" configuration from AIProviderSettings.
    /// </summary>
    public class RequirementsAIProvider : ConfigurableAIProvider, IRequirementsAIProvider
    {
        /// <summary>
        /// Creates a new RequirementsAIProvider with operation-specific configuration.
        /// </summary>
        /// <param name="httpClientFactory">Factory for creating HTTP clients with Docker SSL support</param>
        /// <param name="settings">Operation-specific configuration</param>
        /// <param name="logger">Logger for diagnostics</param>
        public RequirementsAIProvider(
            IHttpClientFactory httpClientFactory,
            IOptions<AIProviderSettings> settings,
            ILogger<RequirementsAIProvider> logger)
            : base("RequirementsAnalysis", httpClientFactory, settings, logger)
        {
            // This provider is specifically configured for Requirements Analysis operations
            // The operation type "RequirementsAnalysis" is used to look up configuration
        }
    }
}