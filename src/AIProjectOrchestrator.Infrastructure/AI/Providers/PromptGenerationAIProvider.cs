using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Models.AI;
using System.Net.Http;
using AIProjectOrchestrator.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AIProjectOrchestrator.Infrastructure.AI.Providers
{
    /// <summary>
    /// AI provider specifically configured for Prompt Generation operations.
    /// Uses "PromptGeneration" configuration from AIOperationSettings.
    /// </summary>
    public class PromptGenerationAIProvider : ConfigurableAIProvider, IPromptGenerationAIProvider
    {
        /// <summary>
        /// Creates a new PromptGenerationAIProvider with operation-specific configuration.
        /// </summary>
        /// <param name="httpClientFactory">Factory for creating HTTP clients with Docker SSL support</param>
        /// <param name="settings">Operation-specific configuration</param>
        /// <param name="logger">Logger for diagnostics</param>
        /// <param name="loggerFactory">Logger factory for creating operation-specific loggers</param>
        /// <param name="serviceProvider">Service provider for dependency resolution</param>
        public PromptGenerationAIProvider(
            IHttpClientFactory httpClientFactory,
            IOptions<AIOperationSettings> settings,
            ILogger<PromptGenerationAIProvider> logger,
            ILoggerFactory loggerFactory,
            IServiceProvider? serviceProvider = null)
            : base("PromptGeneration", httpClientFactory, settings, logger, loggerFactory, serviceProvider)
        {
            // This provider is specifically configured for Prompt Generation operations
            // The operation type "PromptGeneration" is used to look up configuration
        }
    }
}
