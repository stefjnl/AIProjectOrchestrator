using System.Net.Http;
using AIProjectOrchestrator.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AIProjectOrchestrator.Infrastructure.AI.Providers
{
    /// <summary>
    /// AI provider specifically configured for Test Generation operations.
    /// Uses "TestGeneration" configuration from AIOperationSettings.
    /// </summary>
    public class TestGenerationAIProvider : ConfigurableAIProvider, ITestGenerationAIProvider
    {
        /// <summary>
        /// Creates a new TestGenerationAIProvider with operation-specific configuration.
        /// </summary>
        /// <param name="httpClientFactory">Factory for creating HTTP clients with Docker SSL support</param>
        /// <param name="settings">Operation-specific configuration</param>
        /// <param name="logger">Logger for diagnostics</param>
        public TestGenerationAIProvider(
            IHttpClientFactory httpClientFactory,
            IOptions<AIOperationSettings> settings,
            ILogger<TestGenerationAIProvider> logger,
            IProviderConfigurationService? providerConfigService = null,
            IServiceProvider? serviceProvider = null)
            : base("TestGeneration", httpClientFactory, settings, logger, providerConfigService, serviceProvider)
        {
            // This provider is specifically configured for Test Generation operations
            // The operation type "TestGeneration" is used to look up configuration
        }
    }
}