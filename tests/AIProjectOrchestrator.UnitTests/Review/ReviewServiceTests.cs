using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using AIProjectOrchestrator.Domain.Configuration;
using AIProjectOrchestrator.Domain.Models.AI;
using AIProjectOrchestrator.Domain.Models.Review;
using AIProjectOrchestrator.Application.Services;
using AIProjectOrchestrator.Domain.Services;
using Xunit;

namespace AIProjectOrchestrator.UnitTests.Review
{
    public class ReviewServiceTests
    {
        private readonly Mock<ILogger<ReviewService>> _mockLogger;
        private readonly Mock<IOptions<ReviewSettings>> _mockSettings;
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<IRequirementsAnalysisService> _mockRequirementsAnalysisService;
        private readonly Mock<IProjectPlanningService> _mockProjectPlanningService;
        private readonly Mock<IStoryGenerationService> _mockStoryGenerationService;
        private readonly ReviewSettings _settings;

        public ReviewServiceTests()
        {
            _mockLogger = new Mock<ILogger<ReviewService>>();
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockRequirementsAnalysisService = new Mock<IRequirementsAnalysisService>();
            _mockProjectPlanningService = new Mock<IProjectPlanningService>();
            _mockStoryGenerationService = new Mock<IStoryGenerationService>();
            
            _settings = new ReviewSettings
            {
                MaxConcurrentReviews = 100,
                ReviewTimeoutHours = 24,
                MaxContentLength = 50000,
                ValidPipelineStages = new List<string> { "Analysis", "Planning", "Stories", "Implementation", "Review" }
            };
            _mockSettings = new Mock<IOptions<ReviewSettings>>();
            _mockSettings.Setup(s => s.Value).Returns(_settings);
        }

        [Fact]
        public async Task SubmitForReviewAsync_WithValidRequest_CreatesReview()
        {
            // Arrange
            var service = new ReviewService(_mockLogger.Object, _mockSettings.Object, _mockServiceProvider.Object);
            var request = new SubmitReviewRequest
            {
                ServiceName = "TestService",
                Content = "Test content",
                CorrelationId = "test-correlation-id",
                PipelineStage = "Analysis"
            };

            // Act
            var response = await service.SubmitForReviewAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.NotEqual(Guid.Empty, response.ReviewId);
            Assert.Equal(ReviewStatus.Pending, response.Status);
            Assert.True(response.SubmittedAt <= DateTime.UtcNow);
            Assert.Equal("Review submitted successfully", response.Message);
        }

        [Fact]
        public async Task SubmitForReviewAsync_WithMissingServiceName_ThrowsArgumentException()
        {
            // Arrange
            var service = new ReviewService(_mockLogger.Object, _mockSettings.Object, _mockServiceProvider.Object);
            var request = new SubmitReviewRequest
            {
                Content = "Test content",
                CorrelationId = "test-correlation-id",
                PipelineStage = "Analysis"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.SubmitForReviewAsync(request));
        }

        [Fact]
        public async Task SubmitForReviewAsync_WithMissingContent_ThrowsArgumentException()
        {
            // Arrange
            var service = new ReviewService(_mockLogger.Object, _mockSettings.Object, _mockServiceProvider.Object);
            var request = new SubmitReviewRequest
            {
                ServiceName = "TestService",
                CorrelationId = "test-correlation-id",
                PipelineStage = "Analysis"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.SubmitForReviewAsync(request));
        }

        [Fact]
        public async Task SubmitForReviewAsync_WithMissingCorrelationId_ThrowsArgumentException()
        {
            // Arrange
            var service = new ReviewService(_mockLogger.Object, _mockSettings.Object, _mockServiceProvider.Object);
            var request = new SubmitReviewRequest
            {
                ServiceName = "TestService",
                Content = "Test content",
                PipelineStage = "Analysis"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.SubmitForReviewAsync(request));
        }

        [Fact]
        public async Task SubmitForReviewAsync_WithMissingPipelineStage_ThrowsArgumentException()
        {
            // Arrange
            var service = new ReviewService(_mockLogger.Object, _mockSettings.Object, _mockServiceProvider.Object);
            var request = new SubmitReviewRequest
            {
                ServiceName = "TestService",
                Content = "Test content",
                CorrelationId = "test-correlation-id"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.SubmitForReviewAsync(request));
        }

        [Fact]
        public async Task SubmitForReviewAsync_WithInvalidPipelineStage_ThrowsArgumentException()
        {
            // Arrange
            var service = new ReviewService(_mockLogger.Object, _mockSettings.Object, _mockServiceProvider.Object);
            var request = new SubmitReviewRequest
            {
                ServiceName = "TestService",
                Content = "Test content",
                CorrelationId = "test-correlation-id",
                PipelineStage = "InvalidStage"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.SubmitForReviewAsync(request));
        }

        [Fact]
        public async Task SubmitForReviewAsync_WithContentExceedingMaxLength_ThrowsArgumentException()
        {
            // Arrange
            var service = new ReviewService(_mockLogger.Object, _mockSettings.Object, _mockServiceProvider.Object);
            var request = new SubmitReviewRequest
            {
                ServiceName = "TestService",
                Content = new string('a', _settings.MaxContentLength + 1),
                CorrelationId = "test-correlation-id",
                PipelineStage = "Analysis"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.SubmitForReviewAsync(request));
        }

        [Fact]
        public async Task GetReviewAsync_WithExistingReview_ReturnsReview()
        {
            // Arrange
            var service = new ReviewService(_mockLogger.Object, _mockSettings.Object, _mockServiceProvider.Object);
            var request = new SubmitReviewRequest
            {
                ServiceName = "TestService",
                Content = "Test content",
                CorrelationId = "test-correlation-id",
                PipelineStage = "Analysis"
            };

            var response = await service.SubmitForReviewAsync(request);
            var reviewId = response.ReviewId;

            // Act
            var review = await service.GetReviewAsync(reviewId);

            // Assert
            Assert.NotNull(review);
            Assert.Equal(reviewId, review.Id);
            Assert.Equal("TestService", review.ServiceName);
            Assert.Equal("Test content", review.Content);
            Assert.Equal("test-correlation-id", review.CorrelationId);
            Assert.Equal("Analysis", review.PipelineStage);
            Assert.Equal(ReviewStatus.Pending, review.Status);
        }

        [Fact]
        public async Task GetReviewAsync_WithNonExistentReview_ReturnsNull()
        {
            // Arrange
            var service = new ReviewService(_mockLogger.Object, _mockSettings.Object, _mockServiceProvider.Object);
            var reviewId = Guid.NewGuid();

            // Act
            var review = await service.GetReviewAsync(reviewId);

            // Assert
            Assert.Null(review);
        }

        [Fact]
        public async Task ApproveReviewAsync_WithValidReview_ApprovesReview()
        {
            // Arrange
            var service = new ReviewService(_mockLogger.Object, _mockSettings.Object, _mockServiceProvider.Object);
            var request = new SubmitReviewRequest
            {
                ServiceName = "TestService",
                Content = "Test content",
                CorrelationId = "test-correlation-id",
                PipelineStage = "Analysis"
            };

            var submitResponse = await service.SubmitForReviewAsync(request);
            var reviewId = submitResponse.ReviewId;

            var decisionRequest = new ReviewDecisionRequest
            {
                Reason = "Looks good",
                Feedback = "Great work"
            };

            // Act
            var response = await service.ApproveReviewAsync(reviewId, decisionRequest);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(reviewId, response.ReviewId);
            Assert.Equal(ReviewStatus.Approved, response.Status);
            Assert.Equal("Review approved successfully", response.Message);

            // Verify the review was actually updated
            var review = await service.GetReviewAsync(reviewId);
            Assert.NotNull(review);
            Assert.Equal(ReviewStatus.Approved, review.Status);
            Assert.NotNull(review.Decision);
            Assert.Equal(ReviewStatus.Approved, review.Decision.Status);
            Assert.Equal("Looks good", review.Decision.Reason);
            Assert.Equal("Great work", review.Decision.Feedback);
            Assert.NotNull(review.ReviewedAt);
        }

        [Fact]
        public async Task ApproveReviewAsync_WithNonExistentReview_ThrowsInvalidOperationException()
        {
            // Arrange
            var service = new ReviewService(_mockLogger.Object, _mockSettings.Object, _mockServiceProvider.Object);
            var reviewId = Guid.NewGuid();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApproveReviewAsync(reviewId));
        }

        [Fact]
        public async Task RejectReviewAsync_WithValidReview_RejectsReview()
        {
            // Arrange
            var service = new ReviewService(_mockLogger.Object, _mockSettings.Object, _mockServiceProvider.Object);
            var request = new SubmitReviewRequest
            {
                ServiceName = "TestService",
                Content = "Test content",
                CorrelationId = "test-correlation-id",
                PipelineStage = "Analysis"
            };

            var submitResponse = await service.SubmitForReviewAsync(request);
            var reviewId = submitResponse.ReviewId;

            var decisionRequest = new ReviewDecisionRequest
            {
                Reason = "Needs improvement",
                Feedback = "Please fix this issue"
            };

            // Act
            var response = await service.RejectReviewAsync(reviewId, decisionRequest);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(reviewId, response.ReviewId);
            Assert.Equal(ReviewStatus.Rejected, response.Status);
            Assert.Equal("Review rejected successfully", response.Message);

            // Verify the review was actually updated
            var review = await service.GetReviewAsync(reviewId);
            Assert.NotNull(review);
            Assert.Equal(ReviewStatus.Rejected, review.Status);
            Assert.NotNull(review.Decision);
            Assert.Equal(ReviewStatus.Rejected, review.Decision.Status);
            Assert.Equal("Needs improvement", review.Decision.Reason);
            Assert.Equal("Please fix this issue", review.Decision.Feedback);
            Assert.NotNull(review.ReviewedAt);
        }

        [Fact]
        public async Task RejectReviewAsync_WithNonExistentReview_ThrowsInvalidOperationException()
        {
            // Arrange
            var service = new ReviewService(_mockLogger.Object, _mockSettings.Object, _mockServiceProvider.Object);
            var reviewId = Guid.NewGuid();
            var decisionRequest = new ReviewDecisionRequest
            {
                Reason = "Test reason"
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.RejectReviewAsync(reviewId, decisionRequest));
        }

        [Fact]
        public async Task RejectReviewAsync_WithoutReason_ThrowsArgumentException()
        {
            // Arrange
            var service = new ReviewService(_mockLogger.Object, _mockSettings.Object, _mockServiceProvider.Object);
            var request = new SubmitReviewRequest
            {
                ServiceName = "TestService",
                Content = "Test content",
                CorrelationId = "test-correlation-id",
                PipelineStage = "Analysis"
            };

            var submitResponse = await service.SubmitForReviewAsync(request);
            var reviewId = submitResponse.ReviewId;

            var decisionRequest = new ReviewDecisionRequest
            {
                Reason = "" // Empty reason
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.RejectReviewAsync(reviewId, decisionRequest));
        }

        [Fact]
        public async Task GetPendingReviewsAsync_ReturnsOnlyPendingReviews()
        {
            // Arrange
            var service = new ReviewService(_mockLogger.Object, _mockSettings.Object, _mockServiceProvider.Object);
            
            // Create a pending review
            var pendingRequest = new SubmitReviewRequest
            {
                ServiceName = "PendingService",
                Content = "Pending content",
                CorrelationId = "pending-correlation-id",
                PipelineStage = "Analysis"
            };
            await service.SubmitForReviewAsync(pendingRequest);

            // Create an approved review
            var approvedRequest = new SubmitReviewRequest
            {
                ServiceName = "ApprovedService",
                Content = "Approved content",
                CorrelationId = "approved-correlation-id",
                PipelineStage = "Analysis"
            };
            var approvedResponse = await service.SubmitForReviewAsync(approvedRequest);
            await service.ApproveReviewAsync(approvedResponse.ReviewId);

            // Act
            var pendingReviews = await service.GetPendingReviewsAsync();

            // Assert
            Assert.NotNull(pendingReviews);
            var pendingReviewsList = pendingReviews.ToList();
            Assert.Single(pendingReviewsList);
            Assert.Equal("PendingService", pendingReviewsList[0].ServiceName);
            Assert.Equal(ReviewStatus.Pending, pendingReviewsList[0].Status);
        }

        [Fact]
        public async Task IsHealthyAsync_ReturnsTrue()
        {
            // Arrange
            var service = new ReviewService(_mockLogger.Object, _mockSettings.Object, _mockServiceProvider.Object);

            // Act
            var isHealthy = await service.IsHealthyAsync();

            // Assert
            Assert.True(isHealthy);
        }

        [Fact]
        public async Task CleanupExpiredReviewsAsync_MarksExpiredReviewsAsExpired()
        {
            // Arrange
            // Create settings with 0 timeout hours for testing
            var testSettings = new ReviewSettings
            {
                MaxConcurrentReviews = 100,
                ReviewTimeoutHours = 0, // Expire immediately
                MaxContentLength = 50000,
                ValidPipelineStages = new List<string> { "Analysis", "Planning", "Stories", "Implementation", "Review" }
            };
            var mockTestSettings = new Mock<IOptions<ReviewSettings>>();
            mockTestSettings.Setup(s => s.Value).Returns(testSettings);

            var service = new ReviewService(_mockLogger.Object, mockTestSettings.Object, _mockServiceProvider.Object);
            
            var request = new SubmitReviewRequest
            {
                ServiceName = "TestService",
                Content = "Test content",
                CorrelationId = "test-correlation-id",
                PipelineStage = "Analysis"
            };

            var submitResponse = await service.SubmitForReviewAsync(request);

            // Act
            var expiredCount = await service.CleanupExpiredReviewsAsync();

            // Assert
            Assert.Equal(1, expiredCount);

            // Verify the review is now expired
            var review = await service.GetReviewAsync(submitResponse.ReviewId);
            Assert.NotNull(review);
            Assert.Equal(ReviewStatus.Expired, review.Status);
        }
    }
}
