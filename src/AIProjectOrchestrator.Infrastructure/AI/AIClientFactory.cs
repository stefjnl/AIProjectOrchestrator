using System.Collections.Generic;
using System.Linq;
using AIProjectOrchestrator.Domain.Services;
using Microsoft.Extensions.Logging;

namespace AIProjectOrchestrator.Infrastructure.AI
{
    public class AIClientFactory : IAIClientFactory
    {
        private readonly IEnumerable<IAIClient> _clients;
        private readonly ILogger<AIClientFactory>? _logger;

        public AIClientFactory(IEnumerable<IAIClient> clients, ILogger<AIClientFactory>? logger = null)
        {
            _clients = clients;
            _logger = logger;
        }

        public IAIClient? GetClient(string providerName)
        {
            // Try primary provider first
            var primaryClient = _clients.FirstOrDefault(c => c.ProviderName.Equals(providerName, System.StringComparison.OrdinalIgnoreCase));
            if (primaryClient != null)
            {
                return primaryClient;
            }

            // Fallback logic: try alternative providers in order of preference
            var fallbackOrder = new[] { "NanoGpt", "Claude", "LMStudio", "OpenRouter" };
            foreach (var fallbackProvider in fallbackOrder)
            {
                if (!fallbackProvider.Equals(providerName, System.StringComparison.OrdinalIgnoreCase))
                {
                    var fallbackClient = _clients.FirstOrDefault(c => c.ProviderName.Equals(fallbackProvider, System.StringComparison.OrdinalIgnoreCase));
                    if (fallbackClient != null)
                    {
                        _logger?.LogWarning("Primary provider {Primary} not available, falling back to {Fallback}", providerName, fallbackProvider);
                        return fallbackClient;
                    }
                }
            }

            return null; // No fallback available
        }

        public IEnumerable<IAIClient> GetAllClients()
        {
            return _clients;
        }
    }
}