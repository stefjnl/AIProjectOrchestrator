using AIProjectOrchestrator.Application.Interfaces;
using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Interfaces;
using AIProjectOrchestrator.Domain.Services;
using System.Reflection.Emit;

namespace AIProjectOrchestrator.Application.Services;

/// <summary>
/// The ProjectService handles core project management operations. It provides basic CRUD functionality for software projects, including creating,
/// retrieving, updating, and deleting project entities.It interacts with the data layer through repository patterns and serves as the main business logic layer for project-related operations in the AI Project Orchestrator.
/// </summary>
public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IReviewService _reviewService;

    public ProjectService(IProjectRepository projectRepository, IReviewService reviewService)
    {
        _projectRepository = projectRepository;
        _reviewService = reviewService;
    }

    public async Task<IEnumerable<Project>> GetAllProjectsAsync()
    {
        return await _projectRepository.GetAllAsync();
    }

    public async Task<Project?> GetProjectByIdAsync(int id)
    {
        return await _projectRepository.GetByIdAsync(id);
    }

    public async Task<Project?> GetProjectAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _projectRepository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<Project> CreateProjectAsync(Project project)
    {
        return await _projectRepository.AddAsync(project);
    }

    public async Task<Project> UpdateProjectAsync(Project project)
    {
        await _projectRepository.UpdateAsync(project);
        return project;
    }

    public async Task DeleteProjectAsync(int id)
    {
        // First delete all reviews associated with this project to maintain referential integrity
        await _reviewService.DeleteReviewsByProjectIdAsync(id);
        
        // Then delete the project itself
        await _projectRepository.DeleteAsync(id);
    }
}