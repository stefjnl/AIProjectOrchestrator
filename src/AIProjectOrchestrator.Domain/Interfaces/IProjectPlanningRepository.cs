using AIProjectOrchestrator.Domain.Entities;

namespace AIProjectOrchestrator.Domain.Interfaces
{
    /// <summary>
    /// Repository for ProjectPlanning entities (int ID).
    /// Follows Interface Segregation Principle by using IFullRepository.
    /// Includes domain-specific query methods for planning workflow operations.
    /// </summary>
    public interface IProjectPlanningRepository : IFullRepository<ProjectPlanning, int>
    {
        // IFullRepository provides: GetByIdAsync(int), GetAllAsync, AddAsync, UpdateAsync, DeleteAsync(int)
        
        // Domain-specific query methods
        Task<ProjectPlanning?> GetByPlanningIdAsync(string planningId, CancellationToken cancellationToken = default);
        Task<ProjectPlanning?> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default);
    }
}
