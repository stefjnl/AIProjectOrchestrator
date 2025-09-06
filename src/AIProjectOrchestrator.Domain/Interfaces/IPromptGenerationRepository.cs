using AIProjectOrchestrator.Domain.Entities;

namespace AIProjectOrchestrator.Domain.Interfaces
{
    public interface IPromptGenerationRepository : IRepository<PromptGeneration>
    {
        Task<PromptGeneration?> GetByPromptIdAsync(string promptId, CancellationToken cancellationToken = default);
        Task<PromptGeneration?> GetByStoryGenerationIdAndIndexAsync(int storyGenerationId, int storyIndex, CancellationToken cancellationToken = default);
        Task<IEnumerable<PromptGeneration>> GetByStoryGenerationIdAsync(int storyGenerationId, CancellationToken cancellationToken = default);
    }
}
