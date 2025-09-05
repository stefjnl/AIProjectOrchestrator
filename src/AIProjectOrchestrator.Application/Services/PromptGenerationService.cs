using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AIProjectOrchestrator.Domain.Services;
using AIProjectOrchestrator.Domain.Models.PromptGeneration;
using AIProjectOrchestrator.Domain.Models.Stories;
using AIProjectOrchestrator.Domain.Exceptions;

namespace AIProjectOrchestrator.Application.Services
{
    public class PromptGenerationService : IPromptGenerationService
    {
        private readonly IStoryGenerationService _storyGenerationService;
        private readonly IInstructionService _instructionService;
        private readonly ILogger<PromptGenerationService> _logger;
        private readonly ConcurrentDictionary<Guid, PromptGenerationStatus> _promptStatuses;
        private readonly ConcurrentDictionary<Guid, PromptGenerationResponse> _promptResults;

        public PromptGenerationService(
            IStoryGenerationService storyGenerationService,
            IInstructionService instructionService,
            ILogger<PromptGenerationService> logger)
        {
            _storyGenerationService = storyGenerationService;
            _instructionService = instructionService;
            _logger = logger;
            _promptStatuses = new ConcurrentDictionary<Guid, PromptGenerationStatus>();
            _promptResults = new ConcurrentDictionary<Guid, PromptGenerationResponse>();
        }

        public async Task<PromptGenerationResponse> GeneratePromptAsync(
            PromptGenerationRequest request,
            CancellationToken cancellationToken = default)
        {
            var promptId = Guid.NewGuid();
            _logger.LogInformation("Starting prompt generation {PromptId} for story generation: {StoryGenerationId}, index: {StoryIndex}",
                promptId, request.StoryGenerationId, request.StoryIndex);

            try
            {
                // Check for cancellation
                cancellationToken.ThrowIfCancellationRequested();

                // Validate prerequisites
                var canGenerate = await CanGeneratePromptAsync(request.StoryGenerationId, request.StoryIndex, cancellationToken);
                if (!canGenerate)
                {
                    throw new InvalidOperationException("Cannot generate prompt: Prerequisites not met");
                }

                // Retrieve the specific story
                var story = await _storyGenerationService.GetIndividualStoryAsync(
                    request.StoryGenerationId, request.StoryIndex, cancellationToken);

                // Set status to processing
                _promptStatuses[promptId] = PromptGenerationStatus.Processing;

                // Check for cancellation again
                cancellationToken.ThrowIfCancellationRequested();

                // For now, return a placeholder response
                // In future phases, we'll implement the actual prompt generation logic
                var response = new PromptGenerationResponse
                {
                    PromptId = promptId,
                    GeneratedPrompt = $"Generated prompt for story: {story.Title}",
                    ReviewId = Guid.NewGuid(),
                    Status = PromptGenerationStatus.PendingReview,
                    CreatedAt = DateTime.UtcNow
                };

                // Check for cancellation before completing
                cancellationToken.ThrowIfCancellationRequested();

                // Set status to pending review
                _promptStatuses[promptId] = PromptGenerationStatus.PendingReview;

                // Store the result for later retrieval
                _promptResults[promptId] = response;

                _logger.LogInformation("Prompt generation {PromptId} completed successfully. Review ID: {ReviewId}",
                    promptId, response.ReviewId);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Prompt generation {PromptId} failed with exception", promptId);
                _promptStatuses[promptId] = PromptGenerationStatus.Failed;
                throw;
            }
        }

        public Task<PromptGenerationStatus> GetPromptStatusAsync(
            Guid promptId,
            CancellationToken cancellationToken = default)
        {
            // Check for cancellation
            cancellationToken.ThrowIfCancellationRequested();

            if (_promptStatuses.TryGetValue(promptId, out var status))
            {
                return Task.FromResult(status);
            }

            // If we don't have the status in memory, it might have been cleaned up
            // In a production system, we would check a persistent store
            return Task.FromResult(PromptGenerationStatus.Failed);
        }

        public async Task<bool> CanGeneratePromptAsync(
            Guid storyGenerationId,
            int storyIndex,
            CancellationToken cancellationToken = default)
        {
            // Check for cancellation
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // Check if story generation exists and is approved
                var isApproved = await PromptPrerequisiteValidator.ValidateStoryApprovalAsync(
                    _storyGenerationService, storyGenerationId, cancellationToken);
                
                if (!isApproved)
                {
                    _logger.LogWarning("Cannot generate prompt: Story generation {StoryGenerationId} is not approved", storyGenerationId);
                    return false;
                }

                // Validate that the specific story index exists
                var storyExists = await PromptPrerequisiteValidator.ValidateStoryExistsAsync(
                    _storyGenerationService, storyGenerationId, storyIndex, cancellationToken);
                
                if (!storyExists)
                {
                    _logger.LogWarning("Cannot generate prompt: Story index {StoryIndex} does not exist in story generation {StoryGenerationId}", 
                        storyIndex, storyGenerationId);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if prompt can be generated for story generation {StoryGenerationId}, index {StoryIndex}", 
                    storyGenerationId, storyIndex);
                return false;
            }
        }
        
        // Overload for backward compatibility
        public Task<bool> CanGeneratePromptAsync(
            Guid storyId,
            CancellationToken cancellationToken = default)
        {
            // This overload is for backward compatibility but doesn't make sense in the new context
            // We'll just return false since we need both story generation ID and index now
            return Task.FromResult(false);
        }
    }
}