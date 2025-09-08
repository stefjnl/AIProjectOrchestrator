using System;
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
using Microsoft.Extensions.DependencyInjection;
using AIProjectOrchestrator.Domain.Interfaces;
using AIProjectOrchestrator.Domain.Entities;

namespace AIProjectOrchestrator.Application.Services
{
    public class ReviewService : IReviewService
    {
        private readonly ILogger<ReviewService> _logger;
        private readonly IOptions<ReviewSettings> _settings;
        private readonly IServiceProvider _serviceProvider;
        private readonly IReviewRepository _reviewRepository;
        

        public ReviewService(
            ILogger<ReviewService> logger,
            IOptions<ReviewSettings> settings,
            IServiceProvider serviceProvider,
            IReviewRepository reviewRepository)
        {
            _logger = logger;
            _settings = settings;
            _serviceProvider = serviceProvider;
            _reviewRepository = reviewRepository;
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

            // Create review submission
            var reviewId = Guid.NewGuid();
            
            // Create the database entity
            var reviewEntity = new Review
            {
                ReviewId = reviewId,
                Content = request.Content,
                Status = ReviewStatus.Pending,
                ServiceName = request.ServiceName,
                PipelineStage = request.PipelineStage,
                Feedback = string.Empty,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };

            // Set foreign key based on service name and metadata
            if (request.Metadata != null && request.Metadata.TryGetValue("EntityId", out var entityIdObj) && int.TryParse(entityIdObj.ToString(), out var entityId))
            {
                switch (request.ServiceName)
                {
                    case "RequirementsAnalysis":
                        reviewEntity.RequirementsAnalysisId = entityId;
                        break;
                    case "ProjectPlanning":
                        reviewEntity.ProjectPlanningId = entityId;
                        break;
                    case "StoryGeneration":
                        reviewEntity.StoryGenerationId = entityId;
                        break;
                    case "PromptGeneration":
                        reviewEntity.PromptGenerationId = entityId;
                        break;
                }
            }

            // Save to database
            await _reviewRepository.AddAsync(reviewEntity, cancellationToken);

            _logger.LogInformation("Review {ReviewId} submitted successfully for service {ServiceName} in pipeline stage {PipelineStage}", 
                reviewId, request.ServiceName, request.PipelineStage);

            return new ReviewResponse
            {
                ReviewId = reviewId,
                Status = ReviewStatus.Pending,
                SubmittedAt = DateTime.UtcNow,
                Message = "Review submitted successfully"
            };
        }

        public async Task<ReviewSubmission?> GetReviewAsync(Guid reviewId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Retrieving review {ReviewId}", reviewId);

            var reviewEntity = await _reviewRepository.GetByReviewIdAsync(reviewId, cancellationToken);
            if (reviewEntity != null)
            {
                // Check if review has expired
                if (reviewEntity.Status == ReviewStatus.Pending && 
                    DateTime.UtcNow.Subtract(reviewEntity.CreatedDate).TotalHours > _settings.Value.ReviewTimeoutHours)
                {
                    reviewEntity.Status = ReviewStatus.Expired;
                    reviewEntity.UpdatedDate = DateTime.UtcNow;
                    await _reviewRepository.UpdateAsync(reviewEntity, cancellationToken);
                    _logger.LogInformation("Review {ReviewId} has expired", reviewId);
                }

                return new ReviewSubmission
                {
                    Id = reviewEntity.ReviewId,
                    ServiceName = reviewEntity.ServiceName,
                    Content = reviewEntity.Content,
                    CorrelationId = string.Empty, // This would need to be stored in the database
                    PipelineStage = reviewEntity.PipelineStage,
                    Status = reviewEntity.Status,
                    SubmittedAt = reviewEntity.CreatedDate,
                    ReviewedAt = reviewEntity.UpdatedDate,
                    Decision = null, // This would need to be reconstructed from database fields
                    Metadata = new Dictionary<string, object>() // This would need to be stored in the database
                };
            }

            _logger.LogWarning("Review {ReviewId} not found", reviewId);
            return null;
        }

        public async Task<ReviewResponse> ApproveReviewAsync(Guid reviewId, ReviewDecisionRequest? decision = null, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Approving review {ReviewId}", reviewId);

            var reviewEntity = await _reviewRepository.GetByReviewIdAsync(reviewId, cancellationToken);
            if (reviewEntity == null)
            {
                _logger.LogWarning("Review {ReviewId} not found", reviewId);
                throw new InvalidOperationException($"Review {reviewId} not found");
            }

            // Check if review is in a valid state for approval
            if (reviewEntity.Status != ReviewStatus.Pending)
            {
                _logger.LogWarning("Review {ReviewId} is not in pending state. Current status: {Status}", reviewId, reviewEntity.Status);
                throw new InvalidOperationException($"Review is not in pending state. Current status: {reviewEntity.Status}");
            }

            // Check if review has expired
            if (DateTime.UtcNow.Subtract(reviewEntity.CreatedDate).TotalHours > _settings.Value.ReviewTimeoutHours)
            {
                reviewEntity.Status = ReviewStatus.Expired;
                reviewEntity.UpdatedDate = DateTime.UtcNow;
                await _reviewRepository.UpdateAsync(reviewEntity, cancellationToken);
                _logger.LogInformation("Review {ReviewId} has expired and cannot be approved", reviewId);
                throw new InvalidOperationException("Review has expired and cannot be approved");
            }

            // Update review
            reviewEntity.Status = ReviewStatus.Approved;
            reviewEntity.UpdatedDate = DateTime.UtcNow;
            reviewEntity.Feedback = decision?.Feedback ?? string.Empty;
            
            await _reviewRepository.UpdateAsync(reviewEntity, cancellationToken);

            _logger.LogInformation("Review {ReviewId} approved successfully", reviewId);

            // Notify that the review has been approved to trigger workflow progression
            var reviewSubmission = new ReviewSubmission
            {
                Id = reviewEntity.ReviewId,
                ServiceName = reviewEntity.ServiceName,
                Content = reviewEntity.Content,
                CorrelationId = string.Empty,
                PipelineStage = reviewEntity.PipelineStage,
                Status = reviewEntity.Status,
                SubmittedAt = reviewEntity.CreatedDate,
                ReviewedAt = reviewEntity.UpdatedDate,
                Decision = new ReviewDecision
                {
                    Status = ReviewStatus.Approved,
                    Reason = decision?.Reason ?? "Approved without specific reason",
                    Feedback = decision?.Feedback ?? string.Empty,
                    InstructionImprovements = new Dictionary<string, string>(decision?.InstructionImprovements ?? new Dictionary<string, string>())
                },
                Metadata = new Dictionary<string, object>() // This would need to be stored in the database
            };
            
            await NotifyReviewApprovedAsync(reviewId, reviewSubmission, cancellationToken);

            return new ReviewResponse
            {
                ReviewId = reviewEntity.ReviewId,
                Status = reviewEntity.Status,
                SubmittedAt = reviewEntity.CreatedDate,
                Message = "Review approved successfully"
            };
        }

        public async Task<ReviewResponse> RejectReviewAsync(Guid reviewId, ReviewDecisionRequest decision, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Rejecting review {ReviewId}", reviewId);

            var reviewEntity = await _reviewRepository.GetByReviewIdAsync(reviewId, cancellationToken);
            if (reviewEntity == null)
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
            if (reviewEntity.Status != ReviewStatus.Pending)
            {
                _logger.LogWarning("Review {ReviewId} is not in pending state. Current status: {Status}", reviewId, reviewEntity.Status);
                throw new InvalidOperationException($"Review is not in pending state. Current status: {reviewEntity.Status}");
            }

            // Check if review has expired
            if (DateTime.UtcNow.Subtract(reviewEntity.CreatedDate).TotalHours > _settings.Value.ReviewTimeoutHours)
            {
                reviewEntity.Status = ReviewStatus.Expired;
                reviewEntity.UpdatedDate = DateTime.UtcNow;
                await _reviewRepository.UpdateAsync(reviewEntity, cancellationToken);
                _logger.LogInformation("Review {ReviewId} has expired and cannot be rejected", reviewId);
                throw new InvalidOperationException("Review has expired and cannot be rejected");
            }

            // Update review
            reviewEntity.Status = ReviewStatus.Rejected;
            reviewEntity.UpdatedDate = DateTime.UtcNow;
            reviewEntity.Feedback = decision.Feedback ?? string.Empty;
            
            await _reviewRepository.UpdateAsync(reviewEntity, cancellationToken);

            _logger.LogInformation("Review {ReviewId} rejected successfully", reviewId);

            return new ReviewResponse
            {
                ReviewId = reviewEntity.ReviewId,
                Status = reviewEntity.Status,
                SubmittedAt = reviewEntity.CreatedDate,
                Message = "Review rejected successfully"
            };
        }

        public async Task<IEnumerable<ReviewSubmission>> GetPendingReviewsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Retrieving pending reviews");

            var reviewEntities = await _reviewRepository.GetPendingReviewsAsync(cancellationToken);
            var now = DateTime.UtcNow;
            var timeoutHours = _settings.Value.ReviewTimeoutHours;

            var pendingReviews = new List<ReviewSubmission>();
            foreach (var reviewEntity in reviewEntities)
            {
                // Check if review has expired
                if (reviewEntity.Status == ReviewStatus.Pending && 
                    now.Subtract(reviewEntity.CreatedDate).TotalHours <= timeoutHours)
                {
                    pendingReviews.Add(new ReviewSubmission
                    {
                        Id = reviewEntity.ReviewId,
                        ServiceName = reviewEntity.ServiceName,
                        Content = reviewEntity.Content,
                        CorrelationId = string.Empty,
                        PipelineStage = reviewEntity.PipelineStage,
                        Status = reviewEntity.Status,
                        SubmittedAt = reviewEntity.CreatedDate,
                        ReviewedAt = reviewEntity.UpdatedDate,
                        Decision = null,
                        Metadata = new Dictionary<string, object>()
                    });
                }
            }

            _logger.LogInformation("Found {PendingReviewCount} pending reviews", pendingReviews.Count);

            return pendingReviews;
        }

        public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Simple health check - verify we can access the storage
                var count = await _reviewRepository.GetAllAsync(cancellationToken);
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

            var reviewEntities = await _reviewRepository.GetAllAsync(cancellationToken);
            foreach (var reviewEntity in reviewEntities)
            {
                // Check if review has expired
                if (reviewEntity.Status == ReviewStatus.Pending && 
                    now.Subtract(reviewEntity.CreatedDate).TotalHours > timeoutHours)
                {
                    reviewEntity.Status = ReviewStatus.Expired;
                    reviewEntity.UpdatedDate = now;
                    await _reviewRepository.UpdateAsync(reviewEntity, cancellationToken);
                    expiredCount++;
                }
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
            // Aggregate pending reviews from database
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
            _logger.LogInformation("Review {ReviewId} approved, triggering workflow progression", reviewId);

            // First get the full review entity to access FKs
            var reviewEntity = await _reviewRepository.GetByReviewIdAsync(reviewId, cancellationToken);
            if (reviewEntity == null)
            {
                _logger.LogWarning("Review entity {ReviewId} not found for propagation", reviewId);
                return;
            }

            using (var scope = _serviceProvider.CreateScope())
            {
                if (review.ServiceName == "RequirementsAnalysis" && reviewEntity.RequirementsAnalysisId.HasValue)
                {
                    // Fetch the analysis entity using the FK
                    var requirementsRepo = scope.ServiceProvider.GetRequiredService<IRequirementsAnalysisRepository>();
                    var analysisEntity = await requirementsRepo.GetByIdAsync(reviewEntity.RequirementsAnalysisId.Value, cancellationToken);
                    if (analysisEntity != null && Guid.TryParse(analysisEntity.AnalysisId, out var analysisId))
                    {
                        var requirementsService = scope.ServiceProvider.GetRequiredService<IRequirementsAnalysisService>();
                        await requirementsService.UpdateAnalysisStatusAsync(analysisId, Domain.Models.RequirementsAnalysisStatus.Approved, cancellationToken);
                        _logger.LogInformation("Propagated approval to RequirementsAnalysis {AnalysisId} via FK {EntityId}", analysisId, reviewEntity.RequirementsAnalysisId.Value);
                    }
                    else
                    {
                        _logger.LogWarning("Could not find RequirementsAnalysis entity for review {ReviewId} using FK {EntityId}", reviewId, reviewEntity.RequirementsAnalysisId.Value);
                    }
                }
                else if (review.ServiceName == "ProjectPlanning" && reviewEntity.ProjectPlanningId.HasValue)
                {
                    // Similar logic for ProjectPlanning
                    var planningRepo = scope.ServiceProvider.GetRequiredService<IProjectPlanningRepository>();
                    var planningEntity = await planningRepo.GetByIdAsync(reviewEntity.ProjectPlanningId.Value, cancellationToken);
                    if (planningEntity != null && Guid.TryParse(planningEntity.PlanningId, out var planningId))
                    {
                        var planningService = scope.ServiceProvider.GetRequiredService<IProjectPlanningService>();
                        await planningService.UpdatePlanningStatusAsync(planningId, Domain.Models.ProjectPlanningStatus.Approved, cancellationToken);
                        _logger.LogInformation("Propagated approval to ProjectPlanning {PlanningId} via FK {EntityId}", planningId, reviewEntity.ProjectPlanningId.Value);
                    }
                    else
                    {
                        _logger.LogWarning("Could not find ProjectPlanning entity for review {ReviewId} using FK {EntityId}", reviewId, reviewEntity.ProjectPlanningId.Value);
                    }
                }
                else if (review.ServiceName == "StoryGeneration" && reviewEntity.StoryGenerationId.HasValue)
                {
                    // Similar logic for StoryGeneration
                    var storyRepo = scope.ServiceProvider.GetRequiredService<IStoryGenerationRepository>();
                    var storyEntity = await storyRepo.GetByIdAsync(reviewEntity.StoryGenerationId.Value, cancellationToken);
                    if (storyEntity != null && Guid.TryParse(storyEntity.GenerationId, out var generationId))
                    {
                        var storyService = scope.ServiceProvider.GetRequiredService<IStoryGenerationService>();
                        await storyService.UpdateGenerationStatusAsync(generationId, Domain.Models.Stories.StoryGenerationStatus.Approved, cancellationToken);
                        _logger.LogInformation("Propagated approval to StoryGeneration {GenerationId} via FK {EntityId}", generationId, reviewEntity.StoryGenerationId.Value);
                    }
                    else
                    {
                        _logger.LogWarning("Could not find StoryGeneration entity for review {ReviewId} using FK {EntityId}", reviewId, reviewEntity.StoryGenerationId.Value);
                    }
                }
                else
                {
                    _logger.LogWarning("No valid FK found for service {ServiceName} in review {ReviewId}", review.ServiceName, reviewId);
                }
            }
        }
        
        public async Task DeleteReviewsByProjectIdAsync(int projectId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Deleting reviews for project ID {ProjectId}", projectId);
            
            var deletedCount = await _reviewRepository.DeleteReviewsByProjectIdAsync(projectId, cancellationToken);
            
            _logger.LogInformation("Deleted {DeletedCount} reviews for project ID {ProjectId}", deletedCount, projectId);
        }
    }
}
