using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Interfaces;
using AIProjectOrchestrator.Domain.Models.Stories;

namespace AIProjectOrchestrator.Domain.Interfaces
{
    /// <summary>
    /// Repository for StoryGeneration entities (int ID).
    /// Follows Interface Segregation Principle by using IFullRepository.
    /// Includes domain-specific query methods for story generation workflow operations.
    /// </summary>
    public interface IStoryGenerationRepository : IFullRepository<StoryGeneration, int>
    {
        // IFullRepository provides: GetByIdAsync(int), GetAllAsync, AddAsync, UpdateAsync, DeleteAsync(int)
        
        // Domain-specific query methods for StoryGeneration
        Task<StoryGeneration?> GetByGenerationIdAsync(string generationId, CancellationToken cancellationToken = default);
        Task<StoryGeneration?> GetByProjectPlanningIdAsync(int projectPlanningId, CancellationToken cancellationToken = default);
        Task<IEnumerable<StoryGeneration>> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default);

        // Methods for individual story management
        Task<UserStory?> GetStoryByIdAsync(Guid storyId, CancellationToken cancellationToken = default);
        Task UpdateStoryAsync(UserStory story, CancellationToken cancellationToken = default);
        Task<List<UserStory>> GetStoriesByGenerationIdAsync(Guid generationId, CancellationToken cancellationToken = default);
    }
}
