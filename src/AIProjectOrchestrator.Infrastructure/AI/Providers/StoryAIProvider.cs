using System.Net.Http;
using AIProjectOrchestrator.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AIProjectOrchestrator.Infrastructure.AI.Providers
{
    /// <summary>
    /// AI provider specifically configured for Story Generation operations.
    /// Uses "StoryGeneration" configuration from AIOperationSettings.
    /// </summary>
    public class StoryAIProvider : ConfigurableAIProvider, IStoryAIProvider
    {
        /// <summary>
        /// Creates a new StoryAIProvider with operation-specific configuration.
        /// </summary>
        /// <param name="httpClientFactory">Factory for creating HTTP clients with Docker SSL support</param>
        /// <param name="settings">Operation-specific configuration</param>
        /// <param name="logger">Logger for diagnostics</param>
        public StoryAIProvider(
            IHttpClientFactory httpClientFactory,
            IOptions<AIOperationSettings> settings,
            ILogger<StoryAIProvider> logger,
            ILoggerFactory loggerFactory,
            IProviderConfigurationService? providerConfigService = null,
            IServiceProvider? serviceProvider = null)
            : base("StoryGeneration", httpClientFactory, settings, logger, loggerFactory, providerConfigService, serviceProvider)
        {
            // This provider is specifically configured for Story Generation operations
            // The operation type "StoryGeneration" is used to look up configuration
        }
    }
}