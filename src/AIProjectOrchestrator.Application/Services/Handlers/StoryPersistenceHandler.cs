using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Interfaces;
using AIProjectOrchestrator.Domain.Models;
using AIProjectOrchestrator.Domain.Models.AI;
using AIProjectOrchestrator.Domain.Models.Review;
using AIProjectOrchestrator.Domain.Models.Stories;
using AIProjectOrchestrator.Domain.Services;
using Microsoft.Extensions.Logging;

namespace AIProjectOrchestrator.Application.Services.Handlers
{
    public class StoryPersistenceHandler : IStoryPersistenceHandler
    {
        private readonly IStoryGenerationRepository _storyGenerationRepository;
        private readonly IProjectPlanningRepository _projectPlanningRepository;
        private readonly Lazy<IReviewService> _reviewService;
        private readonly ILogger<StoryPersistenceHandler> _logger;

        public StoryPersistenceHandler(
            IStoryGenerationRepository storyGenerationRepository,
            IProjectPlanningRepository projectPlanningRepository,
            Lazy<IReviewService> reviewService,
            ILogger<StoryPersistenceHandler> logger)
        {
            _storyGenerationRepository = storyGenerationRepository;
            _projectPlanningRepository = projectPlanningRepository;
            _reviewService = reviewService;
            _logger = logger;
        }

        public async Task<StoryGenerationResponse> SaveAsync(
            List<UserStory> stories,
            AIResponse aiResponse,
            Guid planningId,
            Guid generationId,
            string projectId,
            CancellationToken cancellationToken = default)
        {
            // Create and store the story generation entity first to get the entity ID
            // Get the ProjectPlanning entity ID
            var projectPlanning = await _projectPlanningRepository.GetByPlanningIdAsync(planningId.ToString(), cancellationToken);
            if (projectPlanning == null)
            {
                _logger.LogError("Story persistence {GenerationId} failed: Project planning not found", generationId);
                throw new InvalidOperationException("Project planning not found");
            }

            var storyGenerationEntity = new StoryGeneration
            {
                GenerationId = generationId.ToString(),
                ProjectPlanningId = projectPlanning.Id,
                Status = StoryGenerationStatus.PendingReview,
                ReviewId = string.Empty, // Will be updated after review submission
                CreatedDate = DateTime.UtcNow,
                Content = aiResponse.Content, // Store the AI response content
                Stories = stories // Set the stories collection for cascade insert
            };

            // Save the StoryGeneration entity - this will cascade insert the UserStory entities
            await _storyGenerationRepository.AddAsync(storyGenerationEntity, cancellationToken);
            var savedStoryGenerationId = storyGenerationEntity.Id; // Get the database-generated int ID

            // Update each story with the correct StoryGenerationId for consistency
            foreach (var story in stories)
            {
                story.StoryGenerationId = savedStoryGenerationId;
            }

            // Submit for review
            _logger.LogDebug("Submitting AI response for review in story generation {GenerationId}", generationId);
            var correlationId = Guid.NewGuid().ToString();

            var reviewRequest = new SubmitReviewRequest
            {
                ServiceName = "StoryGeneration",
                Content = aiResponse.Content, // We can still send the full content for review purposes
                CorrelationId = correlationId,
                PipelineStage = "Stories",
                OriginalRequest = null, // Not available here, but original had aiRequest; can be null or adjust
                AIResponse = aiResponse,
                Metadata = new Dictionary<string, object>
                {
                    { "GenerationId", generationId },
                    { "EntityId", savedStoryGenerationId }, // Pass the entity int ID for FK linking
                    { "PlanningId", planningId },
                    { "ProjectId", projectId }
                }
            };

            var reviewResponse = await _reviewService.Value.SubmitForReviewAsync(reviewRequest, cancellationToken);

            // Update the story generation entity with the review ID
            storyGenerationEntity.ReviewId = reviewResponse.ReviewId.ToString();
            await _storyGenerationRepository.UpdateAsync(storyGenerationEntity, cancellationToken);

            // Create the response object
            var response = new StoryGenerationResponse
            {
                GenerationId = generationId,
                PlanningId = planningId,
                Stories = stories,
                ReviewId = reviewResponse.ReviewId,
                Status = StoryGenerationStatus.PendingReview,
                CreatedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Story persistence {GenerationId} completed successfully. Review ID: {ReviewId}",
                generationId, reviewResponse.ReviewId);

            return response;
        }
    }
}
