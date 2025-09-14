using AIProjectOrchestrator.Domain.Common;
using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Interfaces;
using AIProjectOrchestrator.Infrastructure.Data;

namespace AIProjectOrchestrator.Infrastructure.Repositories
{
    public class ProjectRepository : Repository<Project>, IProjectRepository
    {
        public ProjectRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Project> AddAsync(Project project)
        {
            project.CreatedDate = DateTime.UtcNow;
            project.UpdatedDate = DateTime.UtcNow;
            
            // Ensure default values for new fields if not set
            if (string.IsNullOrEmpty(project.Status))
            {
                project.Status = ProjectConstants.ActiveStatus;
            }
            
            if (string.IsNullOrEmpty(project.Type))
            {
                project.Type = ProjectConstants.WebType;
            }
            
            return await base.AddAsync(project);
        }

        public async Task<Project> UpdateAsync(Project project)
        {
            project.UpdatedDate = DateTime.UtcNow;
            
            await base.UpdateAsync(project);
            return project;
        }
    }
}