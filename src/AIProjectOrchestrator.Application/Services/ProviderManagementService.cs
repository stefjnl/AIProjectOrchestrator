using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AIProjectOrchestrator.Application.Interfaces;
using AIProjectOrchestrator.Domain.Services;
using AIProjectOrchestrator.Domain.Models.AI;

namespace AIProjectOrchestrator.Application.Services
{
    public class ProviderManagementService : IProviderManagementService
    {
        private readonly IAIClientFactory _factory;
        private readonly string[] _validProviders = { "NanoGpt", "OpenRouter" };

        public ProviderManagementService(IAIClientFactory factory)
        {
            _factory = factory;
        }

        public Task<IEnumerable<string>> GetAvailableProvidersAsync()
        {
            var providers = _factory.GetAllClients().Select(c => c.ProviderName);
            return Task.FromResult(providers);
        }

        public Task<object> GetProviderHealthAsync(string name)
        {
            var client = _factory.GetClient(name);
            var status = new
            {
                Available = client != null,
                Provider = name
            };
            return Task.FromResult((object)status);
        }

        public Task<bool> IsValidProviderAsync(string provider)
        {
            return Task.FromResult(_validProviders.Any(p => p.Equals(provider, StringComparison.OrdinalIgnoreCase)));
        }
    }
}
