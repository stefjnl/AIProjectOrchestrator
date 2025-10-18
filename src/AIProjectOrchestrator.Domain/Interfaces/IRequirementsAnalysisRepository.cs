using AIProjectOrchestrator.Domain.Entities;

namespace AIProjectOrchestrator.Domain.Interfaces
{
    /// <summary>
    /// Repository for RequirementsAnalysis entities (int ID).
    /// Follows Interface Segregation Principle by using IFullRepository.
    /// Includes domain-specific query methods for requirements analysis workflow operations.
    /// </summary>
    public interface IRequirementsAnalysisRepository : IFullRepository<RequirementsAnalysis, int>
    {
        // IFullRepository provides: GetByIdAsync(int), GetAllAsync, AddAsync, UpdateAsync, DeleteAsync(int)
        
        // Domain-specific query methods
        Task<RequirementsAnalysis?> GetByAnalysisIdAsync(string analysisId, CancellationToken cancellationToken = default);
        Task<RequirementsAnalysis?> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default);
        Task<int?> GetEntityIdByAnalysisIdAsync(string analysisId, CancellationToken cancellationToken = default);
    }
}
