using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Services;
using AIProjectOrchestrator.Domain.Models.PromptGeneration;

namespace AIProjectOrchestrator.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PromptGenerationController : ControllerBase
    {
        private readonly IPromptGenerationService _promptGenerationService;
        private readonly ILogger<PromptGenerationController> _logger;

        public PromptGenerationController(
            IPromptGenerationService promptGenerationService,
            ILogger<PromptGenerationController> logger)
        {
            _promptGenerationService = promptGenerationService;
            _logger = logger;
        }

        [HttpPost("generate")]
        public async Task<ActionResult<PromptGenerationResponse>> GeneratePrompt(
            [FromBody] PromptGenerationRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var response = await _promptGenerationService.GeneratePromptAsync(request, cancellationToken).ConfigureAwait(false);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid request for prompt generation");
                return BadRequest(new { error = "Invalid request", message = ex.Message });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Prerequisites"))
            {
                _logger.LogError(ex, "Prerequisites not met for prompt generation");
                return Conflict(new { error = "Conflict", message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Service unavailable for prompt generation");
                return StatusCode(503, new { error = "Service unavailable", message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal server error in prompt generation");
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        [HttpGet("{promptId}/status")]
        public async Task<ActionResult<PromptGenerationStatus>> GetPromptStatus(
            string promptId,
            CancellationToken cancellationToken)
        {
            try
            {
                if (!Guid.TryParse(promptId, out var guid))
                {
                    return BadRequest(new { error = "Invalid prompt ID", message = "Prompt ID must be a valid GUID" });
                }

                var status = await _promptGenerationService.GetPromptStatusAsync(guid, cancellationToken).ConfigureAwait(false);
                return Ok(status);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
            {
                _logger.LogWarning(ex, "Prompt status not found for {PromptId}", promptId);
                return NotFound(new { error = "Not found", message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal server error getting prompt status for {PromptId}", promptId);
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        [HttpGet("can-generate/{storyGenerationId}/{storyIndex}")]
        public async Task<ActionResult<bool>> CanGeneratePrompt(
            string storyGenerationId,
            int storyIndex,
            CancellationToken cancellationToken)
        {
            try
            {
                if (!Guid.TryParse(storyGenerationId, out var guid))
                {
                    return BadRequest(new { error = "Invalid story generation ID", message = "Story generation ID must be a valid GUID" });
                }

                var canGenerate = await _promptGenerationService.CanGeneratePromptAsync(guid, storyIndex, cancellationToken).ConfigureAwait(false);
                return Ok(canGenerate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal server error checking if prompt can be generated for {StoryGenerationId}, index {StoryIndex}", storyGenerationId, storyIndex);
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        [HttpGet("{promptId}")]
        public async Task<ActionResult<PromptGenerationResponse>> GetPrompt(
            string promptId,
            CancellationToken cancellationToken)
        {
            try
            {
                if (!Guid.TryParse(promptId, out var guid))
                {
                    return BadRequest(new { error = "Invalid prompt ID", message = "Prompt ID must be a valid GUID" });
                }

                var response = await _promptGenerationService.GetPromptAsync(guid, cancellationToken).ConfigureAwait(false);
                if (response == null)
                {
                    return NotFound(new { error = "Not found", message = "Prompt not found" });
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal server error getting prompt {PromptId}", promptId);
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }
    }
}
