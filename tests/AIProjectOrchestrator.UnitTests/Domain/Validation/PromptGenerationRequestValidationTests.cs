using System;
using System.ComponentModel.DataAnnotations;
using AIProjectOrchestrator.Domain.Models.PromptGeneration;
using AIProjectOrchestrator.UnitTests.Domain.Validation;
using FluentAssertions;
using Xunit;

namespace AIProjectOrchestrator.UnitTests.Domain.Validation
{
    public class PromptGenerationRequestValidationTests
    {
        [Fact]
        public void PromptGenerationRequest_ValidObject_PassesValidation()
        {
            // Arrange
            var request = new PromptGenerationRequest
            {
                StoryGenerationId = Guid.NewGuid(),
                StoryIndex = 0
            };

            // Act
            var isValid = ValidationHelper.IsValid(request);

            // Assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void PromptGenerationRequest_EmptyStoryGenerationId_PassesValidation()
        {
            // Arrange
            var request = new PromptGenerationRequest
            {
                StoryGenerationId = Guid.Empty,
                StoryIndex = 0
            };

            // Act
            var isValid = ValidationHelper.IsValid(request);

            // Assert
            isValid.Should().BeTrue(); // Guid.Empty is a valid value for Guid type, Required attribute doesn't apply
        }

        [Fact]
        public void PromptGenerationRequest_NegativeStoryIndex_FailsValidation()
        {
            // Arrange
            var request = new PromptGenerationRequest
            {
                StoryGenerationId = Guid.NewGuid(),
                StoryIndex = -1
            };

            // Act
            var validationResults = ValidationHelper.ValidateObject(request);

            // Assert
            validationResults.Should().NotBeEmpty();
            validationResults.Should().Contain(vr => vr.ErrorMessage != null && vr.ErrorMessage.Contains("Story index must be a non-negative integer"));
        }

        [Fact]
        public void PromptGenerationRequest_ZeroStoryIndex_PassesValidation()
        {
            // Arrange
            var request = new PromptGenerationRequest
            {
                StoryGenerationId = Guid.NewGuid(),
                StoryIndex = 0
            };

            // Act
            var isValid = ValidationHelper.IsValid(request);

            // Assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void PromptGenerationRequest_WithDefaultValues_PassesValidation()
        {
            // Arrange
            var request = new PromptGenerationRequest(); // All properties will have their default values

            // Act
            var isValid = ValidationHelper.IsValid(request);

            // Assert
            isValid.Should().BeTrue(); // Default values are valid for this model
        }

        [Fact]
        public void PromptGenerationRequest_MissingStoryIndex_PassesValidation()
        {
            // Arrange
            var request = new PromptGenerationRequest
            {
                StoryGenerationId = Guid.NewGuid()
            };

            // Act
            var isValid = ValidationHelper.IsValid(request);

            // Assert
            isValid.Should().BeTrue(); // StoryIndex is an int with default value 0, which passes validation
        }
    }
}