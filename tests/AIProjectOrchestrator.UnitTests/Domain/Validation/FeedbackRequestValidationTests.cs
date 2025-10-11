using AIProjectOrchestrator.Domain.Models;
using AIProjectOrchestrator.UnitTests.Domain.Validation;
using FluentAssertions;
using Xunit;

namespace AIProjectOrchestrator.UnitTests.Domain.Validation
{
    public class FeedbackRequestValidationTests
    {
        [Fact]
        public void FeedbackRequest_ValidObject_PassesValidation()
        {
            // Arrange
            var request = new FeedbackRequest
            {
                Feedback = "This is a test feedback"
            };

            // Act
            var isValid = ValidationHelper.IsValid(request);

            // Assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void FeedbackRequest_WithEmptyFeedback_PassesValidation()
        {
            // Arrange
            var request = new FeedbackRequest
            {
                Feedback = string.Empty
            };

            // Act
            var isValid = ValidationHelper.IsValid(request);

            // Assert
            isValid.Should().BeTrue(); // No validation attributes means empty is allowed
        }

        [Fact]
        public void FeedbackRequest_WithNullFeedback_PassesValidation()
        {
            // Arrange
            var request = new FeedbackRequest();

            // Act
            var isValid = ValidationHelper.IsValid(request);

            // Assert
            isValid.Should().BeTrue(); // No validation attributes means null is allowed
        }
    }
}