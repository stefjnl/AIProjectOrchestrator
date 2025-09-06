using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Interfaces;
using AIProjectOrchestrator.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using AIProjectOrchestrator.Domain.Models.Review;

namespace AIProjectOrchestrator.Infrastructure.Repositories
{
    public class ReviewRepository : Repository<Review>, IReviewRepository
    {
        public ReviewRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Review?> GetByReviewIdAsync(Guid reviewId, CancellationToken cancellationToken = default)
        {
            return await _context.Reviews
                .FirstOrDefaultAsync(r => r.ReviewId == reviewId, cancellationToken);
        }

        public async Task<Review?> GetByWorkflowEntityIdAsync(int entityId, string entityType, CancellationToken cancellationToken = default)
        {
            return entityType.ToLower() switch
            {
                "requirementsanalysis" => await _context.Reviews
                    .FirstOrDefaultAsync(r => r.RequirementsAnalysis != null && r.RequirementsAnalysis.Id == entityId, cancellationToken),
                "projectplanning" => await _context.Reviews
                    .FirstOrDefaultAsync(r => r.ProjectPlanning != null && r.ProjectPlanning.Id == entityId, cancellationToken),
                "storygeneration" => await _context.Reviews
                    .FirstOrDefaultAsync(r => r.StoryGeneration != null && r.StoryGeneration.Id == entityId, cancellationToken),
                "promptgeneration" => await _context.Reviews
                    .FirstOrDefaultAsync(r => r.PromptGeneration != null && r.PromptGeneration.Id == entityId, cancellationToken),
                _ => null
            };
        }

        public async Task<IEnumerable<Review>> GetPendingReviewsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Reviews
                .Where(r => r.Status == ReviewStatus.Pending)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Review>> GetReviewsByServiceAsync(string serviceName, CancellationToken cancellationToken = default)
        {
            return await _context.Reviews
                .Where(r => r.ServiceName == serviceName)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Review>> GetReviewsByPipelineStageAsync(string pipelineStage, CancellationToken cancellationToken = default)
        {
            return await _context.Reviews
                .Where(r => r.PipelineStage == pipelineStage)
                .ToListAsync(cancellationToken);
        }
    }
}
