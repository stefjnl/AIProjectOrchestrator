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
        public async Task<RequirementsAnalysisResponse> AnalyzeRequirements(
            [FromBody] RequirementsAnalysisRequest request,
            CancellationToken cancellationToken)
        {
            // No try-catch needed - middleware handles exceptions
            return await _requirementsAnalysisService.AnalyzeRequirementsAsync(request, cancellationToken).ConfigureAwait(false);
        }

        [HttpGet("{analysisId:guid}/status")]
        public async Task<RequirementsAnalysisStatus> GetAnalysisStatus(
            Guid analysisId,
            CancellationToken cancellationToken)
        {
            return await _requirementsAnalysisService.GetAnalysisStatusAsync(analysisId, cancellationToken).ConfigureAwait(false);
        }

        [HttpGet("{analysisId:guid}")]
        public async Task<ActionResult<RequirementsAnalysisResponse>> GetAnalysis(
            Guid analysisId,
            CancellationToken cancellationToken)
        {
            var result = await _requirementsAnalysisService.GetAnalysisResultsAsync(analysisId, cancellationToken).ConfigureAwait(false);
            if (result == null)
            {
                return NotFound();
            }
            return result;
        }

        [HttpPost("{analysisId:guid}/approve")]
        public async Task<IActionResult> ApproveAnalysis(Guid analysisId, CancellationToken cancellationToken)
        {
            await _requirementsAnalysisService.UpdateAnalysisStatusAsync(analysisId, RequirementsAnalysisStatus.Approved, cancellationToken).ConfigureAwait(false);
            return Ok();
        }
    }
}