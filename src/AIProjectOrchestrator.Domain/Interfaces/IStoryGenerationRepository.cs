using AIProjectOrchestrator.Domain.Entities;

namespace AIProjectOrchestrator.Domain.Interfaces
{
    public interface IStoryGenerationRepository : IRepository<StoryGeneration>
    {
        Task<StoryGeneration?> GetByGenerationIdAsync(string generationId, CancellationToken cancellationToken = default);
        Task<StoryGeneration?> GetByProjectPlanningIdAsync(int projectPlanningId, CancellationToken cancellationToken = default);
        Task<IEnumerable<StoryGeneration>> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default);
        Task<StoryGeneration?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    }
}
