using Microsoft.AspNetCore.Mvc;
using AIProjectOrchestrator.Application.Interfaces;
using AIProjectOrchestrator.Application.DTOs;

namespace AIProjectOrchestrator.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService _projectService;

        public ProjectsController(IProjectService projectService)
        {
            _projectService = projectService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProjectDto>>> GetProjects()
        {
            var projects = await _projectService.GetAllProjectsAsync().ConfigureAwait(false);
            var dtos = projects.Select(ProjectDto.FromEntity);
            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProjectDto>> GetProject(int id)
        {
            var project = await _projectService.GetProjectByIdAsync(id).ConfigureAwait(false);
            if (project == null)
            {
                return NotFound();
            }
            return Ok(ProjectDto.FromEntity(project));
        }

        [HttpPost]
        public async Task<ActionResult<ProjectDto>> CreateProject(ProjectDto projectDto)
        {
            var project = projectDto.ToEntity();
            var createdProject = await _projectService.CreateProjectAsync(project).ConfigureAwait(false);
            var dto = ProjectDto.FromEntity(createdProject);
            return CreatedAtAction(nameof(GetProject), new { id = dto.Id }, dto);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            try
            {
                await _projectService.DeleteProjectAsync(id).ConfigureAwait(false);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }
    }
}
