using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Application.Interfaces;
using AIProjectOrchestrator.Domain.Models.AI;
using AIProjectOrchestrator.Domain.Models.PromptGeneration;
using AIProjectOrchestrator.Domain.Services;
using AIProjectOrchestrator.Infrastructure.AI;
using AIProjectOrchestrator.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AIProjectOrchestrator.Application.Services
{
    public class PromptGenerationService : IPromptGenerationService
    {
        private readonly IStoryGenerationService _storyGenerationService;
        private readonly IProjectPlanningService _projectPlanningService;
        private readonly IInstructionService _instructionService;
        private readonly IAIClientFactory _aiClientFactory;
        private readonly Lazy<IReviewService> _reviewService;
        private readonly ILogger<PromptGenerationService> _logger;
        private readonly ILogger<PromptContextAssembler> _loggerAssembler;
        private readonly Domain.Interfaces.IPromptGenerationRepository _promptGenerationRepository;
        private readonly Domain.Interfaces.IStoryGenerationRepository _storyGenerationRepository;

        public PromptGenerationService(
            IStoryGenerationService storyGenerationService,
            IProjectPlanningService projectPlanningService,
            IInstructionService instructionService,
            IAIClientFactory aiClientFactory,
            Lazy<IReviewService> reviewService,
            ILogger<PromptGenerationService> logger,
            ILogger<PromptContextAssembler> loggerAssembler,
            Domain.Interfaces.IPromptGenerationRepository promptGenerationRepository,
            Domain.Interfaces.IStoryGenerationRepository storyGenerationRepository)
        {
            _storyGenerationService = storyGenerationService;
            _projectPlanningService = projectPlanningService;
            _instructionService = instructionService;
            _aiClientFactory = aiClientFactory;
            _reviewService = reviewService;
            _logger = logger;
            _loggerAssembler = loggerAssembler;
            _promptGenerationRepository = promptGenerationRepository;
            _storyGenerationRepository = storyGenerationRepository;
        }

        public async Task<bool> CanGeneratePromptAsync(Guid storyGenerationId, int storyIndex, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Checking if prompt can be generated for StoryGenerationId: {StoryGenerationId}, StoryIndex: {StoryIndex}",
                    storyGenerationId, storyIndex);

                // For enterprise-level applications, we work directly with individual UserStory entities
                // The frontend sends the individual story ID, so we retrieve it directly from the database
                var storyId = storyGenerationId;

                _logger.LogInformation("Retrieving individual UserStory with ID: {StoryId} for can-generate check", storyId);

                // Get the individual story directly from the repository
                var story = await _storyGenerationRepository.GetStoryByIdAsync(storyId, cancellationToken);

                if (story == null)
                {
                    _logger.LogWarning("UserStory not found with ID: {StoryId}", storyId);
                    return false;
                }

                _logger.LogInformation("Found UserStory: {StoryTitle} (ID: {StoryId}, Status: {StoryStatus})",
                    story.Title, story.Id, story.Status);

                // Check if story is approved
                bool canGenerate = story.Status == StoryStatus.Approved;

                _logger.LogInformation("Can generate prompt: {CanGenerate} for StoryId: {StoryId}, Status: {StoryStatus}",
                    canGenerate, storyId, story.Status);

                return canGenerate;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if prompt can be generated for StoryGenerationId: {StoryGenerationId}, StoryIndex: {StoryIndex}",
                    storyGenerationId, storyIndex);
                return false;
            }
        }

        public async Task<PromptGenerationResponse> GeneratePromptAsync(PromptGenerationRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("=== PROMPT GENERATION SERVICE STARTING ===");
            _logger.LogInformation("Generating prompt for StoryGenerationId: {StoryGenerationId}, StoryIndex: {StoryIndex}",
                request.StoryGenerationId, request.StoryIndex);

            try
            {
                // For enterprise-level applications, we work directly with individual UserStory entities
                // The frontend sends the individual story ID, so we retrieve it directly from the database
                var storyId = request.StoryGenerationId;

                _logger.LogInformation("Retrieving individual UserStory with ID: {StoryId}", storyId);

                // Get the individual story directly from the repository
                var story = await _storyGenerationRepository.GetStoryByIdAsync(storyId, cancellationToken);

                if (story == null)
                {
                    _logger.LogError("UserStory not found with ID: {StoryId}", storyId);
                    throw new InvalidOperationException($"UserStory not found with ID: {storyId}");
                }

                _logger.LogInformation("Found UserStory: {StoryTitle} (ID: {StoryId}, Status: {StoryStatus})",
                    story.Title, story.Id, story.Status);

                if (story.Status != StoryStatus.Approved)
                {
                    _logger.LogError("UserStory {StoryId} is not approved. Current status: {StoryStatus}",
                        storyId, story.Status);
                    throw new InvalidOperationException($"UserStory must be approved before generating a prompt. Current status: {story.Status}");
                }

                _logger.LogInformation("UserStory {StoryId} is approved, proceeding with prompt generation", storyId);

                // Build prompt content from story details
                var promptContent = BuildPromptContent(story, request.TechnicalPreferences, request.PromptStyle);
                _logger.LogInformation("Built prompt content with length: {Length}", promptContent.Length);

                // Generate prompt using AI
                var aiClient = _aiClientFactory.GetClient("NanoGpt");
                if (aiClient == null)
                {
                    throw new InvalidOperationException("AI client not available");
                }

                var aiRequest = new AIRequest
                {
                    Prompt = promptContent,
                    ModelName = "moonshotai/Kimi-K2-Instruct-0905",
                    Temperature = 0.7,
                    MaxTokens = 2000
                };

                var aiResponse = await aiClient.CallAsync(aiRequest, cancellationToken);

                if (!aiResponse.IsSuccess)
                {
                    _logger.LogError("AI prompt generation failed: {ErrorMessage}", aiResponse.ErrorMessage);
                    throw new InvalidOperationException($"AI prompt generation failed: {aiResponse.ErrorMessage}");
                }

                // Create response
                var response = new PromptGenerationResponse
                {
                    PromptId = Guid.NewGuid(),
                    StoryGenerationId = request.StoryGenerationId,
                    StoryIndex = request.StoryIndex,
                    GeneratedPrompt = aiResponse.Content ?? promptContent,
                    ReviewId = Guid.NewGuid(), // Create a review for tracking
                    Status = PromptGenerationStatus.Approved,
                    CreatedAt = DateTime.UtcNow
                };

                _logger.LogInformation("Prompt generated successfully with ID: {PromptId}", response.PromptId);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating prompt for StoryGenerationId: {StoryGenerationId}, StoryIndex: {StoryIndex}",
                    request.StoryGenerationId, request.StoryIndex);
                throw;
            }
        }

        private string BuildPromptContent(UserStory story, Dictionary<string, string> technicalPreferences, string? promptStyle)
        {
            // Build a comprehensive prompt from story details
            var promptBuilder = new System.Text.StringBuilder();

            promptBuilder.AppendLine($"# Development Prompt for: {story.Title}");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("## User Story");
            promptBuilder.AppendLine($"As a user, I want to {story.Description}");
            promptBuilder.AppendLine();

            if (story.AcceptanceCriteria?.Any() == true)
            {
                promptBuilder.AppendLine("## Acceptance Criteria");
                foreach (var criterion in story.AcceptanceCriteria)
                {
                    promptBuilder.AppendLine($"- {criterion}");
                }
                promptBuilder.AppendLine();
            }

            if (!string.IsNullOrEmpty(story.Priority))
            {
                promptBuilder.AppendLine($"## Priority: {story.Priority}");
            }

            if (story.StoryPoints.HasValue)
            {
                promptBuilder.AppendLine($"## Story Points: {story.StoryPoints}");
            }

            if (technicalPreferences?.Any() == true)
            {
                promptBuilder.AppendLine("## Technical Preferences");
                foreach (var pref in technicalPreferences)
                {
                    promptBuilder.AppendLine($"- {pref.Key}: {pref.Value}");
                }
                promptBuilder.AppendLine();
            }

            promptBuilder.AppendLine("## Implementation Requirements");
            promptBuilder.AppendLine("Please implement this user story following best practices and including appropriate error handling, logging, and testing.");

            return promptBuilder.ToString();
        }

        public Task<PromptGenerationStatus> GetPromptStatusAsync(Guid promptId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<AIResponse> GeneratePromptFromPlaygroundAsync(string promptContent, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("=== PROMPT GENERATION SERVICE STARTING ===");
            _logger.LogInformation("Generating prompt from playground...");

            try
            {
                _logger.LogInformation("About to call AIClientFactory.GetClient('NanoGpt')");
                var aiClient = _aiClientFactory.GetClient("NanoGpt");
                _logger.LogInformation("AIClientFactory returned client: {ClientName}", aiClient?.ProviderName ?? "NULL");

                var aiRequest = new AIRequest
                {
                    Prompt = promptContent,
                    ModelName = "moonshotai/Kimi-K2-Instruct-0905", // or any other model
                    Temperature = 0.7,
                    MaxTokens = 2000
                };

                var aiResponse = await aiClient.CallAsync(aiRequest, cancellationToken);

                if (!aiResponse.IsSuccess)
                {
                    _logger.LogError("AI call from playground failed: {ErrorMessage}", aiResponse.ErrorMessage);
                    throw new InvalidOperationException($"AI call from playground failed: {aiResponse.ErrorMessage}");
                }

                _logger.LogInformation("Prompt from playground generated successfully.");
                return aiResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating prompt from playground");
                throw;
            }
        }

        public Task<PromptGenerationResponse> GetPromptAsync(Guid promptId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<PromptGenerationResponse>> GetPromptsByProjectAsync(int projectId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
