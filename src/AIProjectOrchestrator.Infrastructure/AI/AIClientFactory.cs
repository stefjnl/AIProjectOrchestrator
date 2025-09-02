using System.Collections.Generic;
using System.Linq;
using AIProjectOrchestrator.Domain.Services;

namespace AIProjectOrchestrator.Infrastructure.AI
{
    public class AIClientFactory : IAIClientFactory
    {
        private readonly IEnumerable<IAIClient> _clients;

        public AIClientFactory(IEnumerable<IAIClient> clients)
        {
            _clients = clients;
        }

        public IAIClient? GetClient(string providerName)
        {
            return _clients.FirstOrDefault(c => c.ProviderName.Equals(providerName, System.StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<IAIClient> GetAllClients()
        {
            return _clients;
        }
    }
}