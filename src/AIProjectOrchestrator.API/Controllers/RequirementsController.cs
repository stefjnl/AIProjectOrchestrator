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
    public class RequirementsController : ControllerBase
    {
        private readonly IRequirementsAnalysisService _requirementsAnalysisService;

        public RequirementsController(IRequirementsAnalysisService requirementsAnalysisService)
        {
            _requirementsAnalysisService = requirementsAnalysisService;
        }

        [HttpPost("analyze")]
        public async Task<ActionResult<RequirementsAnalysisResponse>> AnalyzeRequirements(
            [FromBody] RequirementsAnalysisRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await _requirementsAnalysisService.AnalyzeRequirementsAsync(request, cancellationToken);
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

        [HttpGet("{analysisId:guid}/status")]
        public async Task<ActionResult<RequirementsAnalysisStatus>> GetAnalysisStatus(
            Guid analysisId,
            CancellationToken cancellationToken)
        {
            try
            {
                var status = await _requirementsAnalysisService.GetAnalysisStatusAsync(analysisId, cancellationToken);
                return Ok(status);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }
    }
}