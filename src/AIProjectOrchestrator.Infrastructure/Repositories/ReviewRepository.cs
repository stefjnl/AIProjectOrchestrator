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
        
        public async Task<int> DeleteReviewsByProjectIdAsync(int projectId, CancellationToken cancellationToken = default)
        {
            // Delete reviews associated with RequirementsAnalysis entities for the project
            var requirementsAnalysisReviews = await _context.Reviews
                .Where(r => r.RequirementsAnalysis != null && r.RequirementsAnalysis.ProjectId == projectId)
                .ToListAsync(cancellationToken);
            
            // Delete reviews associated with ProjectPlanning entities for the project
            // We need to join through RequirementsAnalysis to get to ProjectPlanning
            var projectPlanningReviews = await _context.Reviews
                .Where(r => r.ProjectPlanning != null && r.ProjectPlanning.RequirementsAnalysis.ProjectId == projectId)
                .ToListAsync(cancellationToken);
            
            // Delete reviews associated with StoryGeneration entities for the project
            // We need to join through RequirementsAnalysis -> ProjectPlanning to get to StoryGeneration
            var storyGenerationReviews = await _context.Reviews
                .Where(r => r.StoryGeneration != null && r.StoryGeneration.ProjectPlanning.RequirementsAnalysis.ProjectId == projectId)
                .ToListAsync(cancellationToken);
            
            // Delete reviews associated with PromptGeneration entities for the project
            // We need to join through RequirementsAnalysis -> ProjectPlanning -> StoryGeneration to get to PromptGeneration
            var promptGenerationReviews = await _context.Reviews
                .Where(r => r.PromptGeneration != null && r.PromptGeneration.StoryGeneration.ProjectPlanning.RequirementsAnalysis.ProjectId == projectId)
                .ToListAsync(cancellationToken);
            
            // Combine all reviews to delete
            var allReviewsToDelete = new List<Review>();
            allReviewsToDelete.AddRange(requirementsAnalysisReviews);
            allReviewsToDelete.AddRange(projectPlanningReviews);
            allReviewsToDelete.AddRange(storyGenerationReviews);
            allReviewsToDelete.AddRange(promptGenerationReviews);
            
            // Remove duplicates if any
            var uniqueReviewsToDelete = allReviewsToDelete.Distinct().ToList();
            
            _context.Reviews.RemoveRange(uniqueReviewsToDelete);
            
            // Save changes to ensure the reviews are deleted
            await _context.SaveChangesAsync(cancellationToken);
            
            return uniqueReviewsToDelete.Count;
        }
    }
}
