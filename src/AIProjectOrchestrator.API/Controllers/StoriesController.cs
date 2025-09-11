using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using AIProjectOrchestrator.Domain.Services;
using AIProjectOrchestrator.Domain.Interfaces;
using AIProjectOrchestrator.Domain.Models.Stories;
using AIProjectOrchestrator.Domain.Models;
using AIProjectOrchestrator.Domain.Entities;

namespace AIProjectOrchestrator.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StoriesController : ControllerBase
    {
        private readonly IStoryGenerationService _storyGenerationService;
        private readonly IStoryGenerationRepository _storyGenerationRepository;
        private readonly ILogger<StoriesController> _logger;

        public StoriesController(
            IStoryGenerationService storyGenerationService,
            IStoryGenerationRepository storyGenerationRepository,
            ILogger<StoriesController> logger)
        {
            _storyGenerationService = storyGenerationService;
            _storyGenerationRepository = storyGenerationRepository;
            _logger = logger;
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

        [HttpGet("generations/{generationId:guid}/status")]
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

        [HttpGet("generations/{generationId:guid}/results")]
        public async Task<ActionResult<List<AIProjectOrchestrator.Domain.Models.Stories.UserStoryDto>>> GetGenerationResults(
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

                // Project to DTOs to avoid circular references
                var dtos = results.Select((story, index) => new AIProjectOrchestrator.Domain.Models.Stories.UserStoryDto
                {
                    Id = story.Id,
                    Index = index,
                    Title = story.Title,
                    Description = story.Description,
                    AcceptanceCriteria = story.AcceptanceCriteria ?? new List<string>(),
                    Priority = story.Priority,
                    StoryPoints = story.StoryPoints,
                    Tags = story.Tags ?? new List<string>(),
                    EstimatedComplexity = story.EstimatedComplexity,
                    Status = story.Status,
                    HasPrompt = story.HasPrompt,
                    PromptId = story.PromptId
                }).ToList();

                return Ok(dtos);
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

        [HttpPost("generations/{generationId:guid}/approve")]
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

        [HttpGet("generations/{storyGenerationId:guid}/approved")]
        public async Task<ActionResult<List<AIProjectOrchestrator.Domain.Models.Stories.UserStoryDto>>> GetApprovedStories(Guid storyGenerationId, CancellationToken cancellationToken)
        {
            try
            {
                var stories = await _storyGenerationService.GetApprovedStoriesAsync(storyGenerationId, cancellationToken);
                if (stories == null || !stories.Any())
                {
                    return NotFound(new { error = "Not found", message = "No approved stories found for this generation" });
                }

                var storyDtos = stories.Select((story, index) => new AIProjectOrchestrator.Domain.Models.Stories.UserStoryDto
                {
                    Id = story.Id,
                    Index = index,
                    Title = story.Title,
                    Description = story.Description,
                    AcceptanceCriteria = story.AcceptanceCriteria ?? new List<string>(),
                    Priority = story.Priority,
                    StoryPoints = story.StoryPoints,
                    Tags = story.Tags ?? new List<string>(),
                    EstimatedComplexity = story.EstimatedComplexity,
                    Status = story.Status,
                    HasPrompt = story.HasPrompt,
                    PromptId = story.PromptId
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
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = "Story not found", message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to approve story {StoryId}", storyId);
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        [HttpPut("{storyId:guid}/reject")]
        public async Task<IActionResult> RejectStory(Guid storyId, [FromBody] FeedbackRequest feedback, CancellationToken cancellationToken)
        {
            try
            {
                // Note: The service layer needs to be updated to handle the feedback.
                // For now, we just change the status.
                await _storyGenerationService.UpdateStoryStatusAsync(storyId, StoryStatus.Rejected, cancellationToken);
                _logger.LogInformation("Story {StoryId} rejected with feedback: {Feedback}", storyId, feedback.Feedback);
                return Ok();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = "Story not found", message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reject story {StoryId}", storyId);
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        [HttpPut("{storyId:guid}/edit")]
        public async Task<IActionResult> EditStory(Guid storyId, [FromBody] EditStoryRequest request, CancellationToken cancellationToken)
        {
            var updatedStory = request.UpdatedStory;
            _logger.LogInformation("EditStory called for storyId: {StoryId}", storyId);
            _logger.LogInformation("Received UpdateStoryDto: Title='{Title}', Description='{Description}', Status={Status}",
                updatedStory?.Title, updatedStory?.Description, updatedStory?.Status);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model validation failed for story {StoryId}: {ModelState}", storyId, ModelState);
                return BadRequest(new
                {
                    error = "Validation failed",
                    message = "Please check the required fields",
                    details = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            try
            {
                var userStory = new UserStory
                {
                    Id = storyId,
                    Title = updatedStory.Title ?? string.Empty,
                    Description = updatedStory.Description ?? string.Empty,
                    AcceptanceCriteria = updatedStory.AcceptanceCriteria ?? new List<string>(),
                    Priority = updatedStory.Priority ?? string.Empty,
                    StoryPoints = updatedStory.StoryPoints,
                    Tags = updatedStory.Tags ?? new List<string>(),
                    EstimatedComplexity = updatedStory.EstimatedComplexity,
                    Status = updatedStory.Status
                };

                _logger.LogInformation("Calling UpdateStoryAsync with UserStory: Id={Id}, Title='{Title}', Description='{Description}'",
                    userStory.Id, userStory.Title, userStory.Description);

                await _storyGenerationService.UpdateStoryAsync(storyId, userStory, cancellationToken);

                _logger.LogInformation("Successfully updated story {StoryId}", storyId);
                return Ok();
            }

            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Story not found for editing: {StoryId}", storyId);
                return NotFound(new { error = "Story not found", message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error editing story {StoryId}: {Message}\n{StackTrace}", storyId, ex.Message, ex.StackTrace);
                return StatusCode(500, new { error = "Internal server error", message = "An unexpected error occurred while updating the story" });
            }
        }
    }
}
