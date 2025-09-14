using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Models;
using AIProjectOrchestrator.Domain.Models.AI;
using AIProjectOrchestrator.Domain.Models.Review;
using AIProjectOrchestrator.Domain.Models.Stories;

namespace AIProjectOrchestrator.Domain.Interfaces
{
    public interface IStoryPersistenceHandler
    {
        Task<StoryGenerationResponse> SaveAsync(
            List<UserStory> stories,
            AIResponse aiResponse,
            Guid planningId,
            Guid generationId,
            string projectId,
            CancellationToken cancellationToken = default);
    }
}
