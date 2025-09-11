using AIProjectOrchestrator.Domain.Entities;

namespace AIProjectOrchestrator.Domain.Interfaces
{
    public interface IPromptGenerationRepository : IRepository<PromptGeneration>
    {
        Task<PromptGeneration?> GetByPromptIdAsync(string promptId, CancellationToken cancellationToken = default);
        Task<PromptGeneration?> GetByUserStoryIdAndIndexAsync(Guid userStoryId, int storyIndex, CancellationToken cancellationToken = default);
        Task<IEnumerable<PromptGeneration>> GetByUserStoryIdAsync(Guid userStoryId, CancellationToken cancellationToken = default);
        Task<IEnumerable<PromptGeneration>> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default);
    }
}
