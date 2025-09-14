using System;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Models;
using AIProjectOrchestrator.Domain.Models.AI;
using AIProjectOrchestrator.Domain.Models.Stories;

namespace AIProjectOrchestrator.Domain.Interfaces
{
    public interface IStoryContextBuilder
    {
        Task<AIRequest> BuildAsync(Guid planningId, StoryGenerationRequest request, Guid generationId, CancellationToken cancellationToken = default);
    }
}
