using AIProjectOrchestrator.Domain.Entities;

namespace AIProjectOrchestrator.Domain.Interfaces
{
    public interface IRequirementsAnalysisRepository : IRepository<RequirementsAnalysis>
    {
        Task<RequirementsAnalysis?> GetByAnalysisIdAsync(string analysisId, CancellationToken cancellationToken = default);
        Task<RequirementsAnalysis?> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default);
        new Task<RequirementsAnalysis?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<int?> GetEntityIdByAnalysisIdAsync(string analysisId, CancellationToken cancellationToken = default);
    }
}
