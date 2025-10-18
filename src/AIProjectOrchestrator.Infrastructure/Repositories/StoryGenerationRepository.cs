using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Interfaces;
using AIProjectOrchestrator.Domain.Models.Stories;
using AIProjectOrchestrator.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Linq;
using Microsoft.EntityFrameworkCore.Query;

namespace AIProjectOrchestrator.Infrastructure.Repositories
{
    public class StoryGenerationRepository : Repository<StoryGeneration>, IStoryGenerationRepository
    {
        private readonly ILogger<StoryGenerationRepository> _logger;

        public StoryGenerationRepository(AppDbContext context, ILogger<StoryGenerationRepository> logger) : base(context)
        {
            _logger = logger;
        }

        public async Task<StoryGeneration?> GetByGenerationIdAsync(string generationId, CancellationToken cancellationToken = default)
        {
            return await _context.StoryGenerations
                .Include(sg => sg.Stories)
                .AsNoTracking()
                .FirstOrDefaultAsync(sg => sg.GenerationId == generationId, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<StoryGeneration?> GetByProjectPlanningIdAsync(int projectPlanningId, CancellationToken cancellationToken = default)
        {
            return await _context.StoryGenerations
                .AsNoTracking()
                .FirstOrDefaultAsync(sg => sg.ProjectPlanningId == projectPlanningId, cancellationToken)
                .ConfigureAwait(false);
        }

        public new async Task<StoryGeneration> AddAsync(StoryGeneration entity, CancellationToken cancellationToken = default)
        {
            // Handle cascade insert for UserStory entities by explicitly setting their state
            if (entity.Stories != null && entity.Stories.Any())
            {
                foreach (var story in entity.Stories)
                {
                    // Mark each UserStory as Added so Entity Framework will insert them
                    _context.Entry(story).State = EntityState.Added;
                }
            }

            await _dbSet.AddAsync(entity, cancellationToken).ConfigureAwait(false);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return entity;
        }

        public async Task<IEnumerable<StoryGeneration>> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default)
        {
            return await _context.StoryGenerations
                .AsNoTracking()
                .Where(sg => sg.ProjectPlanning.RequirementsAnalysis.ProjectId == projectId)
                .Select(sg => new StoryGeneration { Id = sg.Id, GenerationId = sg.GenerationId, Status = sg.Status, ProjectPlanningId = sg.ProjectPlanningId })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public new async Task<StoryGeneration?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.StoryGenerations
                .AsNoTracking()
                .FirstOrDefaultAsync(sg => sg.Id == id, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<UserStory?> GetStoryByIdAsync(Guid storyId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Querying single story by GUID: {StoryGuid}", storyId);

                var story = await _context.UserStories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(us => us.Id == storyId, cancellationToken)
                    .ConfigureAwait(false);

                if (story == null)
                {
                    _logger.LogWarning("No UserStory found with GUID: {StoryGuid}", storyId);
                }
                else
                {
                    _logger.LogDebug("Found UserStory {StoryGuid} with title: {StoryTitle}", storyId, story.Title);
                }

                return story;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database error while retrieving story {StoryGuid}", storyId);
                throw new InvalidOperationException($"Failed to retrieve story {storyId}", ex);
            }
        }

        public async Task UpdateStoryAsync(UserStory story, CancellationToken cancellationToken = default)
        {
            try
            {
                if (story == null)
                {
                    throw new ArgumentNullException(nameof(story));
                }

                _logger.LogDebug("Updating story {StoryGuid} with title: {StoryTitle}", story.Id, story.Title);

                // Ensure the entity is properly tracked
                var entry = _context.Entry(story);
                if (entry.State == EntityState.Detached)
                {
                    _context.UserStories.Attach(story);
                }
                entry.State = EntityState.Modified;

                await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Successfully updated story {StoryGuid}", story.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database error while updating story {StoryGuid}: {Message}", story?.Id ?? Guid.Empty, ex.Message);
                throw new InvalidOperationException($"Failed to update story {story?.Id}", ex);
            }
        }

        public async Task<List<UserStory>> GetStoriesByGenerationIdAsync(Guid generationId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Querying stories for generation GUID: {GenerationGuid}", generationId);

                var storyGeneration = await GetByGenerationIdAsync(generationId.ToString(), cancellationToken).ConfigureAwait(false);
                if (storyGeneration == null)
                {
                    _logger.LogWarning("No StoryGeneration record found for GUID: {GenerationGuid}", generationId);
                    return new List<UserStory>();
                }

                _logger.LogDebug("Found StoryGeneration record ID {StoryGenDbId} for GUID {GenerationGuid}, querying associated stories",
                    storyGeneration.Id, generationId);

                var stories = await _context.UserStories
                    .AsNoTracking()
                    .Where(us => us.StoryGenerationId == storyGeneration.Id)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                _logger.LogInformation("Retrieved {StoryCount} stories for StoryGeneration {StoryGenDbId} (GUID: {GenerationGuid})",
                    stories.Count, storyGeneration.Id, generationId);

                return stories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database error while retrieving stories for generation GUID: {GenerationGuid}", generationId);
                throw new InvalidOperationException($"Failed to retrieve stories for generation {generationId}", ex);
            }
        }
    }
}
