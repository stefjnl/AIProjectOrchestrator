using AIProjectOrchestrator.Domain.Entities;

namespace AIProjectOrchestrator.Domain.Interfaces
{
    public interface IProjectRepository : IRepository<Project>
    {
        Task<Project> UpdateAsync(Project project);
    }
}
