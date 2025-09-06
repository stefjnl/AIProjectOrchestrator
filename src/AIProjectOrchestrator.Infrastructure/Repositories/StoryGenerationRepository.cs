using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Interfaces;
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
    }
}
