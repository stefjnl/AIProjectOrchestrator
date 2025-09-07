using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AIProjectOrchestrator.Domain.Services;
using AIProjectOrchestrator.Domain.Models;

namespace AIProjectOrchestrator.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectPlanningController : ControllerBase
    {
        private readonly IProjectPlanningService _projectPlanningService;

        public ProjectPlanningController(IProjectPlanningService projectPlanningService)
        {
            _projectPlanningService = projectPlanningService;
        }

        [HttpPost("create")]
        public async Task<ActionResult<ProjectPlanningResponse>> CreateProjectPlan(
            [FromBody] ProjectPlanningRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await _projectPlanningService.CreateProjectPlanAsync(request, cancellationToken);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = "Invalid request", message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(503, new { error = "Service unavailable", message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        [HttpGet("{planningId:guid}/status")]
        public async Task<ActionResult<ProjectPlanningStatus>> GetPlanningStatus(
            Guid planningId,
            CancellationToken cancellationToken)
        {
            try
            {
                var status = await _projectPlanningService.GetPlanningStatusAsync(planningId, cancellationToken);
                return Ok(status);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        [HttpGet("{planningId:guid}")]
        public async Task<ActionResult<ProjectPlanningResponse>> GetPlanning(
            Guid planningId,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await _projectPlanningService.GetPlanningResultsAsync(planningId, cancellationToken);
                if (result == null)
                {
                    return NotFound(new { error = "Not found", message = "Project planning not found" });
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        [HttpGet("can-create/{requirementsAnalysisId:guid}")]
        public async Task<ActionResult<bool>> CanCreatePlan(
            Guid requirementsAnalysisId,
            CancellationToken cancellationToken)
        {
            try
            {
                var canCreate = await _projectPlanningService.CanCreatePlanAsync(requirementsAnalysisId, cancellationToken);
                return Ok(canCreate);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        [HttpPost("{planningId:guid}/approve")]
        public async Task<IActionResult> ApprovePlan(Guid planningId, CancellationToken cancellationToken)
        {
            try
            {
                await _projectPlanningService.UpdatePlanningStatusAsync(planningId, ProjectPlanningStatus.Approved, cancellationToken);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }
    }
}