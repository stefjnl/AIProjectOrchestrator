using System.Collections.Generic;
using System.Linq;
using AIProjectOrchestrator.Domain.Services;

namespace AIProjectOrchestrator.Infrastructure.AI
{
    public interface IAIClientFactory
    {
        IAIClient? GetClient(string providerName);
        IEnumerable<IAIClient> GetAllClients();
    }
}