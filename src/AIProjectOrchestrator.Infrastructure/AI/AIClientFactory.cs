using System.Collections.Generic;
using System.Linq;
using AIProjectOrchestrator.Domain.Services;
using Microsoft.Extensions.Logging;

namespace AIProjectOrchestrator.Infrastructure.AI
{
    public class AIClientFactory : IAIClientFactory
    {
        private readonly IEnumerable<IAIClient> _clients;
        private readonly AIClientFallbackService _fallbackService;
        private readonly ILogger<AIClientFactory>? _logger;

        public AIClientFactory(IEnumerable<IAIClient> clients, AIClientFallbackService fallbackService, ILogger<AIClientFactory>? logger = null)
        {
            _clients = clients;
            _fallbackService = fallbackService;
            _logger = logger;
        }

        public IAIClient? GetClient(string providerName)
        {
            _logger?.LogInformation("AIClientFactory.GetClient called for provider: {ProviderName}", providerName);
            
            // Try primary provider first
            var primaryClient = _clients.FirstOrDefault(c => c.ProviderName.Equals(providerName, System.StringComparison.OrdinalIgnoreCase));
            if (primaryClient != null)
            {
                _logger?.LogInformation("Found primary client: {ProviderName}", primaryClient.ProviderName);
                return primaryClient;
            }

            _logger?.LogWarning("Primary client {ProviderName} not found, attempting fallback", providerName);
            
            // Fallback logic: try alternative providers in order of preference
            var fallbackClient = _fallbackService.GetFallbackClient(providerName);
            if (fallbackClient != null)
            {
                _logger?.LogInformation("Fallback client found: {ProviderName}", fallbackClient.ProviderName);
            }
            else
            {
                _logger?.LogError("No fallback client available for provider: {ProviderName}", providerName);
            }
            
            return fallbackClient;
        }

        public IEnumerable<IAIClient> GetAllClients()
        {
            return _clients;
        }

        public async Task<IEnumerable<string>> GetModelsAsync(string providerName)
        {
            var client = GetClient(providerName);
            if (client == null)
            {
                _logger?.LogWarning("No client found for provider: {ProviderName}", providerName);
                return new List<string>();
            }

            return await client.GetModelsAsync();
        }
    }
}