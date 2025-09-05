using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Models.Review;
using AIProjectOrchestrator.Domain.Models.Review.Dashboard;

namespace AIProjectOrchestrator.Domain.Services
{
    public interface IReviewService
    {
        Task<ReviewResponse> SubmitForReviewAsync(SubmitReviewRequest request, CancellationToken cancellationToken = default);
        Task<ReviewSubmission?> GetReviewAsync(Guid reviewId, CancellationToken cancellationToken = default);
        Task<ReviewResponse> ApproveReviewAsync(Guid reviewId, ReviewDecisionRequest? decision = null, CancellationToken cancellationToken = default);
        Task<ReviewResponse> RejectReviewAsync(Guid reviewId, ReviewDecisionRequest decision, CancellationToken cancellationToken = default);
        Task<IEnumerable<ReviewSubmission>> GetPendingReviewsAsync(CancellationToken cancellationToken = default);
        Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
        Task<int> CleanupExpiredReviewsAsync(CancellationToken cancellationToken = default);
        
        // Add to IReviewService (Domain layer)
        Task<ReviewDashboardData> GetDashboardDataAsync(CancellationToken cancellationToken = default);
        Task<WorkflowStatusItem?> GetWorkflowStatusAsync(Guid projectId, CancellationToken cancellationToken = default);
        
        // Method to handle workflow progression after review approval
        Task NotifyReviewApprovedAsync(Guid reviewId, ReviewSubmission review, CancellationToken cancellationToken = default);
    }
}