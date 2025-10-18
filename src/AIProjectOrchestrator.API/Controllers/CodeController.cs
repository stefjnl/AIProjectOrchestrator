using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AIProjectOrchestrator.Domain.Services;
using AIProjectOrchestrator.Domain.Models.Code;

namespace AIProjectOrchestrator.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CodeController : ControllerBase
    {
        private readonly ICodeGenerationService _codeGenerationService;

        public CodeController(ICodeGenerationService codeGenerationService)
        {
            _codeGenerationService = codeGenerationService;
        }

        [HttpPost("generate")]
        public async Task<ActionResult<CodeGenerationResponse>> GenerateCode(
            [FromBody] CodeGenerationRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await _codeGenerationService.GenerateCodeAsync(request, cancellationToken).ConfigureAwait(false);
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

        [HttpGet("{generationId:guid}/status")]
        public async Task<ActionResult<CodeGenerationStatus>> GetStatus(
            Guid generationId,
            CancellationToken cancellationToken)
        {
            try
            {
                var status = await _codeGenerationService.GetStatusAsync(generationId, cancellationToken).ConfigureAwait(false);
                return Ok(status);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        [HttpGet("{generationId:guid}/artifacts")]
        public async Task<ActionResult<CodeArtifactsResult>> GetGeneratedCode(
            Guid generationId,
            CancellationToken cancellationToken)
        {
            try
            {
                var results = await _codeGenerationService.GetGeneratedCodeAsync(generationId, cancellationToken).ConfigureAwait(false);
                if (results.Artifacts == null || results.Artifacts.Count == 0)
                {
                    return NotFound(new { error = "Not found", message = "Code generation results not found" });
                }
                
                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        [HttpGet("can-generate/{storyGenerationId:guid}")]
        public async Task<ActionResult<bool>> CanGenerateCode(
            Guid storyGenerationId,
            CancellationToken cancellationToken)
        {
            try
            {
                var canGenerate = await _codeGenerationService.CanGenerateCodeAsync(storyGenerationId, cancellationToken).ConfigureAwait(false);
                return Ok(canGenerate);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        [HttpGet("{generationId:guid}/download")]
        public async Task<IActionResult> DownloadGeneratedFiles(
            Guid generationId,
            CancellationToken cancellationToken)
        {
            try
            {
                var zipFile = await _codeGenerationService.GetGeneratedFilesZipAsync(generationId, cancellationToken).ConfigureAwait(false);
                if (zipFile == null)
                    return NotFound(new { error = "Not found", message = "Generated files not found or not approved" });

                return File(zipFile, "application/zip", $"generated-code-{generationId}.zip");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }
    }
}