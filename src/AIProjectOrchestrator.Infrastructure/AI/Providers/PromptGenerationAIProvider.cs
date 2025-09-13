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
    /// Uses "PromptGeneration" configuration from AIProviderSettings.
    /// </summary>
    public class PromptGenerationAIProvider : ConfigurableAIProvider, IPromptGenerationAIProvider
    {
        /// <summary>
        /// Creates a new PromptGenerationAIProvider with operation-specific configuration.
        /// </summary>
        /// <param name="httpClientFactory">Factory for creating HTTP clients with Docker SSL support</param>
        /// <param name="settings">Operation-specific configuration</param>
        /// <param name="logger">Logger for diagnostics</param>
        public PromptGenerationAIProvider(
            IHttpClientFactory httpClientFactory,
            IOptions<AIProviderSettings> settings,
            ILogger<PromptGenerationAIProvider> logger)
            : base("PromptGeneration", httpClientFactory, settings, logger)
        {
            // This provider is specifically configured for Prompt Generation operations
            // The operation type "PromptGeneration" is used to look up configuration
        }
    }
}