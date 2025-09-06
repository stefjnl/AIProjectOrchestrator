using AIProjectOrchestrator.Domain.Entities;

namespace AIProjectOrchestrator.Domain.Interfaces
{
    public interface IReviewRepository : IRepository<Review>
    {
        Task<Review?> GetByReviewIdAsync(Guid reviewId, CancellationToken cancellationToken = default);
        Task<Review?> GetByWorkflowEntityIdAsync(int entityId, string entityType, CancellationToken cancellationToken = default);
        Task<IEnumerable<Review>> GetPendingReviewsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Review>> GetReviewsByServiceAsync(string serviceName, CancellationToken cancellationToken = default);
        Task<IEnumerable<Review>> GetReviewsByPipelineStageAsync(string pipelineStage, CancellationToken cancellationToken = default);
    }
}
