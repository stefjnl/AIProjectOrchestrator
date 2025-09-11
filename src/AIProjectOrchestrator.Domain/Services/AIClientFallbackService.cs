using System.Collections.Generic;
using AIProjectOrchestrator.Domain.Services;
using Microsoft.Extensions.Logging;

namespace AIProjectOrchestrator.Domain.Services
{
    public class AIClientFallbackService
    {
        private readonly ILogger<AIClientFallbackService>? _logger;
        private readonly IEnumerable<IAIClient> _clients;
        private readonly List<string> _fallbackOrder;

        public AIClientFallbackService(IEnumerable<IAIClient> clients, ILogger<AIClientFallbackService>? logger = null)
        {
            _clients = clients;
            _logger = logger;
            _fallbackOrder = new List<string> { "NanoGpt", "Claude", "LMStudio", "OpenRouter" };
        }

        public IAIClient? GetFallbackClient(string providerName)
        {
            _logger?.LogInformation("AIClientFallbackService.GetFallbackClient called for provider: {ProviderName}", providerName);
            _logger?.LogInformation("Available clients: {ClientNames}", string.Join(", ", _clients.Select(c => c.ProviderName)));
            
            foreach (var fallbackProvider in _fallbackOrder)
            {
                _logger?.LogInformation("Checking fallback provider: {FallbackProvider}", fallbackProvider);
                
                if (!fallbackProvider.Equals(providerName, System.StringComparison.OrdinalIgnoreCase))
                {
                    var fallbackClient = _clients.FirstOrDefault(c => c.ProviderName.Equals(fallbackProvider, System.StringComparison.OrdinalIgnoreCase));
                    if (fallbackClient != null)
                    {
                        _logger?.LogWarning("Primary provider {Primary} not available, falling back to {Fallback}", providerName, fallbackProvider);
                        return fallbackClient;
                    }
                    else
                    {
                        _logger?.LogWarning("Fallback provider {FallbackProvider} not found in available clients", fallbackProvider);
                    }
                }
                else
                {
                    _logger?.LogInformation("Skipping {FallbackProvider} as it matches the requested provider", fallbackProvider);
                }
            }

            _logger?.LogError("No fallback client available for provider: {ProviderName}", providerName);
            return null; // No fallback available
        }
    }
}