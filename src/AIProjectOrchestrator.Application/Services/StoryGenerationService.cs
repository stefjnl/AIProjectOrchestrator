using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AIProjectOrchestrator.Domain.Services;
using AIProjectOrchestrator.Domain.Models.Stories;
using AIProjectOrchestrator.Domain.Models.AI;
using AIProjectOrchestrator.Domain.Models.Review;
using AIProjectOrchestrator.Infrastructure.AI;

namespace AIProjectOrchestrator.Application.Services
{
    public class StoryGenerationService : IStoryGenerationService
    {
        private readonly IRequirementsAnalysisService _requirementsAnalysisService;
        private readonly IProjectPlanningService _projectPlanningService;
        private readonly IInstructionService _instructionService;
        private readonly IAIClientFactory _aiClientFactory;
        private readonly Lazy<IReviewService> _reviewService;
        private readonly ILogger<StoryGenerationService> _logger;
        private readonly ConcurrentDictionary<Guid, StoryGenerationStatus> _generationStatuses;
        private readonly ConcurrentDictionary<Guid, List<UserStory>> _generationResults;

        public StoryGenerationService(
            IRequirementsAnalysisService requirementsAnalysisService,
            IProjectPlanningService projectPlanningService,
            IInstructionService instructionService,
            IAIClientFactory aiClientFactory,
            Lazy<IReviewService> reviewService,
            ILogger<StoryGenerationService> logger)
        {
            _requirementsAnalysisService = requirementsAnalysisService;
            _projectPlanningService = projectPlanningService;
            _instructionService = instructionService;
            _aiClientFactory = aiClientFactory;
            _reviewService = reviewService;
            _logger = logger;
            _generationStatuses = new ConcurrentDictionary<Guid, StoryGenerationStatus>();
            _generationResults = new ConcurrentDictionary<Guid, List<UserStory>>();
        }

        public async Task<StoryGenerationResponse> GenerateStoriesAsync(
            StoryGenerationRequest request,
            CancellationToken cancellationToken = default)
        {
            var generationId = Guid.NewGuid();
            _logger.LogInformation("Starting story generation {GenerationId} for planning: {PlanningId}",
                generationId, request.PlanningId);

            try
            {
                // Validate input
                if (request.PlanningId == Guid.Empty)
                {
                    _logger.LogWarning("Story generation {GenerationId} failed: Planning ID is required", generationId);
                    throw new ArgumentException("Planning ID is required");
                }

                // Set status to processing
                _generationStatuses[generationId] = StoryGenerationStatus.Processing;

                // Validate dependencies (both requirements AND planning approved)
                await ValidateAllDependenciesAsync(request.PlanningId, cancellationToken);

                // Retrieve context from both services
                var planningContent = await _projectPlanningService.GetPlanningResultContentAsync(
                    request.PlanningId, cancellationToken);

                if (string.IsNullOrEmpty(planningContent))
                {
                    _logger.LogError("Story generation {GenerationId} failed: Planning content not found", generationId);
                    _generationStatuses[generationId] = StoryGenerationStatus.Failed;
                    throw new InvalidOperationException("Planning content not found");
                }

                // Get requirements analysis ID and content
                var requirementsAnalysisId = await _projectPlanningService.GetRequirementsAnalysisIdAsync(
                    request.PlanningId, cancellationToken);

                var requirementsContent = string.Empty;
                if (requirementsAnalysisId.HasValue)
                {
                    requirementsContent = await _requirementsAnalysisService.GetAnalysisResultContentAsync(
                        requirementsAnalysisId.Value, cancellationToken) ?? string.Empty;
                }

                // Load instructions
                _logger.LogDebug("Loading instructions for story generation {GenerationId}", generationId);
                var instructionContent = await _instructionService.GetInstructionAsync("StoryGenerator", cancellationToken);

                if (!instructionContent.IsValid)
                {
                    _logger.LogError("Story generation {GenerationId} failed: Invalid instruction content - {ValidationMessage}",
                        generationId, instructionContent.ValidationMessage);
                    _generationStatuses[generationId] = StoryGenerationStatus.Failed;
                    throw new InvalidOperationException($"Failed to load valid instructions: {instructionContent.ValidationMessage}");
                }

                // Create AI request with combined context
                var aiRequest = new AIRequest
                {
                    SystemMessage = instructionContent.Content,
                    Prompt = CreatePromptFromContext(planningContent, requirementsContent, request),
                    ModelName = "qwen/qwen3-coder", // Default model for story generation via OpenRouter
                    Temperature = 0.7,
                    MaxTokens = 4000 // Larger response expected for story generation
                };

                // Log context size metrics
                var contextSize = Encoding.UTF8.GetByteCount(aiRequest.SystemMessage + aiRequest.Prompt);
                _logger.LogInformation("Story generation {GenerationId} context size: {ContextSize} bytes", generationId, contextSize);

                // Warn if context size is approaching limits
                if (contextSize > 100000) // Roughly 25K tokens
                {
                    _logger.LogWarning("Story generation {GenerationId} context size is large: {ContextSize} bytes", generationId, contextSize);
                }

                // Get OpenRouter AI client
                var aiClient = _aiClientFactory.GetClient("OpenRouter");
                if (aiClient == null)
                {
                    _logger.LogError("Story generation {GenerationId} failed: OpenRouter AI client not available", generationId);
                    _generationStatuses[generationId] = StoryGenerationStatus.Failed;
                    throw new InvalidOperationException("OpenRouter AI client is not available");
                }

                _logger.LogDebug("Calling AI client for story generation {GenerationId}", generationId);

                // Call AI
                var aiResponse = await aiClient.CallAsync(aiRequest, cancellationToken);

                if (!aiResponse.IsSuccess)
                {
                    _logger.LogError("Story generation {GenerationId} failed: AI call failed - {ErrorMessage}",
                        generationId, aiResponse.ErrorMessage);
                    _generationStatuses[generationId] = StoryGenerationStatus.Failed;
                    throw new InvalidOperationException($"AI call failed: {aiResponse.ErrorMessage}");
                }

                // Parse AI response to story collection
                var stories = await ParseAIResponseToStories(aiResponse.Content, cancellationToken);

                // Submit for review
                _logger.LogDebug("Submitting AI response for review in story generation {GenerationId}", generationId);
                var correlationId = Guid.NewGuid().ToString();
                var reviewRequest = new SubmitReviewRequest
                {
                    ServiceName = "StoryGeneration",
                    Content = aiResponse.Content,
                    CorrelationId = correlationId,
                    PipelineStage = "Stories",
                    OriginalRequest = aiRequest,
                    AIResponse = aiResponse,
                    Metadata = new System.Collections.Generic.Dictionary<string, object>
                    {
                        { "GenerationId", generationId },
                        { "PlanningId", request.PlanningId },
                        { "ProjectId", "unknown" } // Will be updated when we have project tracking
                    }
                };

                var reviewResponse = await _reviewService.Value.SubmitForReviewAsync(reviewRequest, cancellationToken);

                // Set status to pending review
                _generationStatuses[generationId] = StoryGenerationStatus.PendingReview;

                // Store results for later retrieval
                _generationResults[generationId] = stories;

                _logger.LogInformation("Story generation {GenerationId} completed successfully. Review ID: {ReviewId}",
                    generationId, reviewResponse.ReviewId);

                return new StoryGenerationResponse
                {
                    GenerationId = generationId,
                    PlanningId = request.PlanningId,
                    Stories = stories,
                    ReviewId = reviewResponse.ReviewId,
                    Status = StoryGenerationStatus.PendingReview,
                    CreatedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Story generation {GenerationId} failed with exception", generationId);
                _generationStatuses[generationId] = StoryGenerationStatus.Failed;
                throw;
            }
        }

        public async Task<StoryGenerationStatus> GetGenerationStatusAsync(
            Guid generationId,
            CancellationToken cancellationToken = default)
        {
            if (_generationStatuses.TryGetValue(generationId, out var status))
            {
                return status;
            }

            // If we don't have the status in memory, it might have been cleaned up
            // In a production system, we would check a persistent store
            return StoryGenerationStatus.Failed;
        }

        public async Task<List<UserStory>?> GetGenerationResultsAsync(
            Guid generationId,
            CancellationToken cancellationToken = default)
        {
            if (_generationResults.TryGetValue(generationId, out var result))
            {
                return result;
            }

            // If we don't have the result in memory, it might have been cleaned up
            // In a production system, we would check a persistent store
            return null;
        }

        public async Task<bool> CanGenerateStoriesAsync(
            Guid planningId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Check that project planning exists and is approved
                var canCreatePlan = await _projectPlanningService.CanCreatePlanAsync(planningId, cancellationToken);
                return canCreatePlan;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if stories can be generated for planning {PlanningId}", planningId);
                return false;
            }
        }

        public async Task<List<UserStory>?> GetApprovedStoriesAsync(Guid storyGenerationId, CancellationToken cancellationToken = default)
        {
            // For now, we'll just return the stories if they exist
            // In a production system, we would check if they are approved
            return await GetGenerationResultsAsync(storyGenerationId, cancellationToken);
        }

        public async Task<Guid?> GetPlanningIdAsync(Guid storyGenerationId, CancellationToken cancellationToken = default)
        {
            // In a production system, we would retrieve this from persistent storage
            // For now, we'll return null as we don't have a way to retrieve it
            // This would need to be implemented with proper data storage
            return null;
        }

        public async Task<List<UserStory>> ParseAIResponseToStories(
            string aiResponse,
            CancellationToken cancellationToken = default)
        {
            var stories = new List<UserStory>();

            // Parse structured markdown response
            // Look for story sections
            var storyPattern = @"###\s*Story\s*\d+.*?(?=(###\s*Story|$))\";
            var storyMatches = Regex.Matches(aiResponse, storyPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            foreach (Match match in storyMatches)
            {
                var storyContent = match.Value;

                // Extract title
                var titleMatch = Regex.Match(storyContent, @"\*\*Title:\*\*\s*(.+?)(?=\n|\*\*|$)", RegexOptions.Singleline);
                var title = titleMatch.Success ? titleMatch.Groups[1].Value.Trim() : "Untitled Story";

                // Extract description
                var descriptionMatch = Regex.Match(storyContent, @"\*\*Description:\*\*\s*(.+?)(?=\n|\*\*|$)", RegexOptions.Singleline);
                var description = descriptionMatch.Success ? descriptionMatch.Groups[1].Value.Trim() : "";

                // Extract acceptance criteria
                var criteriaMatch = Regex.Match(storyContent, @"\*\*Acceptance Criteria:\*\*\s*(.+?)(?=\n\*\*|$)", RegexOptions.Singleline);
                var criteriaText = criteriaMatch.Success ? criteriaMatch.Groups[1].Value.Trim() : "";
                var acceptanceCriteria = new List<string>();

                if (!string.IsNullOrEmpty(criteriaText))
                {
                    // Split by bullet points or line breaks
                    var criteriaLines = criteriaText.Split(new[] { '\n', '-' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(line => line.Trim())
                        .Where(line => !string.IsNullOrEmpty(line));

                    acceptanceCriteria.AddRange(criteriaLines);
                }

                // Extract priority
                var priorityMatch = Regex.Match(storyContent, @"\*\*Priority:\*\*\s*(.+?)(?=\n|\*\*|$)", RegexOptions.Singleline);
                var priority = priorityMatch.Success ? priorityMatch.Groups[1].Value.Trim() : "Medium";

                // Extract estimated complexity
                var complexityMatch = Regex.Match(storyContent, @"\*\*Estimated Complexity:\*\*\s*(.+?)(?=\n|\*\*|$)", RegexOptions.Singleline);
                var estimatedComplexity = complexityMatch.Success ? complexityMatch.Groups[1].Value.Trim() : null;

                stories.Add(new UserStory
                {
                    Title = title,
                    Description = description,
                    AcceptanceCriteria = acceptanceCriteria,
                    Priority = priority,
                    EstimatedComplexity = estimatedComplexity
                });
            }

            // If no stories were parsed, try a simpler approach
            if (stories.Count == 0)
            {
                // Try to parse as a simple list of stories
                var simpleStoryPattern = @"^\s*\*\s*(.+?)(?:\s*-\s*(.+?))?$";
                var simpleMatches = Regex.Matches(aiResponse, simpleStoryPattern, RegexOptions.Multiline);

                foreach (Match match in simpleMatches)
                {
                    var title = match.Groups[1].Value.Trim();
                    var description = match.Groups.Count > 2 && !string.IsNullOrEmpty(match.Groups[2].Value)
                        ? match.Groups[2].Value.Trim()
                        : "";

                    stories.Add(new UserStory
                    {
                        Title = title,
                        Description = description,
                        AcceptanceCriteria = new List<string>(),
                        Priority = "Medium",
                        EstimatedComplexity = null
                    });
                }
            }

            return stories;
        }

        private async Task ValidateAllDependenciesAsync(
            Guid planningId,
            CancellationToken cancellationToken)
        {
            // Get the requirements analysis ID from the planning ID
            var requirementsAnalysisId = await _projectPlanningService.GetRequirementsAnalysisIdAsync(
                planningId, cancellationToken);

            if (requirementsAnalysisId == null)
            {
                _logger.LogWarning("Story generation failed: Planning {PlanningId} not found", planningId);
                _generationStatuses[Guid.Empty] = StoryGenerationStatus.PlanningNotApproved;
                throw new InvalidOperationException("Planning not found");
            }

            // Check that requirements analysis is approved
            var canAnalyzeRequirements = await _requirementsAnalysisService.CanAnalyzeRequirementsAsync(
                requirementsAnalysisId.Value, cancellationToken);

            if (!canAnalyzeRequirements)
            {
                _logger.LogWarning("Story generation failed: Requirements analysis {RequirementsAnalysisId} is not approved",
                    requirementsAnalysisId.Value);
                _generationStatuses[Guid.Empty] = StoryGenerationStatus.RequirementsNotApproved;
                throw new InvalidOperationException("Requirements analysis is not approved");
            }

            // Check that project planning is approved
            var canGenerate = await CanGenerateStoriesAsync(planningId, cancellationToken);
            if (!canGenerate)
            {
                _logger.LogWarning("Story generation failed: Planning {PlanningId} is not approved", planningId);
                _generationStatuses[Guid.Empty] = StoryGenerationStatus.PlanningNotApproved;
                throw new InvalidOperationException("Planning is not approved");
            }
        }

        private string CreatePromptFromContext(
            string planningContent,
            string requirementsContent,
            StoryGenerationRequest request)
        {
            var prompt = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(requirementsContent))
            {
                prompt.AppendLine("# Requirements Analysis Content");
                prompt.AppendLine(requirementsContent);
                prompt.AppendLine();
            }

            prompt.AppendLine("# Project Planning Content");
            prompt.AppendLine(planningContent);
            prompt.AppendLine();

            if (!string.IsNullOrWhiteSpace(request.StoryPreferences))
            {
                prompt.AppendLine("## Story Preferences");
                prompt.AppendLine(request.StoryPreferences);
                prompt.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(request.ComplexityLevels))
            {
                prompt.AppendLine("## Complexity Levels");
                prompt.AppendLine(request.ComplexityLevels);
                prompt.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(request.AdditionalGuidance))
            {
                prompt.AppendLine("## Additional Guidance");
                prompt.AppendLine(request.AdditionalGuidance);
                prompt.AppendLine();
            }

            prompt.AppendLine("## Instructions");
            prompt.AppendLine("Please generate 5-15 user stories based on the requirements analysis and project planning content above.");
            prompt.AppendLine("Each story should include:");
            prompt.AppendLine("- A clear title");
            prompt.AppendLine("- A detailed description following the 'As a [role], I want [goal] so that [benefit]' format");
            prompt.AppendLine("- Specific acceptance criteria as bullet points");
            prompt.AppendLine("- A priority level (High, Medium, Low)");
            prompt.AppendLine("- An estimated complexity level if applicable");

            return prompt.ToString();
        }
        
        public async Task UpdateGenerationStatusAsync(
            Guid generationId,
            StoryGenerationStatus status,
            CancellationToken cancellationToken = default)
        {
            // Update the in-memory status
            _generationStatuses[generationId] = status;
            
            _logger.LogInformation("Updated story generation {GenerationId} status to {Status}", generationId, status);
        }
    }
}