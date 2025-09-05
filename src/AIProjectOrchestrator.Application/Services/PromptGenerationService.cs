using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AIProjectOrchestrator.Domain.Services;
using AIProjectOrchestrator.Domain.Models.PromptGeneration;

namespace AIProjectOrchestrator.Application.Services
{
    public class PromptGenerationService : IPromptGenerationService
    {
        private readonly ILogger<PromptGenerationService> _logger;
        private readonly ConcurrentDictionary<Guid, PromptGenerationStatus> _promptStatuses;
        private readonly ConcurrentDictionary<Guid, PromptGenerationResponse> _promptResults;

        public PromptGenerationService(
            ILogger<PromptGenerationService> logger)
        {
            _logger = logger;
            _promptStatuses = new ConcurrentDictionary<Guid, PromptGenerationStatus>();
            _promptResults = new ConcurrentDictionary<Guid, PromptGenerationResponse>();
        }

        public async Task<PromptGenerationResponse> GeneratePromptAsync(
            PromptGenerationRequest request,
            CancellationToken cancellationToken = default)
        {
            var promptId = Guid.NewGuid();
            _logger.LogInformation("Starting prompt generation {PromptId} for story: {StoryId}",
                promptId, request.StoryId);

            try
            {
                // Set status to processing
                _promptStatuses[promptId] = PromptGenerationStatus.Processing;

                // For now, return a placeholder response
                // In future phases, we'll implement the actual prompt generation logic
                var response = new PromptGenerationResponse
                {
                    PromptId = promptId,
                    GeneratedPrompt = $"Generated prompt for story {request.StoryId}",
                    ReviewId = Guid.NewGuid(),
                    Status = PromptGenerationStatus.PendingReview,
                    CreatedAt = DateTime.UtcNow
                };

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
            if (_promptStatuses.TryGetValue(promptId, out var status))
            {
                return Task.FromResult(status);
            }

            // If we don't have the status in memory, it might have been cleaned up
            // In a production system, we would check a persistent store
            return Task.FromResult(PromptGenerationStatus.Failed);
        }

        public Task<bool> CanGeneratePromptAsync(
            Guid storyId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // For now, always allow prompt generation
                // In a production system, this might check:
                // - If the story exists and is approved
                // - If prompt generation hasn't already been completed
                // - If there are any business rules preventing generation
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if prompt can be generated for story {StoryId}", storyId);
                return Task.FromResult(false);
            }
        }
    }
}