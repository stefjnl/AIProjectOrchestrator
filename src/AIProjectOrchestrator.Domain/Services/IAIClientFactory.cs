using System.Collections.Generic;
using System.Linq;

namespace AIProjectOrchestrator.Domain.Services
{
    public interface IAIClientFactory
    {
        IAIClient? GetClient(string providerName);
        IEnumerable<IAIClient> GetAllClients();
        Task<IEnumerable<string>> GetModelsAsync(string providerName);
    }
}