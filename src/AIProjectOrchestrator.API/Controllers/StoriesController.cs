using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AIProjectOrchestrator.Domain.Services;
using AIProjectOrchestrator.Domain.Models.Stories;

namespace AIProjectOrchestrator.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StoriesController : ControllerBase
    {
        private readonly IStoryGenerationService _storyGenerationService;

        public StoriesController(IStoryGenerationService storyGenerationService)
        {
            _storyGenerationService = storyGenerationService;
        }

        [HttpPost("generate")]
        public async Task<ActionResult<StoryGenerationResponse>> GenerateStories(
            [FromBody] StoryGenerationRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await _storyGenerationService.GenerateStoriesAsync(request, cancellationToken);
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
        public async Task<ActionResult<StoryGenerationStatus>> GetGenerationStatus(
            Guid generationId,
            CancellationToken cancellationToken)
        {
            try
            {
                var status = await _storyGenerationService.GetGenerationStatusAsync(generationId, cancellationToken);
                return Ok(status);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        [HttpGet("{generationId:guid}/results")]
        public async Task<ActionResult<List<UserStory>>> GetGenerationResults(
            Guid generationId,
            CancellationToken cancellationToken)
        {
            try
            {
                var results = await _storyGenerationService.GetGenerationResultsAsync(generationId, cancellationToken);
                if (results == null)
                {
                    return NotFound(new { error = "Not found", message = "Story generation results not found" });
                }
                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        [HttpGet("can-generate/{planningId:guid}")]
        public async Task<ActionResult<bool>> CanGenerateStories(
            Guid planningId,
            CancellationToken cancellationToken)
        {
            try
            {
                var canGenerate = await _storyGenerationService.CanGenerateStoriesAsync(planningId, cancellationToken);
                return Ok(canGenerate);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }
    }
}
