using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Interfaces;
using AIProjectOrchestrator.Domain.Models.Stories;

namespace AIProjectOrchestrator.Domain.Repositories
{
    public interface IStoryGenerationRepository : IRepository<StoryGeneration>
    {
        Task<StoryGeneration?> GetByGenerationIdAsync(string generationId, CancellationToken cancellationToken = default);
        Task<StoryGeneration?> GetByProjectPlanningIdAsync(int projectPlanningId, CancellationToken cancellationToken = default);
        Task<IEnumerable<StoryGeneration>> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default);
        Task<StoryGeneration?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        // New methods for individual story management
        Task<UserStory?> GetStoryByIdAsync(Guid storyId, CancellationToken cancellationToken = default);
        Task UpdateStoryAsync(UserStory story, CancellationToken cancellationToken = default);
        Task<List<UserStory>> GetStoriesByGenerationIdAsync(Guid generationId, CancellationToken cancellationToken = default);
    }
}