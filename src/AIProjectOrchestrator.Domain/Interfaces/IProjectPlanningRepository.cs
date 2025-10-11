using AIProjectOrchestrator.Domain.Entities;

namespace AIProjectOrchestrator.Domain.Interfaces
{
    public interface IProjectPlanningRepository : IRepository<ProjectPlanning>
    {
        Task<ProjectPlanning?> GetByPlanningIdAsync(string planningId, CancellationToken cancellationToken = default);
        new Task<ProjectPlanning?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        // Get planning by project for workflow state
        Task<ProjectPlanning?> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default);
    }
}
