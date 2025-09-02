using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using AIProjectOrchestrator.Domain.Models.Review;
using AIProjectOrchestrator.IntegrationTests;

namespace AIProjectOrchestrator.IntegrationTests.Review
{
    [Collection("Sequential")]
    public class ReviewWorkflowIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public ReviewWorkflowIntegrationTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task ReviewWorkflow_SubmitApproveReject_FullCycle()
        {
            // 1. Submit a review
            var submitRequest = new SubmitReviewRequest
            {
                ServiceName = "TestService",
                Content = "This is test content for review",
                CorrelationId = "test-correlation-id-123",
                PipelineStage = "Analysis"
            };

            var submitResponse = await _client.PostAsJsonAsync("/api/review/submit", submitRequest);
            submitResponse.EnsureSuccessStatusCode();

            var reviewResponse = await submitResponse.Content.ReadFromJsonAsync<ReviewResponse>();
            Assert.NotNull(reviewResponse);
            Assert.NotEqual(Guid.Empty, reviewResponse.ReviewId);
            Assert.Equal(ReviewStatus.Pending, reviewResponse.Status);

            // 2. Get the review
            var getResponse = await _client.GetAsync($"/api/review/{reviewResponse.ReviewId}");
            getResponse.EnsureSuccessStatusCode();

            var reviewSubmission = await getResponse.Content.ReadFromJsonAsync<ReviewSubmission>();
            Assert.NotNull(reviewSubmission);
            Assert.Equal(reviewResponse.ReviewId, reviewSubmission.Id);
            Assert.Equal("TestService", reviewSubmission.ServiceName);
            Assert.Equal("This is test content for review", reviewSubmission.Content);
            Assert.Equal("test-correlation-id-123", reviewSubmission.CorrelationId);
            Assert.Equal("Analysis", reviewSubmission.PipelineStage);
            Assert.Equal(ReviewStatus.Pending, reviewSubmission.Status);

            // 3. Approve the review
            var approveRequest = new ReviewDecisionRequest
            {
                Reason = "Content looks good",
                Feedback = "No issues found"
            };

            var approveResponse = await _client.PostAsJsonAsync($"/api/review/{reviewResponse.ReviewId}/approve", approveRequest);
            approveResponse.EnsureSuccessStatusCode();

            var approveResult = await approveResponse.Content.ReadFromJsonAsync<ReviewResponse>();
            Assert.NotNull(approveResult);
            Assert.Equal(reviewResponse.ReviewId, approveResult.ReviewId);
            Assert.Equal(ReviewStatus.Approved, approveResult.Status);

            // 4. Verify review is approved
            var getResponseAfterApprove = await _client.GetAsync($"/api/review/{reviewResponse.ReviewId}");
            getResponseAfterApprove.EnsureSuccessStatusCode();

            var reviewSubmissionAfterApprove = await getResponseAfterApprove.Content.ReadFromJsonAsync<ReviewSubmission>();
            Assert.NotNull(reviewSubmissionAfterApprove);
            Assert.Equal(ReviewStatus.Approved, reviewSubmissionAfterApprove.Status);
            Assert.NotNull(reviewSubmissionAfterApprove.Decision);
            Assert.Equal(ReviewStatus.Approved, reviewSubmissionAfterApprove.Decision.Status);
            Assert.Equal("Content looks good", reviewSubmissionAfterApprove.Decision.Reason);

            // 5. Submit another review for rejection
            var submitRequest2 = new SubmitReviewRequest
            {
                ServiceName = "TestService2",
                Content = "This is another test content for review",
                CorrelationId = "test-correlation-id-456",
                PipelineStage = "Planning"
            };

            var submitResponse2 = await _client.PostAsJsonAsync("/api/review/submit", submitRequest2);
            submitResponse2.EnsureSuccessStatusCode();

            var reviewResponse2 = await submitResponse2.Content.ReadFromJsonAsync<ReviewResponse>();
            Assert.NotNull(reviewResponse2);

            // 6. Reject the review
            var rejectRequest = new ReviewDecisionRequest
            {
                Reason = "Content needs improvement",
                Feedback = "Please fix the formatting issues"
            };

            var rejectResponse = await _client.PostAsJsonAsync($"/api/review/{reviewResponse2.ReviewId}/reject", rejectRequest);
            rejectResponse.EnsureSuccessStatusCode();

            var rejectResult = await rejectResponse.Content.ReadFromJsonAsync<ReviewResponse>();
            Assert.NotNull(rejectResult);
            Assert.Equal(reviewResponse2.ReviewId, rejectResult.ReviewId);
            Assert.Equal(ReviewStatus.Rejected, rejectResult.Status);

            // 7. Verify review is rejected
            var getResponseAfterReject = await _client.GetAsync($"/api/review/{reviewResponse2.ReviewId}");
            getResponseAfterReject.EnsureSuccessStatusCode();

            var reviewSubmissionAfterReject = await getResponseAfterReject.Content.ReadFromJsonAsync<ReviewSubmission>();
            Assert.NotNull(reviewSubmissionAfterReject);
            Assert.Equal(ReviewStatus.Rejected, reviewSubmissionAfterReject.Status);
            Assert.NotNull(reviewSubmissionAfterReject.Decision);
            Assert.Equal(ReviewStatus.Rejected, reviewSubmissionAfterReject.Decision.Status);
            Assert.Equal("Content needs improvement", reviewSubmissionAfterReject.Decision.Reason);
        }

        [Fact]
        public async Task SubmitReview_WithInvalidData_ReturnsBadRequest()
        {
            // Arrange
            var submitRequest = new SubmitReviewRequest
            {
                // Missing required fields
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/review/submit", submitRequest);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetReview_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/review/{nonExistentId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetPendingReviews_ReturnsPendingReviews()
        {
            // Arrange - Submit a few reviews
            var submitRequest1 = new SubmitReviewRequest
            {
                ServiceName = "Service1",
                Content = "Content 1",
                CorrelationId = "corr-1",
                PipelineStage = "Analysis"
            };

            var submitRequest2 = new SubmitReviewRequest
            {
                ServiceName = "Service2",
                Content = "Content 2",
                CorrelationId = "corr-2",
                PipelineStage = "Planning"
            };

            await _client.PostAsJsonAsync("/api/review/submit", submitRequest1);
            await _client.PostAsJsonAsync("/api/review/submit", submitRequest2);

            // Act
            var response = await _client.GetAsync("/api/review/pending");
            response.EnsureSuccessStatusCode();

            var pendingReviews = await response.Content.ReadFromJsonAsync<ReviewSubmission[]>();
            Assert.NotNull(pendingReviews);
            Assert.True(pendingReviews.Length >= 2); // At least the two we just added
        }
    }
}