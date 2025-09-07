using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Interfaces;
using AIProjectOrchestrator.Domain.Models.Stories;
using AIProjectOrchestrator.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIProjectOrchestrator.Infrastructure.Repositories
{
    public class StoryGenerationRepository : Repository<StoryGeneration>, IStoryGenerationRepository
    {
        public StoryGenerationRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<StoryGeneration?> GetByGenerationIdAsync(string generationId, CancellationToken cancellationToken = default)
        {
            return await _context.StoryGenerations
                .FirstOrDefaultAsync(sg => sg.GenerationId == generationId, cancellationToken);
        }

        public async Task<StoryGeneration?> GetByProjectPlanningIdAsync(int projectPlanningId, CancellationToken cancellationToken = default)
        {
            return await _context.StoryGenerations
                .FirstOrDefaultAsync(sg => sg.ProjectPlanningId == projectPlanningId, cancellationToken);
        }

        public async Task<IEnumerable<StoryGeneration>> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default)
        {
            return await _context.StoryGenerations
                .Where(sg => sg.ProjectPlanning.RequirementsAnalysis.ProjectId == projectId)
                .ToListAsync(cancellationToken);
        }

        public async Task<StoryGeneration?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.StoryGenerations
                .FirstOrDefaultAsync(sg => sg.Id == id, cancellationToken);
        }

        public async Task<UserStory?> GetStoryByIdAsync(Guid storyId, CancellationToken cancellationToken = default)
        {
            return await _context.UserStories
                .FirstOrDefaultAsync(us => us.Id == storyId, cancellationToken);
        }

        public async Task UpdateStoryAsync(UserStory story, CancellationToken cancellationToken = default)
        {
            _context.UserStories.Update(story);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<UserStory>> GetStoriesByGenerationIdAsync(Guid generationId, CancellationToken cancellationToken = default)
        {
            var storyGeneration = await GetByGenerationIdAsync(generationId.ToString(), cancellationToken);
            if (storyGeneration == null)
            {
                return new List<UserStory>();
            }

            return await _context.UserStories
                .Where(us => us.StoryGenerationId == storyGeneration.Id)
                .ToListAsync(cancellationToken);
        }
    }
}
