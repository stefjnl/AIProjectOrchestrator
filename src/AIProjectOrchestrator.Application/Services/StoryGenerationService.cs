using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Interfaces;
using AIProjectOrchestrator.Domain.Models;
using AIProjectOrchestrator.Domain.Models.AI;
using AIProjectOrchestrator.Domain.Models.Review;
using AIProjectOrchestrator.Domain.Models.Stories;
using AIProjectOrchestrator.Domain.Services;
using Microsoft.Extensions.Logging;

namespace AIProjectOrchestrator.Application.Services
{
    public class StoryGenerationService : IStoryGenerationService
    {
        private readonly IStoryDependencyValidator _dependencyValidator;
        private readonly IStoryContextBuilder _contextBuilder;
        private readonly IStoryAIOrchestrator _aiOrchestrator;
        private readonly IStoryParser _storyParser;
        private readonly IStoryPersistenceHandler _storyPersistence;
        private readonly IProjectPlanningService _projectPlanningService;
        private readonly IStoryGenerationRepository _storyGenerationRepository;
        private readonly IProjectPlanningRepository _projectPlanningRepository;
        private readonly ILogger<StoryGenerationService> _logger;

        public StoryGenerationService(
            IStoryDependencyValidator dependencyValidator,
            IStoryContextBuilder contextBuilder,
            IStoryAIOrchestrator aiOrchestrator,
            IStoryParser storyParser,
            IStoryPersistenceHandler storyPersistence,
            IProjectPlanningService projectPlanningService,
            IStoryGenerationRepository storyGenerationRepository,
            IProjectPlanningRepository projectPlanningRepository,
            ILogger<StoryGenerationService> logger)
        {
            _dependencyValidator = dependencyValidator;
            _contextBuilder = contextBuilder;
            _aiOrchestrator = aiOrchestrator;
            _storyParser = storyParser;
            _storyPersistence = storyPersistence;
            _projectPlanningService = projectPlanningService;
            _storyGenerationRepository = storyGenerationRepository;
            _projectPlanningRepository = projectPlanningRepository;
            _logger = logger;
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
                await _dependencyValidator.ValidateAsync(request.PlanningId, cancellationToken).ConfigureAwait(false);

                var aiRequest = await _contextBuilder.BuildAsync(request.PlanningId, request, generationId, cancellationToken).ConfigureAwait(false);

                var aiResponse = await _aiOrchestrator.GenerateAsync(aiRequest, generationId, cancellationToken).ConfigureAwait(false);

                var stories = await _storyParser.ParseAsync(aiResponse.Content, cancellationToken).ConfigureAwait(false);

                string projectId = "unknown";

                return await _storyPersistence.SaveAsync(stories, aiResponse, request.PlanningId, generationId, projectId, cancellationToken).ConfigureAwait(false);
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
            var storyGeneration = await _storyGenerationRepository.GetByGenerationIdAsync(generationId.ToString(), cancellationToken).ConfigureAwait(false);
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

                var storyGeneration = await _storyGenerationRepository.GetByGenerationIdAsync(generationId.ToString(), cancellationToken).ConfigureAwait(false);
                if (storyGeneration == null)
                {
                    _logger.LogWarning("Service: No StoryGeneration entity found for {GenerationId}", generationId);
                    return null; // Will trigger 404 in controller
                }

                _logger.LogDebug("Service: Found StoryGeneration with DB ID {StoryGenDbId} for {GenerationId}",
                    storyGeneration.Id, generationId);

                var stories = await _storyGenerationRepository.GetStoriesByGenerationIdAsync(generationId, cancellationToken).ConfigureAwait(false);

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
                var planningStatus = await _projectPlanningService.GetPlanningStatusAsync(planningId, cancellationToken).ConfigureAwait(false);
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
            var status = await GetGenerationStatusAsync(storyGenerationId, cancellationToken).ConfigureAwait(false);
            if (status != StoryGenerationStatus.Approved)
            {
                return null;
            }

            // Return the stories if they are approved
            return await GetGenerationResultsAsync(storyGenerationId, cancellationToken).ConfigureAwait(false);
        }

        public async Task<Guid?> GetPlanningIdAsync(Guid storyGenerationId, CancellationToken cancellationToken = default)
        {
            var storyGeneration = await _storyGenerationRepository.GetByGenerationIdAsync(storyGenerationId.ToString(), cancellationToken).ConfigureAwait(false);
            if (storyGeneration != null)
            {
                var projectPlanning = await _projectPlanningRepository.GetByIdAsync(storyGeneration.ProjectPlanningId, cancellationToken).ConfigureAwait(false);
                if (projectPlanning != null)
                {
                    return Guid.Parse(projectPlanning.PlanningId);
                }
            }

            return null;
        }



        public async Task UpdateGenerationStatusAsync(
            Guid generationId,
            StoryGenerationStatus status,
            CancellationToken cancellationToken = default)
        {
            var storyGeneration = await _storyGenerationRepository.GetByGenerationIdAsync(generationId.ToString(), cancellationToken).ConfigureAwait(false);
            if (storyGeneration != null)
            {
                storyGeneration.Status = status;
                await _storyGenerationRepository.UpdateAsync(storyGeneration, cancellationToken).ConfigureAwait(false);

                _logger.LogInformation("Updated story generation {GenerationId} status to {Status}", generationId, status);
            }
        }

        public async Task<UserStory> GetIndividualStoryAsync(
            Guid storyGenerationId,
            int storyIndex,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var stories = await GetAllStoriesAsync(storyGenerationId, cancellationToken).ConfigureAwait(false);

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
            var generations = await _storyGenerationRepository.GetByProjectIdAsync(projectId, cancellationToken).ConfigureAwait(false);
            // Return the latest/most recent generation (assuming one active per project for workflow state)
            return generations.OrderByDescending(g => g.CreatedDate).FirstOrDefault();
        }

        public async Task<List<UserStory>> GetAllStoriesAsync(
            Guid storyGenerationId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Always retrieve stories from the database to ensure we have the latest data
            return await _storyGenerationRepository.GetStoriesByGenerationIdAsync(storyGenerationId, cancellationToken).ConfigureAwait(false);
        }

        public async Task<int> GetStoryCountAsync(
            Guid storyGenerationId,
            CancellationToken cancellationToken = default)
        {
            var stories = await GetAllStoriesAsync(storyGenerationId, cancellationToken).ConfigureAwait(false);
            return stories.Count;
        }

        public async Task<StoryStatus> GetStoryStatusAsync(
            Guid storyId,
            CancellationToken cancellationToken = default)
        {
            var story = await _storyGenerationRepository.GetStoryByIdAsync(storyId, cancellationToken).ConfigureAwait(false);
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
                var story = await _storyGenerationRepository.GetStoryByIdAsync(storyId, cancellationToken).ConfigureAwait(false);
                if (story == null)
                {
                    _logger.LogWarning("Story with ID {StoryId} not found for status update", storyId);
                    throw new KeyNotFoundException($"Story with ID {storyId} not found");
                }

                story.Status = status;
                await _storyGenerationRepository.UpdateStoryAsync(story, cancellationToken).ConfigureAwait(false);

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

                var existingStory = await _storyGenerationRepository.GetStoryByIdAsync(storyId, cancellationToken).ConfigureAwait(false);
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
                await _storyGenerationRepository.UpdateStoryAsync(existingStory, cancellationToken).ConfigureAwait(false);
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
            var stories = await GetAllStoriesAsync(storyGenerationId, cancellationToken).ConfigureAwait(false);
            return stories.Count(s => s.Status == StoryStatus.Approved);
        }

    }
}
