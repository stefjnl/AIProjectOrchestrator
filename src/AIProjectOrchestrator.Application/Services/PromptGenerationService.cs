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
                _logger.LogInformation("Built coding assistant prompt with length: {Length} characters", promptContent.Length);

                // Generate prompt using AI with fallback logic
                IAIClient? aiClient = null;
                AIResponse? aiResponse = null;

                // Try primary provider first
                try
                {
                    aiClient = _aiClientFactory.GetClient("NanoGpt");
                    if (aiClient != null)
                    {
                        _logger.LogInformation("Using primary AI client: {ProviderName}", aiClient.ProviderName);

                        var aiRequest = new AIRequest
                        {
                            Prompt = promptContent,
                            ModelName = "moonshotai/Kimi-K2-Instruct-0905",
                            Temperature = 0.7,
                            MaxTokens = 1500 // Reduced from 2000 to help with timeout issues
                        };

                        _logger.LogInformation("Calling AI service with prompt length: {Length} characters", promptContent.Length);
                        aiResponse = await aiClient.CallAsync(aiRequest, cancellationToken);

                        if (aiResponse.IsSuccess)
                        {
                            _logger.LogInformation("AI prompt generation successful with provider: {ProviderName}", aiClient.ProviderName);
                        }
                        else
                        {
                            _logger.LogWarning("Primary AI provider failed: {ErrorMessage}", aiResponse.ErrorMessage);
                        }
                    }
                }
                catch (TaskCanceledException tex) when (tex.InnerException is TimeoutException)
                {
                    _logger.LogWarning("Primary AI provider timed out, will attempt fallback: {ErrorMessage}", tex.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Primary AI provider failed, will attempt fallback");
                }

                // If primary failed or timed out, try fallback providers
                if (aiResponse == null || !aiResponse.IsSuccess)
                {
                    _logger.LogInformation("Attempting fallback AI providers");

                    var fallbackProviders = new[] { "OpenRouter", "LMStudio" };
                    foreach (var provider in fallbackProviders)
                    {
                        try
                        {
                            aiClient = _aiClientFactory.GetClient(provider);
                            if (aiClient != null)
                            {
                                _logger.LogInformation("Trying fallback AI client: {ProviderName}", aiClient.ProviderName);

                                var fallbackRequest = new AIRequest
                                {
                                    Prompt = promptContent,
                                    ModelName = aiClient.ProviderName == "OpenRouter" ? "qwen/qwen3-coder" : "qwen-coder",
                                    Temperature = 0.7,
                                    MaxTokens = 1500
                                };

                                aiResponse = await aiClient.CallAsync(fallbackRequest, cancellationToken);

                                if (aiResponse.IsSuccess)
                                {
                                    _logger.LogInformation("Fallback AI provider successful: {ProviderName}", aiClient.ProviderName);
                                    break;
                                }
                                else
                                {
                                    _logger.LogWarning("Fallback provider {ProviderName} failed: {ErrorMessage}",
                                        aiClient.ProviderName, aiResponse.ErrorMessage);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Fallback provider {ProviderName} failed", provider);
                        }
                    }
                }

                // If all providers failed, use the original prompt content as fallback
                if (aiResponse == null || !aiResponse.IsSuccess)
                {
                    _logger.LogWarning("All AI providers failed, using original prompt as fallback");
                    aiResponse = new AIResponse
                    {
                        Content = promptContent,
                        TokensUsed = 0,
                        ProviderName = "Fallback",
                        IsSuccess = true,
                        ErrorMessage = "AI generation failed, using original prompt"
                    };
                }

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

                // Update the UserStory with prompt information
                try
                {
                    _logger.LogInformation("Updating UserStory {StoryId} with prompt information", story.Id);
                    story.HasPrompt = true;
                    story.PromptId = response.PromptId.ToString();

                    // Update the story in the repository
                    await _storyGenerationRepository.UpdateStoryAsync(story, cancellationToken);
                    _logger.LogInformation("Successfully updated UserStory {StoryId} with prompt ID: {PromptId}", story.Id, response.PromptId);
                }
                catch (Exception updateEx)
                {
                    _logger.LogWarning(updateEx, "Failed to update UserStory {StoryId} with prompt information, but prompt generation was successful", story.Id);
                    // Don't fail the entire operation if the update fails
                }

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
            // Build a prompt that will generate a coding prompt for an AI assistant
            var promptBuilder = new StringBuilder();

            promptBuilder.AppendLine($"# Prompt Generation for AI Coding Assistant");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine($"## User Story: {story.Title}");
            promptBuilder.AppendLine($"As a user, I want to {story.Description}");
            promptBuilder.AppendLine();

            if (story.AcceptanceCriteria?.Any() == true)
            {
                promptBuilder.AppendLine("## Acceptance Criteria");
                foreach (var criterion in story.AcceptanceCriteria.Take(5))
                {
                    if (!string.IsNullOrWhiteSpace(criterion))
                    {
                        promptBuilder.AppendLine($"- {criterion}");
                    }
                }
                if (story.AcceptanceCriteria.Count > 5)
                {
                    promptBuilder.AppendLine($"- ... and {story.AcceptanceCriteria.Count - 5} additional criteria");
                }
                promptBuilder.AppendLine();
            }

            promptBuilder.AppendLine("## Request");
            promptBuilder.AppendLine("Can you create a clear, complete and comprehensive prompt for the AI coding assistant to implement the following User Story?");
            promptBuilder.AppendLine();

            promptBuilder.AppendLine("## Requirements for the Generated Prompt");
            promptBuilder.AppendLine("The generated prompt should:");
            promptBuilder.AppendLine("- Be clear and actionable for an AI coding assistant");
            promptBuilder.AppendLine("- Include all necessary technical details from the user story");
            promptBuilder.AppendLine("- Specify the acceptance criteria as implementation requirements");
            promptBuilder.AppendLine("- Provide context about the expected functionality");
            promptBuilder.AppendLine("- Guide the AI assistant to create production-ready code");
            promptBuilder.AppendLine("- Include testing considerations and error handling requirements");
            promptBuilder.AppendLine();

            if (!string.IsNullOrEmpty(story.Priority) && story.Priority.ToLower() != "medium")
            {
                promptBuilder.AppendLine($"**Priority**: {story.Priority}");
                promptBuilder.AppendLine();
            }

            if (story.StoryPoints.HasValue && story.StoryPoints.Value > 0)
            {
                promptBuilder.AppendLine($"**Complexity**: {story.StoryPoints} story points");
                promptBuilder.AppendLine();
            }

            if (technicalPreferences?.Any() == true)
            {
                promptBuilder.AppendLine("## Technical Preferences");
                foreach (var pref in technicalPreferences.Take(3))
                {
                    promptBuilder.AppendLine($"- {pref.Key}: {pref.Value}");
                }
                promptBuilder.AppendLine();
            }

            promptBuilder.AppendLine("## Expected Output");
            promptBuilder.AppendLine("Generate a comprehensive prompt that an AI coding assistant can use to implement this user story with:");
            promptBuilder.AppendLine("- Complete implementation instructions");
            promptBuilder.AppendLine("- All acceptance criteria addressed");
            promptBuilder.AppendLine("- Proper error handling and logging");
            promptBuilder.AppendLine("- Unit tests where applicable");
            promptBuilder.AppendLine("- Production-ready code structure");

            var result = promptBuilder.ToString();
            _logger.LogInformation("Built coding assistant prompt with length: {Length} characters", result.Length);
            return result;
        }

        public async Task<PromptGenerationStatus> GetPromptStatusAsync(Guid promptId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Getting prompt status for ID: {PromptId}", promptId);

                // For now, we'll assume prompts are always approved once generated
                // In a real implementation, this would check the database
                return PromptGenerationStatus.Approved;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting prompt status for ID: {PromptId}", promptId);
                return PromptGenerationStatus.Failed;
            }
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

        public async Task<PromptGenerationResponse?> GetPromptAsync(Guid promptId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Getting prompt for ID: {PromptId}", promptId);

                // For now, return a mock response since we don't have database storage implemented
                // In a real implementation, this would query the database
                _logger.LogWarning("GetPromptAsync not fully implemented - returning mock response for prompt ID: {PromptId}", promptId);

                return new PromptGenerationResponse
                {
                    PromptId = promptId,
                    StoryGenerationId = Guid.NewGuid(), // This would come from database
                    StoryIndex = 0,
                    GeneratedPrompt = "Prompt content would be retrieved from database here",
                    ReviewId = Guid.NewGuid(),
                    Status = PromptGenerationStatus.Approved,
                    CreatedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting prompt for ID: {PromptId}", promptId);
                return null;
            }
        }

        public Task<IEnumerable<PromptGenerationResponse>> GetPromptsByProjectAsync(int projectId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
