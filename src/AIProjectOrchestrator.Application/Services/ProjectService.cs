using AIProjectOrchestrator.Application.Interfaces;
using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Interfaces;
using System.Reflection.Emit;

namespace AIProjectOrchestrator.Application.Services;

/// <summary>
/// The ProjectService handles core project management operations. It provides basic CRUD functionality for software projects, including creating,
/// retrieving, updating, and deleting project entities.It interacts with the data layer through repository patterns and serves as the main business logic layer for project-related operations in the AI Project Orchestrator.
/// </summary>
public class ProjectService(IProjectRepository projectRepository) : IProjectService
{
    public async Task<IEnumerable<Project>> GetAllProjectsAsync()
    {
        return await projectRepository.GetAllAsync();
    }

    public async Task<Project?> GetProjectByIdAsync(int id)
    {
        return await projectRepository.GetByIdAsync(id);
    }

    public async Task<Project?> GetProjectAsync(int id, CancellationToken cancellationToken = default)
    {
        return await projectRepository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<Project> CreateProjectAsync(Project project)
    {
        return await projectRepository.AddAsync(project);
    }

    public async Task<Project> UpdateProjectAsync(Project project)
    {
        return await projectRepository.UpdateAsync(project);
    }

    public async Task DeleteProjectAsync(int id)
    {
        await projectRepository.DeleteAsync(id);
    }
}