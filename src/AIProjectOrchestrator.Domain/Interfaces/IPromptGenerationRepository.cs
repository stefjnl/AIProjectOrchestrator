using AIProjectOrchestrator.Domain.Entities;

namespace AIProjectOrchestrator.Domain.Interfaces
{
    /// <summary>
    /// Repository for PromptGeneration entities (int ID).
    /// Follows Interface Segregation Principle by using IFullRepository.
    /// Includes domain-specific query methods for prompt generation workflow operations.
    /// </summary>
    public interface IPromptGenerationRepository : IFullRepository<PromptGeneration, int>
    {
        // IFullRepository provides: GetByIdAsync(int), GetAllAsync, AddAsync, UpdateAsync, DeleteAsync(int)
        
        // Domain-specific query methods
        Task<PromptGeneration?> GetByPromptIdAsync(string promptId, CancellationToken cancellationToken = default);
        Task<PromptGeneration?> GetByUserStoryIdAndIndexAsync(Guid userStoryId, int storyIndex, CancellationToken cancellationToken = default);
        Task<IEnumerable<PromptGeneration>> GetByUserStoryIdAsync(Guid userStoryId, CancellationToken cancellationToken = default);
        Task<IEnumerable<PromptGeneration>> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default);
    }
}
