using AIProjectOrchestrator.Domain.Entities;

namespace AIProjectOrchestrator.Domain.Interfaces
{
    /// <summary>
    /// Repository for Project entities (int ID).
    /// Follows Interface Segregation Principle by using IFullRepository.
    /// </summary>
    public interface IProjectRepository : IFullRepository<Project, int>
    {
        // IFullRepository provides: GetByIdAsync(int), GetAllAsync, AddAsync, UpdateAsync, DeleteAsync(int)
        // No additional methods needed
    }
}
