using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Interfaces;
using AIProjectOrchestrator.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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
            return await _context.Projects.ToListAsync();
        }

        public async Task<Project?> GetByIdAsync(int id)
        {
            return await _context.Projects.FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Project> AddAsync(Project project)
        {
            project.CreatedAt = DateTime.UtcNow;
            project.UpdatedAt = DateTime.UtcNow;
            
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();
            
            return project;
        }

        public async Task<Project> UpdateAsync(Project project)
        {
            project.UpdatedAt = DateTime.UtcNow;
            
            _context.Projects.Update(project);
            await _context.SaveChangesAsync();
            
            return project;
        }

        public async Task DeleteAsync(int id)
        {
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id);
            if (project != null)
            {
                _context.Projects.Remove(project);
                await _context.SaveChangesAsync();
            }
        }
    }
}