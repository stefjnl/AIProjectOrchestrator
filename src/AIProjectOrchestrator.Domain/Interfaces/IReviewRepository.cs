using AIProjectOrchestrator.Domain.Entities;

namespace AIProjectOrchestrator.Domain.Interfaces
{
    /// <summary>
    /// Repository for Review entities (int ID).
    /// Follows Interface Segregation Principle by using IFullRepository.
    /// Includes domain-specific query methods for workflow operations.
    /// </summary>
    public interface IReviewRepository : IFullRepository<Review, int>
    {
        // IFullRepository provides: GetByIdAsync(int), GetAllAsync, AddAsync, UpdateAsync, DeleteAsync(int)
        
        // Domain-specific query methods
        Task<Review?> GetByReviewIdAsync(Guid reviewId, CancellationToken cancellationToken = default);
        Task<Review?> GetByWorkflowEntityIdAsync(int entityId, string entityType, CancellationToken cancellationToken = default);
        Task<IEnumerable<Review>> GetPendingReviewsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Review>> GetReviewsByServiceAsync(string serviceName, CancellationToken cancellationToken = default);
        Task<IEnumerable<Review>> GetReviewsByPipelineStageAsync(string pipelineStage, CancellationToken cancellationToken = default);
        Task<int> DeleteReviewsByProjectIdAsync(int projectId, CancellationToken cancellationToken = default);

        Task<IEnumerable<Review>> GetPendingReviewsWithProjectAsync(CancellationToken cancellationToken = default);
        Task<Review?> GetReviewWithWorkflowAsync(Guid reviewId, CancellationToken cancellationToken = default);
        Task<int> DeleteReviewWithCascadesAsync(Review review, CancellationToken cancellationToken = default);
    }
}
