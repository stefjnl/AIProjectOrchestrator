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
                .FirstOrDefaultAsync(pg => pg.PromptId == promptId, cancellationToken);
        }

        public async Task<PromptGeneration?> GetByStoryGenerationIdAndIndexAsync(int storyGenerationId, int storyIndex, CancellationToken cancellationToken = default)
        {
            return await _context.PromptGenerations
                .FirstOrDefaultAsync(pg => pg.StoryGenerationId == storyGenerationId && pg.StoryIndex == storyIndex, cancellationToken);
        }

        public async Task<IEnumerable<PromptGeneration>> GetByStoryGenerationIdAsync(int storyGenerationId, CancellationToken cancellationToken = default)
        {
            return await _context.PromptGenerations
                .Where(pg => pg.StoryGenerationId == storyGenerationId)
                .ToListAsync(cancellationToken);
        }
    }
}
