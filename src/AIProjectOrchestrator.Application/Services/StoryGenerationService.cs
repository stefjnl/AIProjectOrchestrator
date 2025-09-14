using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Interfaces;
using AIProjectOrchestrator.Domain.Models;
using AIProjectOrchestrator.Domain.Models.AI;
using AIProjectOrchestrator.Domain.Models.Review;
using AIProjectOrchestrator.Domain.Models.Stories;
using AIProjectOrchestrator.Domain.Services;
using AIProjectOrchestrator.Infrastructure.AI.Providers;
using Microsoft.Extensions.Logging;
using System.Text;

namespace AIProjectOrchestrator.Application.Services
{
    public class StoryGenerationService : IStoryGenerationService
    {
        private readonly IRequirementsAnalysisService _requirementsAnalysisService;
        private readonly IProjectPlanningService _projectPlanningService;
        private readonly IInstructionService _instructionService;
        private readonly IStoryAIProvider _storyAIProvider;
        private readonly Lazy<IReviewService> _reviewService;
        private readonly ILogger<StoryGenerationService> _logger;
        private readonly IStoryGenerationRepository _storyGenerationRepository;
        private readonly IProjectPlanningRepository _projectPlanningRepository;

        public StoryGenerationService(
            IRequirementsAnalysisService requirementsAnalysisService,
            IProjectPlanningService projectPlanningService,
            IInstructionService instructionService,
            IStoryAIProvider storyAIProvider,
            Lazy<IReviewService> reviewService,
            ILogger<StoryGenerationService> logger,
            IStoryGenerationRepository storyGenerationRepository,
            IProjectPlanningRepository projectPlanningRepository)
        {
            _requirementsAnalysisService = requirementsAnalysisService;
            _projectPlanningService = projectPlanningService;
            _instructionService = instructionService;
            _storyAIProvider = storyAIProvider;
            _reviewService = reviewService;
            _logger = logger;
            _storyGenerationRepository = storyGenerationRepository;
            _projectPlanningRepository = projectPlanningRepository;
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

                // Validate dependencies (both requirements AND planning approved)
                await ValidateAllDependenciesAsync(request.PlanningId, cancellationToken);

                // Retrieve context from both services
                var planningContent = await _projectPlanningService.GetPlanningResultContentAsync(
                    request.PlanningId, cancellationToken);

                if (string.IsNullOrEmpty(planningContent))
                {
                    _logger.LogError("Story generation {GenerationId} failed: Planning content not found", generationId);
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
                    throw new InvalidOperationException($"Failed to load valid instructions: {instructionContent.ValidationMessage}");
                }

                // Create AI request with combined context
                var aiRequest = new AIRequest
                {
                    SystemMessage = instructionContent.Content,
                    Prompt = CreatePromptFromContext(planningContent, requirementsContent, request),
                    ModelName = string.Empty, // Will be set by the provider
                    Temperature = 0.7, // Default value, will be overridden by provider
                    MaxTokens = 1000  // Default value, will be overridden by provider
                };

                // Log context size metrics
                var contextSize = Encoding.UTF8.GetByteCount(aiRequest.SystemMessage + aiRequest.Prompt);
                _logger.LogInformation("Story generation {GenerationId} context size: {ContextSize} bytes", generationId, contextSize);

                // Warn if context size is approaching limits
                if (contextSize > 100000) // Roughly 25K tokens
                {
                    _logger.LogWarning("Story generation {GenerationId} context size is large: {ContextSize} bytes", generationId, contextSize);
                }

                _logger.LogDebug("Calling AI provider for story generation {GenerationId}", generationId);

                // Call AI using the story-specific provider
                var generatedContent = await _storyAIProvider.GenerateContentAsync(aiRequest.Prompt, aiRequest.SystemMessage);
                
                // GenerateContentAsync returns the content directly, so we need to create an AIResponse
                var aiResponse = new AIResponse
                {
                    Content = generatedContent,
                    TokensUsed = 0, // We don't have token info from GenerateContentAsync
                    ProviderName = _storyAIProvider.ProviderName,
                    IsSuccess = true,
                    ErrorMessage = null
                };

                if (!aiResponse.IsSuccess)
                {
                    _logger.LogError("Story generation {GenerationId} failed: AI call failed - {ErrorMessage}",
                        generationId, aiResponse.ErrorMessage);
                    throw new InvalidOperationException($"AI call failed: {aiResponse.ErrorMessage}");
                }

                // Parse AI response to story collection
                var stories = await ParseAIResponseToStories(aiResponse.Content, cancellationToken);
                
                // Debug: Log the parsed stories
                _logger.LogInformation("Parsed {StoryCount} stories from AI response", stories.Count);
                foreach (var story in stories)
                {
                    _logger.LogDebug("Parsed story: Title='{Title}', Description='{Description}', Priority='{Priority}'",
                        story.Title, story.Description, story.Priority);
                }

                // Create and store the story generation entity first to get the entity ID
                // Get the ProjectPlanning entity ID
                var projectPlanning = await _projectPlanningRepository.GetByPlanningIdAsync(request.PlanningId.ToString(), cancellationToken);
                if (projectPlanning == null)
                {
                    _logger.LogError("Story generation {GenerationId} failed: Project planning not found", generationId);
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
                // Get project ID from project planning if available
                string projectId = "unknown";
                var planningResult = await _projectPlanningService.GetPlanningResultContentAsync(
                    request.PlanningId, cancellationToken);
                if (planningResult != null)
                {
                    // Try to get project ID from planning metadata
                    // For now, we'll keep it as "unknown" since we don't have a direct way to get it
                    // In a production system, we would store the project ID in the planning result
                }

                var reviewRequest = new SubmitReviewRequest
                {
                    ServiceName = "StoryGeneration",
                    Content = aiResponse.Content, // We can still send the full content for review purposes
                    CorrelationId = correlationId,
                    PipelineStage = "Stories",
                    OriginalRequest = aiRequest,
                    AIResponse = aiResponse,
                    Metadata = new System.Collections.Generic.Dictionary<string, object>
                    {
                        { "GenerationId", generationId },
                        { "EntityId", savedStoryGenerationId }, // Pass the entity int ID for FK linking
                        { "PlanningId", request.PlanningId },
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
                    PlanningId = request.PlanningId,
                    Stories = stories,
                    ReviewId = reviewResponse.ReviewId,
                    Status = StoryGenerationStatus.PendingReview,
                    CreatedAt = DateTime.UtcNow
                };

                _logger.LogInformation("Story generation {GenerationId} completed successfully. Review ID: {ReviewId}",
                    generationId, reviewResponse.ReviewId);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Story generation {GenerationId} failed with exception", generationId);
                throw;
            }
        }

        public async Task<StoryGenerationStatus> GetGenerationStatusAsync(
            Guid generationId,
            CancellationToken cancellationToken = default)
        {
            var storyGeneration = await _storyGenerationRepository.GetByGenerationIdAsync(generationId.ToString(), cancellationToken);
            if (storyGeneration != null)
            {
                return storyGeneration.Status;
            }

            return StoryGenerationStatus.Failed;
        }

        public async Task<List<UserStory>?> GetGenerationResultsAsync(
            Guid generationId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Service: Getting generation results for {GenerationId}", generationId);

                var storyGeneration = await _storyGenerationRepository.GetByGenerationIdAsync(generationId.ToString(), cancellationToken);
                if (storyGeneration == null)
                {
                    _logger.LogWarning("Service: No StoryGeneration entity found for {GenerationId}", generationId);
                    return null; // Will trigger 404 in controller
                }

                _logger.LogDebug("Service: Found StoryGeneration with DB ID {StoryGenDbId} for {GenerationId}",
                    storyGeneration.Id, generationId);

                var stories = await _storyGenerationRepository.GetStoriesByGenerationIdAsync(generationId, cancellationToken);

                if (stories == null)
                {
                    _logger.LogWarning("Service: Repository returned null stories for {GenerationId}", generationId);
                    return new List<UserStory>();
                }

                _logger.LogInformation("Service: Successfully retrieved {StoryCount} stories for generation {GenerationId}",
                    stories.Count, generationId);

                return stories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Service: Failed to get generation results for {GenerationId}. Exception: {ExceptionType} - {Message}",
                    generationId, ex.GetType().Name, ex.Message);
                throw; // Let controller handle HTTP response with detailed logging
            }
        }

        public async Task<bool> CanGenerateStoriesAsync(
            Guid planningId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Check that project planning exists and is approved
                var planningStatus = await _projectPlanningService.GetPlanningStatusAsync(planningId, cancellationToken);
                return planningStatus == ProjectPlanningStatus.Approved;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if stories can be generated for planning {PlanningId}", planningId);
                return false;
            }
        }

        public async Task<List<UserStory>?> GetApprovedStoriesAsync(Guid storyGenerationId, CancellationToken cancellationToken = default)
        {
            // Check if the story generation is approved
            var status = await GetGenerationStatusAsync(storyGenerationId, cancellationToken);
            if (status != StoryGenerationStatus.Approved)
            {
                return null;
            }

            // Return the stories if they are approved
            return await GetGenerationResultsAsync(storyGenerationId, cancellationToken);
        }

        public async Task<Guid?> GetPlanningIdAsync(Guid storyGenerationId, CancellationToken cancellationToken = default)
        {
            var storyGeneration = await _storyGenerationRepository.GetByGenerationIdAsync(storyGenerationId.ToString(), cancellationToken);
            if (storyGeneration != null)
            {
                var projectPlanning = await _projectPlanningRepository.GetByIdAsync(storyGeneration.ProjectPlanningId, cancellationToken);
                if (projectPlanning != null)
                {
                    return Guid.Parse(projectPlanning.PlanningId);
                }
            }

            return null;
        }

        public async Task<List<UserStory>> ParseAIResponseToStories(
            string aiResponse,
            CancellationToken cancellationToken = default)
        {
            var stories = new List<UserStory>();
            
            // Simple junior-level parsing - no regex, just string operations
            var lines = aiResponse.Split('\n');
            var currentStory = new UserStory();
            var inStory = false;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();

                // Check for story start
                if (line.StartsWith("#### Story"))
                {
                    if (inStory && !string.IsNullOrEmpty(currentStory.Title))
                    {
                        // Save previous story
                        stories.Add(currentStory);
                        currentStory = new UserStory();
                    }
                    inStory = true;
                    continue;
                }

                if (!inStory) continue;

                // Simple field extraction
                if (line.StartsWith("**Title**:"))
                {
                    currentStory.Title = line.Substring("**Title**:".Length).Trim();
                }
                else if (line.StartsWith("**Description**:"))
                {
                    currentStory.Description = line.Substring("**Description**:".Length).Trim();
                }
                else if (line.StartsWith("**Acceptance Criteria**:"))
                {
                    // Collect acceptance criteria until next field
                    var criteria = new List<string>();
                    for (int j = i + 1; j < lines.Length; j++)
                    {
                        var criteriaLine = lines[j].Trim();
                        if (criteriaLine.StartsWith("**")) break; // Next field
                        if (!string.IsNullOrEmpty(criteriaLine))
                        {
                            criteria.Add(criteriaLine.Replace("- ", "").Replace("• ", "").Trim());
                        }
                    }
                    currentStory.AcceptanceCriteria = criteria;
                }
                else if (line.StartsWith("**Priority**:"))
                {
                    currentStory.Priority = line.Substring("**Priority**:".Length).Trim();
                }
                else if (line.StartsWith("**Estimated Complexity**:"))
                {
                    currentStory.EstimatedComplexity = line.Substring("**Estimated Complexity**:".Length).Trim();
                }
            }

            // Don't forget the last story
            if (inStory && !string.IsNullOrEmpty(currentStory.Title))
            {
                stories.Add(currentStory);
            }

            // Set defaults for missing values
            foreach (var story in stories)
            {
                if (string.IsNullOrEmpty(story.Title))
                    story.Title = "Untitled Story";
                if (string.IsNullOrEmpty(story.Description))
                    story.Description = "";
                if (string.IsNullOrEmpty(story.Priority))
                    story.Priority = "Medium";
                if (story.AcceptanceCriteria == null)
                    story.AcceptanceCriteria = new List<string>();
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
                throw new InvalidOperationException("Planning not found");
            }

            // Check that requirements analysis is approved
            // First get the project ID from the requirements analysis
            var requirementsAnalysis = await _requirementsAnalysisService.GetAnalysisResultsAsync(
                requirementsAnalysisId.Value, cancellationToken);
            if (requirementsAnalysis == null)
            {
                _logger.LogWarning("Story generation failed: Requirements analysis {RequirementsAnalysisId} not found",
                    requirementsAnalysisId.Value);
                throw new InvalidOperationException("Requirements analysis not found");
            }
            
            // Check that requirements analysis is approved
            if (requirementsAnalysis.Status != RequirementsAnalysisStatus.Approved)
            {
                _logger.LogWarning("Story generation failed: Requirements analysis {RequirementsAnalysisId} is not approved (status: {Status})",
                    requirementsAnalysisId.Value, requirementsAnalysis.Status);
                throw new InvalidOperationException("Requirements analysis is not approved");
            }

            // Check that project planning is approved
            var canGenerate = await CanGenerateStoriesAsync(planningId, cancellationToken);
            if (!canGenerate)
            {
                _logger.LogWarning("Story generation failed: Planning {PlanningId} is not approved", planningId);
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
            var storyGeneration = await _storyGenerationRepository.GetByGenerationIdAsync(generationId.ToString(), cancellationToken);
            if (storyGeneration != null)
            {
                storyGeneration.Status = status;
                await _storyGenerationRepository.UpdateAsync(storyGeneration, cancellationToken);

                _logger.LogInformation("Updated story generation {GenerationId} status to {Status}", generationId, status);
            }
        }

        public async Task<UserStory> GetIndividualStoryAsync(
            Guid storyGenerationId,
            int storyIndex,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var stories = await GetAllStoriesAsync(storyGenerationId, cancellationToken);

            if (storyIndex >= 0 && storyIndex < stories.Count)
            {
                return stories[storyIndex];
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(storyIndex), $"Story index {storyIndex} is out of range. Total stories: {stories.Count}");
            }
        }

        public async Task<StoryGeneration?> GetGenerationByProjectAsync(int projectId, CancellationToken cancellationToken = default)
        {
            var generations = await _storyGenerationRepository.GetByProjectIdAsync(projectId, cancellationToken);
            // Return the latest/most recent generation (assuming one active per project for workflow state)
            return generations.OrderByDescending(g => g.CreatedDate).FirstOrDefault();
        }

        public async Task<List<UserStory>> GetAllStoriesAsync(
            Guid storyGenerationId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Always retrieve stories from the database to ensure we have the latest data
            return await _storyGenerationRepository.GetStoriesByGenerationIdAsync(storyGenerationId, cancellationToken);
        }

        public async Task<int> GetStoryCountAsync(
            Guid storyGenerationId,
            CancellationToken cancellationToken = default)
        {
            var stories = await GetAllStoriesAsync(storyGenerationId, cancellationToken);
            return stories.Count;
        }

        public async Task<StoryStatus> GetStoryStatusAsync(
            Guid storyId,
            CancellationToken cancellationToken = default)
        {
            var story = await _storyGenerationRepository.GetStoryByIdAsync(storyId, cancellationToken);
            if (story == null)
            {
                _logger.LogWarning("Story with ID {StoryId} not found", storyId);
                throw new KeyNotFoundException($"Story with ID {storyId} not found");
            }
            return story.Status;
        }

        public async Task UpdateStoryStatusAsync(
            Guid storyId,
            StoryStatus status,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var story = await _storyGenerationRepository.GetStoryByIdAsync(storyId, cancellationToken);
                if (story == null)
                {
                    _logger.LogWarning("Story with ID {StoryId} not found for status update", storyId);
                    throw new KeyNotFoundException($"Story with ID {storyId} not found");
                }

                story.Status = status;
                await _storyGenerationRepository.UpdateStoryAsync(story, cancellationToken);

                _logger.LogInformation("Updated story {StoryId} status to {Status}", storyId, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update story status for {StoryId} to {Status}", storyId, status);
                throw;
            }
        }

        public async Task UpdateStoryAsync(
            Guid storyId,
            UserStory updatedStory,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("UpdateStoryAsync called for storyId: {StoryId}", storyId);

            try
            {
                if (updatedStory == null)
                {
                    _logger.LogError("UpdatedStory parameter is null for story {StoryId}", storyId);
                    throw new ArgumentNullException(nameof(updatedStory));
                }

                _logger.LogInformation("UpdatedStory data: Title='{Title}', Description='{Description}', Status={Status}",
                    updatedStory.Title, updatedStory.Description, updatedStory.Status);

                var existingStory = await _storyGenerationRepository.GetStoryByIdAsync(storyId, cancellationToken);
                if (existingStory == null)
                {
                    _logger.LogWarning("Story with ID {StoryId} not found for update", storyId);
                    throw new KeyNotFoundException($"Story with ID {storyId} not found");
                }

                _logger.LogInformation("Found existing story: Id={Id}, Title='{Title}'", existingStory.Id, existingStory.Title);

                // Update properties without validation
                existingStory.Title = updatedStory.Title ?? string.Empty;
                existingStory.Description = updatedStory.Description ?? string.Empty;
                existingStory.AcceptanceCriteria = updatedStory.AcceptanceCriteria ?? new List<string>();
                existingStory.Priority = updatedStory.Priority ?? string.Empty;
                existingStory.StoryPoints = updatedStory.StoryPoints;
                existingStory.Tags = updatedStory.Tags ?? new List<string>();
                existingStory.EstimatedComplexity = updatedStory.EstimatedComplexity;
                existingStory.Status = updatedStory.Status;

                _logger.LogInformation("About to call repository UpdateStoryAsync for story {StoryId}", storyId);
                await _storyGenerationRepository.UpdateStoryAsync(existingStory, cancellationToken);
                _logger.LogInformation("Repository UpdateStoryAsync completed successfully for story {StoryId}", storyId);

                _logger.LogInformation("Updated story {StoryId}", storyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update story {StoryId}", storyId);
                throw;
            }
        }

        public async Task<int> GetApprovedStoryCountAsync(
            Guid storyGenerationId,
            CancellationToken cancellationToken = default)
        {
            var stories = await GetAllStoriesAsync(storyGenerationId, cancellationToken);
            return stories.Count(s => s.Status == StoryStatus.Approved);
        }

    }
}
