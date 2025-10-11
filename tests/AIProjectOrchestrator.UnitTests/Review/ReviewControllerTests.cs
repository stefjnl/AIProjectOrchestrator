using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Moq;
using AIProjectOrchestrator.Domain.Models.Review;
using AIProjectOrchestrator.Domain.Services;
using AIProjectOrchestrator.Application.Interfaces;
using AIProjectOrchestrator.API.Controllers;
using Xunit;

namespace AIProjectOrchestrator.UnitTests.Review
{
    public class ReviewControllerTests
    {
        private readonly Mock<IReviewService> _mockReviewService;
        private readonly Mock<IRequirementsAnalysisService> _mockRequirementsAnalysisService;
        private readonly Mock<IProjectPlanningService> _mockProjectPlanningService;
        private readonly Mock<IStoryGenerationService> _mockStoryGenerationService;
        private readonly Mock<IPromptGenerationService> _mockPromptGenerationService;
        private readonly Mock<IProjectService> _mockProjectService;
        private readonly Mock<ILogger<ReviewController>> _mockLogger;
        private readonly ReviewController _controller;

        public ReviewControllerTests()
        {
            _mockReviewService = new Mock<IReviewService>();
            _mockRequirementsAnalysisService = new Mock<IRequirementsAnalysisService>();
            _mockProjectPlanningService = new Mock<IProjectPlanningService>();
            _mockStoryGenerationService = new Mock<IStoryGenerationService>();
            _mockPromptGenerationService = new Mock<IPromptGenerationService>();
            _mockLogger = new Mock<ILogger<ReviewController>>();
            _mockProjectService = new Mock<IProjectService>();
            _controller = new ReviewController(
                _mockReviewService.Object,
                _mockRequirementsAnalysisService.Object,
                _mockProjectPlanningService.Object,
                _mockStoryGenerationService.Object,
                _mockPromptGenerationService.Object,
                _mockProjectService.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task SubmitReview_WithValidRequest_ReturnsCreatedAtAction()
        {
            // Arrange
            var request = new SubmitReviewRequest
            {
                ServiceName = "TestService",
                Content = "Test content",
                CorrelationId = "test-correlation-id",
                PipelineStage = "Analysis"
            };

            var response = new ReviewResponse
            {
                ReviewId = Guid.NewGuid(),
                Status = ReviewStatus.Pending,
                SubmittedAt = DateTime.UtcNow,
                Message = "Review submitted successfully"
            };

            _mockReviewService.Setup(s => s.SubmitForReviewAsync(request, CancellationToken.None))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.SubmitReview(request, CancellationToken.None);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal("GetReview", createdAtActionResult.ActionName);
            Assert.NotNull(createdAtActionResult.RouteValues);
            Assert.Equal(response.ReviewId, createdAtActionResult.RouteValues["id"]);
            var returnValue = Assert.IsType<ReviewResponse>(createdAtActionResult.Value);
            Assert.Equal(response.ReviewId, returnValue.ReviewId);
        }

        [Fact]
        public async Task SubmitReview_WithInvalidModel_ReturnsBadRequest()
        {
            // Arrange
            var request = new SubmitReviewRequest(); // Invalid because required fields are missing
            
            // Add model state errors to simulate validation failure
            _controller.ModelState.AddModelError("ServiceName", "Service name is required");

            // Act
            var result = await _controller.SubmitReview(request, CancellationToken.None);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.IsType<SerializableError>(badRequestResult.Value);
        }

        [Fact]
        public async Task SubmitReview_WithArgumentException_ReturnsBadRequest()
        {
            // Arrange
            var request = new SubmitReviewRequest
            {
                ServiceName = "TestService",
                Content = "Test content",
                CorrelationId = "test-correlation-id",
                PipelineStage = "Analysis"
            };

            _mockReviewService.Setup(s => s.SubmitForReviewAsync(request, CancellationToken.None))
                .ThrowsAsync(new ArgumentException("Invalid argument"));

            // Act
            var result = await _controller.SubmitReview(request, CancellationToken.None);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
            Assert.Equal("Invalid request", problemDetails.Title);
        }

        [Fact]
        public async Task SubmitReview_WithInvalidOperationException_ReturnsServiceUnavailable()
        {
            // Arrange
            var request = new SubmitReviewRequest
            {
                ServiceName = "TestService",
                Content = "Test content",
                CorrelationId = "test-correlation-id",
                PipelineStage = "Analysis"
            };

            _mockReviewService.Setup(s => s.SubmitForReviewAsync(request, CancellationToken.None))
                .ThrowsAsync(new InvalidOperationException("Service unavailable"));

            // Act
            var result = await _controller.SubmitReview(request, CancellationToken.None);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status503ServiceUnavailable, objectResult.StatusCode);
            var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
            Assert.Equal("Service unavailable", problemDetails.Title);
        }

        [Fact]
        public async Task GetReview_WithExistingReview_ReturnsOk()
        {
            // Arrange
            var reviewId = Guid.NewGuid();
            var review = new ReviewSubmission
            {
                Id = reviewId,
                ServiceName = "TestService",
                Content = "Test content",
                CorrelationId = "test-correlation-id",
                PipelineStage = "Analysis",
                Status = ReviewStatus.Pending
            };

            _mockReviewService.Setup(s => s.GetReviewAsync(reviewId, CancellationToken.None))
                .ReturnsAsync(review);

            // Act
            var result = await _controller.GetReview(reviewId, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<ReviewSubmission>(okResult.Value);
            Assert.Equal(reviewId, returnValue.Id);
        }

        [Fact]
        public async Task GetReview_WithNonExistentReview_ReturnsNotFound()
        {
            // Arrange
            var reviewId = Guid.NewGuid();

            _mockReviewService.Setup(s => s.GetReviewAsync(reviewId, CancellationToken.None))
                .ReturnsAsync((ReviewSubmission)null!);

            // Act
            var result = await _controller.GetReview(reviewId, CancellationToken.None);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
            Assert.Equal("Review not found", problemDetails.Title);
        }

        [Fact]
        public async Task ApproveReview_WithValidRequest_ReturnsOk()
        {
            // Arrange
            var reviewId = Guid.NewGuid();
            var correlationId = reviewId; // Assuming correlationId is the same as reviewId for simplicity in test
            var reviewSubmission = new ReviewSubmission
            {
                Id = reviewId,
                ServiceName = "RequirementsAnalysis", // This needs to match a case in the controller's switch
                Content = "Test content",
                CorrelationId = correlationId.ToString(),
                PipelineStage = "Analysis",
                Status = ReviewStatus.Pending
            };

            var reviewResponse = new ReviewResponse
            {
                ReviewId = reviewId,
                Status = ReviewStatus.Approved,
                SubmittedAt = DateTime.UtcNow,
                Message = "Review approved successfully"
            };

            _mockReviewService.Setup(s => s.GetReviewAsync(reviewId, CancellationToken.None))
                .ReturnsAsync(reviewSubmission);
            _mockReviewService.Setup(s => s.ApproveReviewAsync(reviewId, It.IsAny<ReviewDecisionRequest>(), CancellationToken.None))
                .ReturnsAsync(reviewResponse);

            // Act
            var result = await _controller.ApproveReview(reviewId, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<ReviewResponse>(okResult.Value);
            Assert.Equal(reviewId, returnValue.ReviewId);
            Assert.Equal(ReviewStatus.Approved, returnValue.Status);

            // Verify that the correct service's UpdateStatusAsync method was called
            _mockRequirementsAnalysisService.Verify(
                s => s.UpdateAnalysisStatusAsync(
                    correlationId, 
                    AIProjectOrchestrator.Domain.Models.RequirementsAnalysisStatus.Approved, 
                    It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        [Fact]
        public async Task ApproveReview_WithNonExistentReview_ReturnsNotFound()
        {
            // Arrange
            var reviewId = Guid.NewGuid();

            _mockReviewService.Setup(s => s.ApproveReviewAsync(reviewId, It.IsAny<ReviewDecisionRequest>(), CancellationToken.None))
                .ThrowsAsync(new InvalidOperationException("Review not found"));

            // Act
            var result = await _controller.ApproveReview(reviewId, CancellationToken.None);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
            Assert.Equal("Review not found", problemDetails.Title);
        }

        [Fact]
        public async Task RejectReview_WithValidRequest_ReturnsOk()
        {
            // Arrange
            var reviewId = Guid.NewGuid();
            var decisionRequest = new ReviewDecisionRequest
            {
                Reason = "Needs improvement"
            };

            var response = new ReviewResponse
            {
                ReviewId = reviewId,
                Status = ReviewStatus.Rejected,
                SubmittedAt = DateTime.UtcNow,
                Message = "Review rejected successfully"
            };

            _mockReviewService.Setup(s => s.RejectReviewAsync(reviewId, decisionRequest, CancellationToken.None))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.RejectReview(reviewId, decisionRequest, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<ReviewResponse>(okResult.Value);
            Assert.Equal(reviewId, returnValue.ReviewId);
            Assert.Equal(ReviewStatus.Rejected, returnValue.Status);
        }

        [Fact]
        public async Task RejectReview_WithInvalidModel_ReturnsBadRequest()
        {
            // Arrange
            var reviewId = Guid.NewGuid();
            var decisionRequest = new ReviewDecisionRequest(); // Invalid because reason is missing

            // Add model state errors to simulate validation failure
            _controller.ModelState.AddModelError("Reason", "Reason is required");

            // Act
            var result = await _controller.RejectReview(reviewId, decisionRequest, CancellationToken.None);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.IsType<SerializableError>(badRequestResult.Value);
        }

        [Fact]
        public async Task RejectReview_WithArgumentException_ReturnsBadRequest()
        {
            // Arrange
            var reviewId = Guid.NewGuid();
            var decisionRequest = new ReviewDecisionRequest
            {
                Reason = "Test reason"
            };

            _mockReviewService.Setup(s => s.RejectReviewAsync(reviewId, decisionRequest, CancellationToken.None))
                .ThrowsAsync(new ArgumentException("Invalid argument"));

            // Act
            var result = await _controller.RejectReview(reviewId, decisionRequest, CancellationToken.None);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
            Assert.Equal("Invalid request", problemDetails.Title);
        }

        [Fact]
        public async Task GetPendingReviews_ReturnsOk()
        {
            // Arrange
            var reviews = new List<ReviewSubmission>
            {
                new ReviewSubmission
                {
                    Id = Guid.NewGuid(),
                    ServiceName = "Service1",
                    Content = "Content1",
                    CorrelationId = "correlation1",
                    PipelineStage = "Analysis",
                    Status = ReviewStatus.Pending
                },
                new ReviewSubmission
                {
                    Id = Guid.NewGuid(),
                    ServiceName = "Service2",
                    Content = "Content2",
                    CorrelationId = "correlation2",
                    PipelineStage = "Planning",
                    Status = ReviewStatus.Pending
                }
            };

            _mockReviewService.Setup(s => s.GetPendingReviewsAsync(CancellationToken.None))
                .ReturnsAsync(reviews);

            // Act
            var result = await _controller.GetPendingReviews(CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<List<ReviewSubmission>>(okResult.Value);
            Assert.Equal(2, returnValue.Count);
        }

        [Fact]
        public async Task ApproveReview_NotifiesCorrectServiceForRequirementsAnalysis()
        {
            // Arrange
            var reviewId = Guid.NewGuid();
            var correlationId = Guid.NewGuid();
            var reviewSubmission = new ReviewSubmission
            {
                Id = reviewId,
                ServiceName = "RequirementsAnalysis",
                Content = "Test content",
                CorrelationId = correlationId.ToString(),
                PipelineStage = "Analysis",
                Status = ReviewStatus.Pending
            };

            var reviewResponse = new ReviewResponse
            {
                ReviewId = reviewId,
                Status = ReviewStatus.Approved,
                SubmittedAt = DateTime.UtcNow,
                Message = "Review approved successfully"
            };

            _mockReviewService.Setup(s => s.GetReviewAsync(reviewId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(reviewSubmission);
            _mockReviewService.Setup(s => s.ApproveReviewAsync(reviewId, It.IsAny<ReviewDecisionRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(reviewResponse);

            // Act
            var result = await _controller.ApproveReview(reviewId, CancellationToken.None);

            // Assert
            _mockRequirementsAnalysisService.Verify(
                s => s.UpdateAnalysisStatusAsync(
                    correlationId,
                    AIProjectOrchestrator.Domain.Models.RequirementsAnalysisStatus.Approved,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _mockProjectPlanningService.Verify(
                s => s.UpdatePlanningStatusAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<AIProjectOrchestrator.Domain.Models.ProjectPlanningStatus>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            _mockStoryGenerationService.Verify(
                s => s.UpdateGenerationStatusAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<AIProjectOrchestrator.Domain.Models.Stories.StoryGenerationStatus>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            _mockProjectPlanningService.Verify(
                s => s.UpdatePlanningStatusAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<AIProjectOrchestrator.Domain.Models.ProjectPlanningStatus>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _mockStoryGenerationService.Verify(
                s => s.UpdateGenerationStatusAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<AIProjectOrchestrator.Domain.Models.Stories.StoryGenerationStatus>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ApproveReview_NotifiesCorrectServiceForStoryGeneration()
        {
            // Arrange
            var reviewId = Guid.NewGuid();
            var correlationId = Guid.NewGuid();
            var reviewSubmission = new ReviewSubmission
            {
                Id = reviewId,
                ServiceName = "StoryGeneration",
                Content = "Test content",
                CorrelationId = correlationId.ToString(),
                PipelineStage = "StoryGeneration",
                Status = ReviewStatus.Pending
            };

            var reviewResponse = new ReviewResponse
            {
                ReviewId = reviewId,
                Status = ReviewStatus.Approved,
                SubmittedAt = DateTime.UtcNow,
                Message = "Review approved successfully"
            };

            _mockReviewService.Setup(s => s.GetReviewAsync(reviewId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(reviewSubmission);
            _mockReviewService.Setup(s => s.ApproveReviewAsync(reviewId, It.IsAny<ReviewDecisionRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(reviewResponse);

            // Act
            var result = await _controller.ApproveReview(reviewId, CancellationToken.None);

            // Assert
            _mockRequirementsAnalysisService.Verify(
                s => s.UpdateAnalysisStatusAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<AIProjectOrchestrator.Domain.Models.RequirementsAnalysisStatus>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            _mockProjectPlanningService.Verify(
                s => s.UpdatePlanningStatusAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<AIProjectOrchestrator.Domain.Models.ProjectPlanningStatus>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            _mockStoryGenerationService.Verify(
                s => s.UpdateGenerationStatusAsync(
                    correlationId,
                    AIProjectOrchestrator.Domain.Models.Stories.StoryGenerationStatus.Approved,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
