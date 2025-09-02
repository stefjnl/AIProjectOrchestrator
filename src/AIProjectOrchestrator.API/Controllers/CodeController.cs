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
                var response = await _codeGenerationService.GenerateCodeAsync(request, cancellationToken);
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
        public async Task<ActionResult<CodeGenerationStatus>> GetGenerationStatus(
            Guid generationId,
            CancellationToken cancellationToken)
        {
            try
            {
                var status = await _codeGenerationService.GetGenerationStatusAsync(generationId, cancellationToken);
                return Ok(status);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        [HttpGet("{generationId:guid}/results")]
        public async Task<ActionResult<CodeGenerationResponse>> GetGenerationResults(
            Guid generationId,
            CancellationToken cancellationToken)
        {
            try
            {
                var results = await _codeGenerationService.GetGenerationResultsAsync(generationId, cancellationToken);
                if (results == null)
                {
                    return NotFound(new { error = "Not found", message = "Code generation results not found" });
                }
                
                // Create a response object with the results
                var response = new CodeGenerationResponse
                {
                    GenerationId = generationId,
                    GeneratedFiles = results,
                    Status = await _codeGenerationService.GetGenerationStatusAsync(generationId, cancellationToken)
                };
                
                return Ok(response);
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
                var canGenerate = await _codeGenerationService.CanGenerateCodeAsync(storyGenerationId, cancellationToken);
                return Ok(canGenerate);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        [HttpGet("{generationId:guid}/files")]
        public async Task<ActionResult<CodeGenerationResponse>> GetGeneratedFiles(
            Guid generationId,
            CancellationToken cancellationToken)
        {
            try
            {
                var files = await _codeGenerationService.GetGeneratedFilesAsync(generationId, cancellationToken);
                
                // Create a response object with the files
                var response = new CodeGenerationResponse
                {
                    GenerationId = generationId,
                    GeneratedFiles = files,
                    Status = await _codeGenerationService.GetGenerationStatusAsync(generationId, cancellationToken)
                };
                
                return Ok(response);
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
                var zipFile = await _codeGenerationService.GetGeneratedFilesZipAsync(generationId, cancellationToken);
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