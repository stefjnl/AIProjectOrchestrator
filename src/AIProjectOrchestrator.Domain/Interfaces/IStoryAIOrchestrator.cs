using System;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Models.AI;

namespace AIProjectOrchestrator.Domain.Interfaces
{
    public interface IStoryAIOrchestrator
    {
        Task<AIResponse> GenerateAsync(AIRequest request, Guid generationId, CancellationToken cancellationToken = default);
    }
}
