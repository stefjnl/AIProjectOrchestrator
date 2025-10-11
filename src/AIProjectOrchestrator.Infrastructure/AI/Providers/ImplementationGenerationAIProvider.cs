using System.Net.Http;
using AIProjectOrchestrator.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AIProjectOrchestrator.Infrastructure.AI.Providers
{
    /// <summary>
    /// AI provider specifically configured for Implementation Generation operations.
    /// Uses "ImplementationGeneration" configuration from AIProviderSettings.
    /// </summary>
    public class ImplementationGenerationAIProvider : ConfigurableAIProvider, IImplementationGenerationAIProvider
    {
        /// <summary>
        /// Creates a new ImplementationGenerationAIProvider with operation-specific configuration.
        /// </summary>
        /// <param name="httpClientFactory">Factory for creating HTTP clients with Docker SSL support</param>
        /// <param name="settings">Operation-specific configuration</param>
        /// <param name="logger">Logger for diagnostics</param>
        public ImplementationGenerationAIProvider(
            IHttpClientFactory httpClientFactory,
            IOptions<AIOperationSettings> settings,
            ILogger<ImplementationGenerationAIProvider> logger,
            ILoggerFactory loggerFactory,
            IProviderConfigurationService? providerConfigService = null,
            IServiceProvider? serviceProvider = null)
            : base("ImplementationGeneration", httpClientFactory, settings, logger, loggerFactory, providerConfigService, serviceProvider)
        {
            // This provider is specifically configured for Implementation Generation operations
            // The operation type "ImplementationGeneration" is used to look up configuration
        }
    }
}