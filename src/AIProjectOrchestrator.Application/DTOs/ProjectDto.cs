using AIProjectOrchestrator.Domain.Entities;

namespace AIProjectOrchestrator.Application.DTOs;

/// <summary>
/// Data Transfer Object for Project entity with API-friendly property names
/// </summary>
public class ProjectDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// Alias for CreatedDate to support frontend compatibility.
    /// This property exists only in the DTO layer to keep domain pure.
    /// </summary>
    public DateTime CreatedAt => CreatedDate;
    
    /// <summary>
    /// Maps domain entity to DTO
    /// </summary>
    public static ProjectDto FromEntity(Project project)
    {
        return new ProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            CreatedDate = project.CreatedDate,
            UpdatedDate = project.UpdatedDate,
            Status = project.Status,
            Type = project.Type
        };
    }
    
    /// <summary>
    /// Maps DTO to domain entity (for creation/updates)
    /// </summary>
    public Project ToEntity()
    {
        return new Project
        {
            Id = this.Id,
            Name = this.Name,
            Description = this.Description,
            CreatedDate = this.CreatedDate,
            UpdatedDate = this.UpdatedDate,
            Status = this.Status,
            Type = this.Type
        };
    }
}
