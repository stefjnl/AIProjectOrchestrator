using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AIProjectOrchestrator.Domain.Interfaces;
using AIProjectOrchestrator.Domain.Services;
using AIProjectOrchestrator.Domain.Models;
using AIProjectOrchestrator.Domain.Models.AI;
using AIProjectOrchestrator.Domain.Models.PromptGeneration;
using AIProjectOrchestrator.Domain.Models.Review;
using AIProjectOrchestrator.Domain.Models.Stories;
using AIProjectOrchestrator.Infrastructure.AI;
using AIProjectOrchestrator.Domain.Entities;

namespace AIProjectOrchestrator.Application.Services
{
    public class PromptGenerationService : IPromptGenerationService
    {
        private readonly IStoryGenerationService _storyGenerationService;
        private readonly IInstructionService _instructionService;
        private readonly ILogger<PromptGenerationService> _logger;
        private readonly IPromptGenerationRepository _promptGenerationRepository;
        private readonly IStoryGenerationRepository _storyGenerationRepository;

        private readonly IProjectPlanningService _projectPlanningService;
        private readonly IAIClientFactory _aiClientFactory;
        private readonly Lazy<IReviewService> _reviewService;
        private readonly ILogger<PromptContextAssembler> _assemblerLogger;
        private readonly PromptContextAssembler _assembler;
        private readonly ContextOptimizer _optimizer;

        public PromptGenerationService(
            IStoryGenerationService storyGenerationService,
            IProjectPlanningService projectPlanningService,
            IInstructionService instructionService,
            IAIClientFactory aiClientFactory,
            Lazy<IReviewService> reviewService,
            ILogger<PromptGenerationService> logger,
            ILogger<PromptContextAssembler> assemblerLogger,
            IPromptGenerationRepository promptGenerationRepository,
            IStoryGenerationRepository storyGenerationRepository)
        {
            _storyGenerationService = storyGenerationService;
            _projectPlanningService = projectPlanningService;
            _instructionService = instructionService;
            _aiClientFactory = aiClientFactory;
            _reviewService = reviewService;
            _logger = logger;
            _assemblerLogger = assemblerLogger;
            _promptGenerationRepository = promptGenerationRepository;
            _storyGenerationRepository = storyGenerationRepository;
            _assembler = new PromptContextAssembler(projectPlanningService, storyGenerationService, _assemblerLogger);
            _optimizer = new ContextOptimizer();
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
                var targetStory = await _storyGenerationService.GetIndividualStoryAsync(
                    request.StoryGenerationId, request.StoryIndex, cancellationToken);

                // Get planning ID from story generation
                var planningId = await _storyGenerationService.GetPlanningIdAsync(request.StoryGenerationId, cancellationToken);
                if (!planningId.HasValue)
                {
                    throw new InvalidOperationException("Planning ID not found for story generation");
                }

                // Check for cancellation again
                cancellationToken.ThrowIfCancellationRequested();

                // Assemble context
                var context = await _assembler.AssembleContextAsync(request.StoryGenerationId, request.StoryIndex, cancellationToken);
                var optimizedContext = _optimizer.OptimizeContext(context);

                // Load instructions
                var instructionContent = await _instructionService.GetInstructionAsync("PromptGenerator", cancellationToken);
                if (!instructionContent.IsValid)
                {
                    throw new InvalidOperationException($"Invalid instruction content: {instructionContent.ValidationMessage}");
                }

                // Build prompt from optimized context
                var promptBuilder = new StringBuilder();
                promptBuilder.AppendLine("# Prompt Generation Request");
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("## Target Story");
                promptBuilder.AppendLine($"**Title:** {optimizedContext.TargetStory.Title}");
                promptBuilder.AppendLine($"**Description:** {optimizedContext.TargetStory.Description}");
                promptBuilder.AppendLine("**Acceptance Criteria:**");
                foreach (var criterion in optimizedContext.TargetStory.AcceptanceCriteria)
                {
                    promptBuilder.AppendLine($"- {criterion}");
                }
                promptBuilder.AppendLine();

                if (!string.IsNullOrEmpty(optimizedContext.ProjectArchitecture))
                {
                    promptBuilder.AppendLine("## Project Architecture");
                    promptBuilder.AppendLine(optimizedContext.ProjectArchitecture);
                    promptBuilder.AppendLine();
                }

                if (optimizedContext.RelatedStories.Any())
                {
                    promptBuilder.AppendLine("## Related Stories");
                    foreach (var story in optimizedContext.RelatedStories)
                    {
                        promptBuilder.AppendLine($"**{story.Title}:** {story.Description}");
                    }
                    promptBuilder.AppendLine();
                }

                if (optimizedContext.TechnicalPreferences.Any())
                {
                    promptBuilder.AppendLine("## Technical Preferences");
                    foreach (var pref in optimizedContext.TechnicalPreferences)
                    {
                        promptBuilder.AppendLine($"{pref.Key}: {pref.Value}");
                    }
                    promptBuilder.AppendLine();
                }

                if (!string.IsNullOrEmpty(optimizedContext.IntegrationGuidance))
                {
                    promptBuilder.AppendLine("## Integration Guidance");
                    promptBuilder.AppendLine(optimizedContext.IntegrationGuidance);
                    promptBuilder.AppendLine();
                }

                promptBuilder.AppendLine("## Instructions");
                promptBuilder.AppendLine("Generate a comprehensive coding prompt for an AI assistant based on the target story and provided context. Include all relevant details for accurate implementation.");

                // Create AI request
                var aiRequest = new AIRequest
                {
                    SystemMessage = instructionContent.Content,
                    Prompt = promptBuilder.ToString(),
                    ModelName = "qwen/qwen3-coder",
                    Temperature = 0.7,
                    MaxTokens = 2000
                };

                // Log context size
                var contextSize = Encoding.UTF8.GetByteCount(aiRequest.SystemMessage + aiRequest.Prompt);
                _logger.LogInformation("Prompt generation {PromptId} context size: {ContextSize} bytes", promptId, contextSize);

                // Get AI client
                var aiClient = _aiClientFactory.GetClient("OpenRouter");
                if (aiClient == null)
                {
                    throw new InvalidOperationException("OpenRouter AI client not available");
                }

                // Call AI
                var aiResponse = await aiClient.CallAsync(aiRequest, cancellationToken);

                if (!aiResponse.IsSuccess)
                {
                    throw new InvalidOperationException($"AI call failed: {aiResponse.ErrorMessage}");
                }

                // Submit for review
                var correlationId = Guid.NewGuid().ToString();
                var reviewRequest = new SubmitReviewRequest
                {
                    ServiceName = "PromptGeneration",
                    Content = aiResponse.Content,
                    CorrelationId = correlationId,
                    PipelineStage = "Prompt",
                    OriginalRequest = aiRequest,
                    AIResponse = aiResponse,
                    Metadata = new Dictionary<string, object>
                    {
                        { "PromptId", promptId },
                        { "StoryGenerationId", request.StoryGenerationId },
                        { "StoryIndex", request.StoryIndex }
                    }
                };

                var reviewResponse = await _reviewService.Value.SubmitForReviewAsync(reviewRequest, cancellationToken);

                // Get the StoryGeneration entity ID
                var storyGeneration = await _storyGenerationRepository.GetByGenerationIdAsync(request.StoryGenerationId.ToString(), cancellationToken);
                if (storyGeneration == null)
                {
                    throw new InvalidOperationException("Story generation not found");
                }

                // Create the database entity
                var promptGenerationEntity = new PromptGeneration
                {
                    PromptId = promptId.ToString(),
                    StoryGenerationId = storyGeneration.Id,
                    StoryIndex = request.StoryIndex,
                    Status = PromptGenerationStatus.PendingReview,
                    Content = aiResponse.Content,
                    ReviewId = reviewResponse.ReviewId.ToString(),
                    CreatedDate = DateTime.UtcNow
                };

                // Save to database
                await _promptGenerationRepository.AddAsync(promptGenerationEntity, cancellationToken);

                // Create response
                var response = new PromptGenerationResponse
                {
                    PromptId = promptId,
                    StoryGenerationId = request.StoryGenerationId,
                    StoryIndex = request.StoryIndex,
                    GeneratedPrompt = aiResponse.Content,
                    ReviewId = reviewResponse.ReviewId,
                    Status = PromptGenerationStatus.PendingReview,
                    CreatedAt = DateTime.UtcNow
                };

                // Check for cancellation before completing
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogInformation("Prompt generation {PromptId} completed successfully. Review ID: {ReviewId}",
                    promptId, reviewResponse.ReviewId);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Prompt generation {PromptId} failed with exception", promptId);
                throw;
            }
        }

        public async Task<PromptGenerationStatus> GetPromptStatusAsync(
            Guid promptId,
            CancellationToken cancellationToken = default)
        {
            // Check for cancellation
            cancellationToken.ThrowIfCancellationRequested();

            var promptGeneration = await _promptGenerationRepository.GetByPromptIdAsync(promptId.ToString(), cancellationToken);
            if (promptGeneration != null)
            {
                return promptGeneration.Status;
            }

            return PromptGenerationStatus.Failed;
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
        
        public async Task<PromptGenerationResponse?> GetPromptAsync(
            Guid promptId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var promptGeneration = await _promptGenerationRepository.GetByPromptIdAsync(promptId.ToString(), cancellationToken);
            if (promptGeneration != null)
            {
                return new PromptGenerationResponse
                {
                    PromptId = Guid.Parse(promptGeneration.PromptId),
                    StoryGenerationId = await GetStoryGenerationGuidAsync(promptGeneration.StoryGenerationId, cancellationToken),
                    StoryIndex = promptGeneration.StoryIndex,
                    GeneratedPrompt = promptGeneration.Content,
                    ReviewId = Guid.Parse(promptGeneration.ReviewId),
                    Status = promptGeneration.Status,
                    CreatedAt = promptGeneration.CreatedDate
                };
            }

            return null;
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
        
        private async Task<Guid> GetStoryGenerationGuidAsync(int storyGenerationId, CancellationToken cancellationToken)
        {
            var storyGeneration = await _storyGenerationRepository.GetByIdAsync(storyGenerationId, cancellationToken);
            if (storyGeneration != null)
            {
                return Guid.Parse(storyGeneration.GenerationId);
            }
            
            throw new InvalidOperationException($"Story generation with ID {storyGenerationId} not found");
        }
    }
}
