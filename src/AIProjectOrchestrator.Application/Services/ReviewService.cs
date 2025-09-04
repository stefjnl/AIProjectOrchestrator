using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AIProjectOrchestrator.Domain.Models.Review;
using AIProjectOrchestrator.Domain.Models.Review.Dashboard;
using AIProjectOrchestrator.Domain.Services;
using AIProjectOrchestrator.Domain.Configuration;

namespace AIProjectOrchestrator.Application.Services
{
    public class ReviewService : IReviewService
    {
        private readonly ConcurrentDictionary<Guid, ReviewSubmission> _reviews;
        private readonly ILogger<ReviewService> _logger;
        private readonly IOptions<ReviewSettings> _settings;

        public ReviewService(
            ILogger<ReviewService> logger,
            IOptions<ReviewSettings> settings)
        {
            _reviews = new ConcurrentDictionary<Guid, ReviewSubmission>();
            _logger = logger;
            _settings = settings;
        }

        public async Task<ReviewResponse> SubmitForReviewAsync(SubmitReviewRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Submitting review for service {ServiceName} with correlation ID {CorrelationId}", 
                request.ServiceName, request.CorrelationId);

            // Validate request
            if (string.IsNullOrWhiteSpace(request.ServiceName))
            {
                throw new ArgumentException("Service name is required", nameof(request.ServiceName));
            }

            if (string.IsNullOrWhiteSpace(request.Content))
            {
                throw new ArgumentException("Content is required", nameof(request.Content));
            }

            if (request.Content.Length > _settings.Value.MaxContentLength)
            {
                throw new ArgumentException($"Content exceeds maximum length of {_settings.Value.MaxContentLength} characters", nameof(request.Content));
            }

            if (string.IsNullOrWhiteSpace(request.CorrelationId))
            {
                throw new ArgumentException("Correlation ID is required", nameof(request.CorrelationId));
            }

            if (string.IsNullOrWhiteSpace(request.PipelineStage))
            {
                throw new ArgumentException("Pipeline stage is required", nameof(request.PipelineStage));
            }

            if (!_settings.Value.ValidPipelineStages.Contains(request.PipelineStage))
            {
                throw new ArgumentException($"Invalid pipeline stage. Valid stages are: {string.Join(", ", _settings.Value.ValidPipelineStages)}", nameof(request.PipelineStage));
            }

            // Check if we're at max capacity
            if (_reviews.Count >= _settings.Value.MaxConcurrentReviews)
            {
                _logger.LogWarning("Maximum concurrent reviews reached ({MaxConcurrentReviews})", _settings.Value.MaxConcurrentReviews);
                throw new InvalidOperationException($"Maximum concurrent reviews ({_settings.Value.MaxConcurrentReviews}) reached");
            }

            // Create review submission
            var review = new ReviewSubmission
            {
                Id = Guid.NewGuid(),
                ServiceName = request.ServiceName,
                Content = request.Content,
                CorrelationId = request.CorrelationId,
                PipelineStage = request.PipelineStage,
                OriginalRequest = request.OriginalRequest,
                AIResponse = request.AIResponse,
                Metadata = new Dictionary<string, object>(request.Metadata)
            };

            // Add to storage
            if (!_reviews.TryAdd(review.Id, review))
            {
                _logger.LogError("Failed to add review {ReviewId} to storage", review.Id);
                throw new InvalidOperationException("Failed to submit review");
            }

            _logger.LogInformation("Review {ReviewId} submitted successfully for service {ServiceName} in pipeline stage {PipelineStage}", 
                review.Id, request.ServiceName, request.PipelineStage);

            return new ReviewResponse
            {
                ReviewId = review.Id,
                Status = review.Status,
                SubmittedAt = review.SubmittedAt,
                Message = "Review submitted successfully"
            };
        }

        public async Task<ReviewSubmission?> GetReviewAsync(Guid reviewId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Retrieving review {ReviewId}", reviewId);

            if (_reviews.TryGetValue(reviewId, out var review))
            {
                // Check if review has expired
                if (review.Status == ReviewStatus.Pending && 
                    DateTime.UtcNow.Subtract(review.SubmittedAt).TotalHours > _settings.Value.ReviewTimeoutHours)
                {
                    review.Status = ReviewStatus.Expired;
                    review.ReviewedAt = DateTime.UtcNow;
                    _logger.LogInformation("Review {ReviewId} has expired", reviewId);
                }

                return review;
            }

            _logger.LogWarning("Review {ReviewId} not found", reviewId);
            return null;
        }

        public async Task<ReviewResponse> ApproveReviewAsync(Guid reviewId, ReviewDecisionRequest? decision = null, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Approving review {ReviewId}", reviewId);

            if (!_reviews.TryGetValue(reviewId, out var review))
            {
                _logger.LogWarning("Review {ReviewId} not found", reviewId);
                throw new InvalidOperationException($"Review {reviewId} not found");
            }

            // Check if review is in a valid state for approval
            if (review.Status != ReviewStatus.Pending)
            {
                _logger.LogWarning("Review {ReviewId} is not in pending state. Current status: {Status}", reviewId, review.Status);
                throw new InvalidOperationException($"Review is not in pending state. Current status: {review.Status}");
            }

            // Check if review has expired
            if (DateTime.UtcNow.Subtract(review.SubmittedAt).TotalHours > _settings.Value.ReviewTimeoutHours)
            {
                review.Status = ReviewStatus.Expired;
                review.ReviewedAt = DateTime.UtcNow;
                _logger.LogInformation("Review {ReviewId} has expired and cannot be approved", reviewId);
                throw new InvalidOperationException("Review has expired and cannot be approved");
            }

            // Update review
            review.Status = ReviewStatus.Approved;
            review.ReviewedAt = DateTime.UtcNow;
            
            review.Decision = new ReviewDecision
            {
                Status = ReviewStatus.Approved,
                Reason = decision?.Reason ?? "Approved without specific reason",
                Feedback = decision?.Feedback ?? string.Empty,
                InstructionImprovements = new Dictionary<string, string>(decision?.InstructionImprovements ?? new Dictionary<string, string>())
            };

            _logger.LogInformation("Review {ReviewId} approved successfully", reviewId);

            // Notify that the review has been approved to trigger workflow progression
            await NotifyReviewApprovedAsync(reviewId, review, cancellationToken);

            return new ReviewResponse
            {
                ReviewId = review.Id,
                Status = review.Status,
                SubmittedAt = review.SubmittedAt,
                Message = "Review approved successfully"
            };
        }

        public async Task<ReviewResponse> RejectReviewAsync(Guid reviewId, ReviewDecisionRequest decision, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Rejecting review {ReviewId}", reviewId);

            if (!_reviews.TryGetValue(reviewId, out var review))
            {
                _logger.LogWarning("Review {ReviewId} not found", reviewId);
                throw new InvalidOperationException($"Review {reviewId} not found");
            }

            // Validate decision
            if (string.IsNullOrWhiteSpace(decision.Reason))
            {
                throw new ArgumentException("Reason is required for rejection", nameof(decision.Reason));
            }

            // Check if review is in a valid state for rejection
            if (review.Status != ReviewStatus.Pending)
            {
                _logger.LogWarning("Review {ReviewId} is not in pending state. Current status: {Status}", reviewId, review.Status);
                throw new InvalidOperationException($"Review is not in pending state. Current status: {review.Status}");
            }

            // Check if review has expired
            if (DateTime.UtcNow.Subtract(review.SubmittedAt).TotalHours > _settings.Value.ReviewTimeoutHours)
            {
                review.Status = ReviewStatus.Expired;
                review.ReviewedAt = DateTime.UtcNow;
                _logger.LogInformation("Review {ReviewId} has expired and cannot be rejected", reviewId);
                throw new InvalidOperationException("Review has expired and cannot be rejected");
            }

            // Update review
            review.Status = ReviewStatus.Rejected;
            review.ReviewedAt = DateTime.UtcNow;
            
            review.Decision = new ReviewDecision
            {
                Status = ReviewStatus.Rejected,
                Reason = decision.Reason,
                Feedback = decision.Feedback ?? string.Empty,
                InstructionImprovements = new Dictionary<string, string>(decision.InstructionImprovements ?? new Dictionary<string, string>())
            };

            _logger.LogInformation("Review {ReviewId} rejected successfully", reviewId);

            return new ReviewResponse
            {
                ReviewId = review.Id,
                Status = review.Status,
                SubmittedAt = review.SubmittedAt,
                Message = "Review rejected successfully"
            };
        }

        public async Task<IEnumerable<ReviewSubmission>> GetPendingReviewsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Retrieving pending reviews");

            var now = DateTime.UtcNow;
            var timeoutHours = _settings.Value.ReviewTimeoutHours;

            var pendingReviews = _reviews.Values
                .Where(r => r.Status == ReviewStatus.Pending)
                .Where(r => now.Subtract(r.SubmittedAt).TotalHours <= timeoutHours)
                .ToList();

            _logger.LogInformation("Found {PendingReviewCount} pending reviews", pendingReviews.Count);

            return pendingReviews;
        }

        public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Simple health check - verify we can access the storage
                var count = _reviews.Count;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed for ReviewService");
                return false;
            }
        }

        public async Task<int> CleanupExpiredReviewsAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var timeoutHours = _settings.Value.ReviewTimeoutHours;
            var expiredCount = 0;

            var expiredReviews = _reviews.Values
                .Where(r => r.Status == ReviewStatus.Pending)
                .Where(r => now.Subtract(r.SubmittedAt).TotalHours > timeoutHours)
                .ToList();

            foreach (var review in expiredReviews)
            {
                review.Status = ReviewStatus.Expired;
                review.ReviewedAt = now;
                expiredCount++;
            }

            if (expiredCount > 0)
            {
                _logger.LogInformation("Cleaned up {ExpiredReviewCount} expired reviews", expiredCount);
            }

            return expiredCount;
        }
        
        // Add to ReviewService (Application layer)
        public async Task<ReviewDashboardData> GetDashboardDataAsync(CancellationToken cancellationToken)
        {
            // Aggregate pending reviews from in-memory storage
            // Cross-reference with requirements/planning/story services to build workflow status
            // Return structured data for dashboard consumption
            
            var pendingReviews = await GetPendingReviewsAsync(cancellationToken);
            
            var dashboardData = new ReviewDashboardData
            {
                PendingReviews = pendingReviews.Select(r => new PendingReviewItem
                {
                    ReviewId = r.Id,
                    ServiceType = r.ServiceName,
                    Title = r.PipelineStage,
                    Content = r.Content,
                    OriginalRequest = r.OriginalRequest?.Prompt ?? string.Empty,
                    SubmittedAt = r.SubmittedAt
                }).ToList(),
                ActiveWorkflows = new List<WorkflowStatusItem>(), // Will be implemented later
                LastUpdated = DateTime.UtcNow
            };

            return dashboardData;
        }

        public async Task<WorkflowStatusItem?> GetWorkflowStatusAsync(Guid projectId, CancellationToken cancellationToken = default)
        {
            // Track complete workflow status across all services
            // Requirements status, Planning status, Stories status
            // Return current stage and next required action
            
            // This is a placeholder implementation
            // In a real implementation, we would query the actual workflow status
            return null;
        }
        
        public async Task NotifyReviewApprovedAsync(Guid reviewId, ReviewSubmission review, CancellationToken cancellationToken = default)
        {
            // This method will be called after a review is approved
            // It should trigger the next stage in the workflow based on the review metadata
            _logger.LogInformation("Review {ReviewId} approved, triggering workflow progression", reviewId);
            
            // For now, we'll just log the metadata
            foreach (var kvp in review.Metadata)
            {
                _logger.LogDebug("Review {ReviewId} metadata: {Key} = {Value}", reviewId, kvp.Key, kvp.Value);
            }
            
            // In a real implementation, we would:
            // 1. Check the ServiceName and PipelineStage
            // 2. Based on that, trigger the next stage in the workflow
            // 3. Update project state or notify relevant services
            
            // Example logic:
            // if (review.ServiceName == "RequirementsAnalysis" && review.PipelineStage == "Analysis")
            // {
            //     // Enable project planning for this project
            //     // This would require a project tracking system
            // }
        }
    }
}