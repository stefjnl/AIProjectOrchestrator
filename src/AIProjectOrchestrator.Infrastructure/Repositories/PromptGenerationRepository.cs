using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Interfaces;
using AIProjectOrchestrator.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIProjectOrchestrator.Infrastructure.Repositories
{
    public class PromptGenerationRepository : Repository<PromptGeneration>, IPromptGenerationRepository
    {
        public PromptGenerationRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<PromptGeneration?> GetByPromptIdAsync(string promptId, CancellationToken cancellationToken = default)
        {
            return await _context.PromptGenerations
                .Include(pg => pg.UserStory)
                .FirstOrDefaultAsync(pg => pg.PromptId == promptId, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<PromptGeneration?> GetByUserStoryIdAndIndexAsync(Guid userStoryId, int storyIndex, CancellationToken cancellationToken = default)
        {
            return await _context.PromptGenerations
                .FirstOrDefaultAsync(pg => pg.UserStoryId == userStoryId && pg.StoryIndex == storyIndex, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<IEnumerable<PromptGeneration>> GetByUserStoryIdAsync(Guid userStoryId, CancellationToken cancellationToken = default)
        {
            return await _context.PromptGenerations
                .Where(pg => pg.UserStoryId == userStoryId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<IEnumerable<PromptGeneration>> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default)
        {
            return await _context.PromptGenerations
                .Include(pg => pg.UserStory)
                    .ThenInclude(us => us.StoryGeneration)
                        .ThenInclude(sg => sg.ProjectPlanning)
                            .ThenInclude(pp => pp.RequirementsAnalysis)
                                .ThenInclude(ra => ra.Project)
                .Where(pg => pg.UserStory.StoryGeneration.ProjectPlanning.RequirementsAnalysis.ProjectId == projectId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
