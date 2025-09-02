using System;
using System.Collections.Generic;
using AIProjectOrchestrator.Domain.Models.AI;
using AIProjectOrchestrator.Domain.Models.Review;
using Xunit;

namespace AIProjectOrchestrator.UnitTests.Review
{
    public class ReviewSubmissionTests
    {
        [Fact]
        public void ReviewSubmission_InitializesWithDefaultValues()
        {
            // Act
            var review = new ReviewSubmission();

            // Assert
            Assert.NotEqual(Guid.Empty, review.Id);
            Assert.Equal(string.Empty, review.ServiceName);
            Assert.Equal(string.Empty, review.Content);
            Assert.Equal(string.Empty, review.CorrelationId);
            Assert.Equal(string.Empty, review.PipelineStage);
            Assert.Equal(ReviewStatus.Pending, review.Status);
            Assert.True(review.SubmittedAt <= DateTime.UtcNow);
            Assert.Null(review.ReviewedAt);
            Assert.Null(review.OriginalRequest);
            Assert.Null(review.AIResponse);
            Assert.Null(review.Decision);
            Assert.NotNull(review.Metadata);
            Assert.Empty(review.Metadata);
        }

        [Fact]
        public void ReviewDecision_InitializesWithDefaultValues()
        {
            // Act
            var decision = new ReviewDecision();

            // Assert
            Assert.Equal(ReviewStatus.Pending, decision.Status);
            Assert.Equal(string.Empty, decision.Reason);
            Assert.Equal(string.Empty, decision.Feedback);
            Assert.True(decision.DecidedAt <= DateTime.UtcNow);
            Assert.NotNull(decision.InstructionImprovements);
            Assert.Empty(decision.InstructionImprovements);
        }

        [Fact]
        public void ReviewStatus_Enum_HasCorrectValues()
        {
            // Assert
            Assert.Equal(0, (int)ReviewStatus.Pending);
            Assert.Equal(1, (int)ReviewStatus.Approved);
            Assert.Equal(2, (int)ReviewStatus.Rejected);
            Assert.Equal(3, (int)ReviewStatus.Expired);
        }

        [Fact]
        public void ReviewSubmission_CanBeInitializedWithValues()
        {
            // Arrange
            var id = Guid.NewGuid();
            var serviceName = "TestService";
            var content = "Test content";
            var correlationId = "test-correlation-id";
            var pipelineStage = "Analysis";
            var status = ReviewStatus.Approved;
            var submittedAt = DateTime.UtcNow.AddMinutes(-10);
            var reviewedAt = DateTime.UtcNow;
            var originalRequest = new AIRequest { Prompt = "Test prompt" };
            var aiResponse = new AIResponse { Content = "Test response" };
            var decision = new ReviewDecision { Status = ReviewStatus.Approved, Reason = "Test reason" };
            var metadata = new Dictionary<string, object> { { "key", "value" } };

            // Act
            var review = new ReviewSubmission
            {
                Id = id,
                ServiceName = serviceName,
                Content = content,
                CorrelationId = correlationId,
                PipelineStage = pipelineStage,
                Status = status,
                SubmittedAt = submittedAt,
                ReviewedAt = reviewedAt,
                OriginalRequest = originalRequest,
                AIResponse = aiResponse,
                Decision = decision,
                Metadata = metadata
            };

            // Assert
            Assert.Equal(id, review.Id);
            Assert.Equal(serviceName, review.ServiceName);
            Assert.Equal(content, review.Content);
            Assert.Equal(correlationId, review.CorrelationId);
            Assert.Equal(pipelineStage, review.PipelineStage);
            Assert.Equal(status, review.Status);
            Assert.Equal(submittedAt, review.SubmittedAt);
            Assert.Equal(reviewedAt, review.ReviewedAt);
            Assert.Equal(originalRequest, review.OriginalRequest);
            Assert.Equal(aiResponse, review.AIResponse);
            Assert.Equal(decision, review.Decision);
            Assert.Equal(metadata, review.Metadata);
        }
    }
}