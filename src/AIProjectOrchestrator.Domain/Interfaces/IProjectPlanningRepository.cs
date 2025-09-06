using AIProjectOrchestrator.Domain.Entities;

namespace AIProjectOrchestrator.Domain.Interfaces
{
    public interface IProjectPlanningRepository : IRepository<ProjectPlanning>
    {
        Task<ProjectPlanning?> GetByPlanningIdAsync(string planningId, CancellationToken cancellationToken = default);
        Task<ProjectPlanning?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    }
}
