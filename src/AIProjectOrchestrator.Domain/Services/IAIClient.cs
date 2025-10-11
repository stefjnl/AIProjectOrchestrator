using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Models.AI;

namespace AIProjectOrchestrator.Domain.Services
{
    public interface IAIClient
    {
        string ProviderName { get; }
        Task<AIResponse> CallAsync(AIRequest request, CancellationToken cancellationToken = default);
        Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<string>> GetModelsAsync();
    }
}