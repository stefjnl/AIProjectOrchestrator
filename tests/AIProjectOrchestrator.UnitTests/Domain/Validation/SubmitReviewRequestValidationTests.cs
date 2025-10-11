using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AIProjectOrchestrator.Domain.Models.Review;
using AIProjectOrchestrator.Domain.Models.AI;
using AIProjectOrchestrator.UnitTests.Domain.Validation;
using FluentAssertions;
using Xunit;

namespace AIProjectOrchestrator.UnitTests.Domain.Validation
{
    public class SubmitReviewRequestValidationTests
    {
        [Fact]
        public void SubmitReviewRequest_ValidObject_PassesValidation()
        {
            // Arrange
            var request = new SubmitReviewRequest
            {
                ServiceName = "TestService",
                Content = "Test content",
                CorrelationId = "TestCorrelationId",
                PipelineStage = "TestStage"
            };

            // Act
            var isValid = ValidationHelper.IsValid(request);

            // Assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void SubmitReviewRequest_MissingServiceName_FailsValidation()
        {
            // Arrange
            var request = new SubmitReviewRequest
            {
                ServiceName = string.Empty,
                Content = "Test content",
                CorrelationId = "TestCorrelationId",
                PipelineStage = "TestStage"
            };

            // Act
            var validationResults = ValidationHelper.ValidateObject(request);

            // Assert
            validationResults.Should().NotBeEmpty();
            validationResults.Should().Contain(vr => vr.ErrorMessage != null && vr.ErrorMessage.Contains("The ServiceName field is required"));
        }

        [Fact]
        public void SubmitReviewRequest_ServiceNameTooLong_FailsValidation()
        {
            // Arrange
            var request = new SubmitReviewRequest
            {
                ServiceName = new string('A', 101), // More than 100 characters
                Content = "Test content",
                CorrelationId = "TestCorrelationId",
                PipelineStage = "TestStage"
            };

            // Act
            var validationResults = ValidationHelper.ValidateObject(request);

            // Assert
            validationResults.Should().NotBeEmpty();
            validationResults.Should().Contain(vr => vr.ErrorMessage != null && vr.ErrorMessage.Contains("The field ServiceName must be a string or array type with a maximum length of '100'"));
        }

        [Fact]
        public void SubmitReviewRequest_MissingContent_FailsValidation()
        {
            // Arrange
            var request = new SubmitReviewRequest
            {
                ServiceName = "TestService",
                Content = string.Empty,
                CorrelationId = "TestCorrelationId",
                PipelineStage = "TestStage"
            };

            // Act
            var validationResults = ValidationHelper.ValidateObject(request);

            // Assert
            validationResults.Should().NotBeEmpty();
            validationResults.Should().Contain(vr => vr.ErrorMessage != null && vr.ErrorMessage.Contains("The Content field is required"));
        }

        [Fact]
        public void SubmitReviewRequest_ContentTooLong_FailsValidation()
        {
            // Arrange
            var request = new SubmitReviewRequest
            {
                ServiceName = "TestService",
                Content = new string('A', 50001), // More than 50000 characters
                CorrelationId = "TestCorrelationId",
                PipelineStage = "TestStage"
            };

            // Act
            var validationResults = ValidationHelper.ValidateObject(request);

            // Assert
            validationResults.Should().NotBeEmpty();
            validationResults.Should().Contain(vr => vr.ErrorMessage != null && vr.ErrorMessage.Contains("The field Content must be a string or array type with a maximum length of '50000'"));
        }

        [Fact]
        public void SubmitReviewRequest_MissingCorrelationId_FailsValidation()
        {
            // Arrange
            var request = new SubmitReviewRequest
            {
                ServiceName = "TestService",
                Content = "Test content",
                CorrelationId = string.Empty,
                PipelineStage = "TestStage"
            };

            // Act
            var validationResults = ValidationHelper.ValidateObject(request);

            // Assert
            validationResults.Should().NotBeEmpty();
            validationResults.Should().Contain(vr => vr.ErrorMessage != null && vr.ErrorMessage.Contains("The CorrelationId field is required"));
        }

        [Fact]
        public void SubmitReviewRequest_MissingPipelineStage_FailsValidation()
        {
            // Arrange
            var request = new SubmitReviewRequest
            {
                ServiceName = "TestService",
                Content = "Test content",
                CorrelationId = "TestCorrelationId",
                PipelineStage = string.Empty
            };

            // Act
            var validationResults = ValidationHelper.ValidateObject(request);

            // Assert
            validationResults.Should().NotBeEmpty();
            validationResults.Should().Contain(vr => vr.ErrorMessage != null && vr.ErrorMessage.Contains("The PipelineStage field is required"));
        }

        [Fact]
        public void SubmitReviewRequest_PipelineStageTooLong_FailsValidation()
        {
            // Arrange
            var request = new SubmitReviewRequest
            {
                ServiceName = "TestService",
                Content = "Test content",
                CorrelationId = "TestCorrelationId",
                PipelineStage = new string('A', 51) // More than 50 characters
            };

            // Act
            var validationResults = ValidationHelper.ValidateObject(request);

            // Assert
            validationResults.Should().NotBeEmpty();
            validationResults.Should().Contain(vr => vr.ErrorMessage != null && vr.ErrorMessage.Contains("The field PipelineStage must be a string or array type with a maximum length of '50'"));
        }
    }
}