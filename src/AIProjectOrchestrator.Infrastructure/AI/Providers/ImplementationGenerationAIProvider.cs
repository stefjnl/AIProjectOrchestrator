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
        /// <param name="loggerFactory">Logger factory for creating operation-specific loggers</param>
        /// <param name="serviceProvider">Service provider for dependency resolution</param>
        public ImplementationGenerationAIProvider(
            IHttpClientFactory httpClientFactory,
            IOptions<AIOperationSettings> settings,
            ILogger<ImplementationGenerationAIProvider> logger,
            ILoggerFactory loggerFactory,
            IServiceProvider? serviceProvider = null)
            : base("ImplementationGeneration", httpClientFactory, settings, logger, loggerFactory, serviceProvider)
        {
            // This provider is specifically configured for Implementation Generation operations
            // The operation type "ImplementationGeneration" is used to look up configuration
        }
    }
}