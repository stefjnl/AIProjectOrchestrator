using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AIProjectOrchestrator.Domain.Models.AI;
using AIProjectOrchestrator.Domain.Models.Review;
using Xunit;

namespace AIProjectOrchestrator.UnitTests.Review
{
    public class SubmitReviewRequestTests
    {
        [Fact]
        public void SubmitReviewRequest_InitializesWithDefaultValues()
        {
            // Act
            var request = new SubmitReviewRequest();

            // Assert
            Assert.Equal(string.Empty, request.ServiceName);
            Assert.Equal(string.Empty, request.Content);
            Assert.Equal(string.Empty, request.CorrelationId);
            Assert.Equal(string.Empty, request.PipelineStage);
            Assert.Null(request.OriginalRequest);
            Assert.Null(request.AIResponse);
            Assert.NotNull(request.Metadata);
            Assert.Empty(request.Metadata);
        }

        [Fact]
        public void SubmitReviewRequest_ValidationAttributes_ArePresent()
        {
            // Arrange
            var request = new SubmitReviewRequest();

            // Act
            var context = new ValidationContext(request);
            var results = new List<ValidationResult>();

            // Assert
            var isServiceNameValid = Validator.TryValidateProperty(request.ServiceName, new ValidationContext(request) { MemberName = "ServiceName" }, results);
            Assert.False(isServiceNameValid); // Should fail because it's required

            var isContentValid = Validator.TryValidateProperty(request.Content, new ValidationContext(request) { MemberName = "Content" }, results);
            Assert.False(isContentValid); // Should fail because it's required

            var isCorrelationIdValid = Validator.TryValidateProperty(request.CorrelationId, new ValidationContext(request) { MemberName = "CorrelationId" }, results);
            Assert.False(isCorrelationIdValid); // Should fail because it's required

            var isPipelineStageValid = Validator.TryValidateProperty(request.PipelineStage, new ValidationContext(request) { MemberName = "PipelineStage" }, results);
            Assert.False(isPipelineStageValid); // Should fail because it's required
        }

        [Fact]
        public void SubmitReviewRequest_Validation_PassesWithValidData()
        {
            // Arrange
            var request = new SubmitReviewRequest
            {
                ServiceName = "TestService",
                Content = "Test content that is long enough to pass validation",
                CorrelationId = "test-correlation-id",
                PipelineStage = "Analysis"
            };

            // Act
            var context = new ValidationContext(request);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(request, context, results, true);

            // Assert
            Assert.True(isValid);
            Assert.Empty(results);
        }

        [Fact]
        public void ReviewDecisionRequest_InitializesWithDefaultValues()
        {
            // Act
            var request = new ReviewDecisionRequest();

            // Assert
            Assert.Equal(string.Empty, request.Reason);
            Assert.Equal(string.Empty, request.Feedback);
            Assert.NotNull(request.InstructionImprovements);
            Assert.Empty(request.InstructionImprovements);
        }

        [Fact]
        public void ReviewDecisionRequest_Validation_FailsWithoutReason()
        {
            // Arrange
            var request = new ReviewDecisionRequest();

            // Act
            var context = new ValidationContext(request);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(request, context, results, true);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void ReviewResponse_InitializesWithDefaultValues()
        {
            // Act
            var response = new ReviewResponse();

            // Assert
            Assert.Equal(Guid.Empty, response.ReviewId);
            Assert.Equal(ReviewStatus.Pending, response.Status);
            Assert.Equal(DateTime.MinValue, response.SubmittedAt);
            Assert.Equal(string.Empty, response.Message);
        }
    }
}