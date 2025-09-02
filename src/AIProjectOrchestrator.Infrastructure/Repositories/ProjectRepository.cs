using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Interfaces;
using AIProjectOrchestrator.Infrastructure.Data;

namespace AIProjectOrchestrator.Infrastructure.Repositories
{
    public class ProjectRepository : IProjectRepository
    {
        private readonly AppDbContext _context;

        public ProjectRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Project>> GetAllAsync()
        {
            // Implementation would go here
            throw new NotImplementedException();
        }

        public async Task<Project?> GetByIdAsync(int id)
        {
            // Implementation would go here
            throw new NotImplementedException();
        }

        public async Task<Project> AddAsync(Project project)
        {
            // Implementation would go here
            throw new NotImplementedException();
        }

        public async Task<Project> UpdateAsync(Project project)
        {
            // Implementation would go here
            throw new NotImplementedException();
        }

        public async Task DeleteAsync(int id)
        {
            // Implementation would go here
            throw new NotImplementedException();
        }
    }
}