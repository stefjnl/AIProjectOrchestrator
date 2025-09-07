using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using AIProjectOrchestrator.Domain.Services;
using AIProjectOrchestrator.Domain.Models.Stories;
using AIProjectOrchestrator.Domain.Models;

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

        [HttpPost("{generationId:guid}/approve")]
        public async Task<IActionResult> ApproveStories(Guid generationId, CancellationToken cancellationToken)
        {
            try
            {
                await _storyGenerationService.UpdateGenerationStatusAsync(generationId, StoryGenerationStatus.Approved, cancellationToken);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        [HttpGet("{storyGenerationId:guid}/approved")]
        public async Task<ActionResult<List<UserStoryDto>>> GetApprovedStories(Guid storyGenerationId, CancellationToken cancellationToken)
        {
            try
            {
                var stories = await _storyGenerationService.GetApprovedStoriesAsync(storyGenerationId, cancellationToken);
                if (stories == null || !stories.Any())
                {
                    return NotFound(new { error = "Not found", message = "No approved stories found for this generation" });
                }

                var storyDtos = stories.Select((story, index) => new UserStoryDto
                {
                    Index = index,
                    Title = story.Title,
                    AsA = "User", // Fallback; parse from Description if structured
                    IWant = story.Description.Length > 0 ? story.Description.Substring(0, Math.Min(50, story.Description.Length)) + (story.Description.Length > 50 ? "..." : "") : "To perform an action",
                    SoThat = "To achieve project goals", // Fallback
                    AcceptanceCriteria = story.AcceptanceCriteria,
                    StoryPoints = story.StoryPoints
                }).ToList();

                return Ok(storyDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        [HttpGet("{storyId:guid}/status")]
        public async Task<ActionResult<StoryStatus>> GetStoryStatus(Guid storyId, CancellationToken cancellationToken)
        {
            try
            {
                var status = await _storyGenerationService.GetStoryStatusAsync(storyId, cancellationToken);
                return Ok(status);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        [HttpPut("{storyId:guid}/approve")]
        public async Task<IActionResult> ApproveStory(Guid storyId, CancellationToken cancellationToken)
        {
            try
            {
                await _storyGenerationService.UpdateStoryStatusAsync(storyId, StoryStatus.Approved, cancellationToken);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        [HttpPut("{storyId:guid}/reject")]
        public async Task<IActionResult> RejectStory(Guid storyId, CancellationToken cancellationToken)
        {
            try
            {
                await _storyGenerationService.UpdateStoryStatusAsync(storyId, StoryStatus.Rejected, cancellationToken);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        [HttpPut("{storyId:guid}/edit")]
        public async Task<IActionResult> EditStory(Guid storyId, [FromBody] UserStory updatedStory, CancellationToken cancellationToken)
        {
            try
            {
                updatedStory.Id = storyId;
                await _storyGenerationService.UpdateStoryAsync(storyId, updatedStory, cancellationToken);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }
    }
}
