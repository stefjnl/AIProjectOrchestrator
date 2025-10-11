using System;
using AIProjectOrchestrator.Domain.Models.Code;
using AIProjectOrchestrator.UnitTests.Domain.Validation;
using FluentAssertions;
using Xunit;

namespace AIProjectOrchestrator.UnitTests.Domain.Validation
{
    public class CodeGenerationRequestValidationTests
    {
        [Fact]
        public void CodeGenerationRequest_ValidObject_PassesValidation()
        {
            // Arrange
            var request = new CodeGenerationRequest
            {
                StoryGenerationId = Guid.NewGuid(),
                TechnicalPreferences = "Some preferences",
                TargetFramework = ".NET 9",
                CodeStylePreferences = "Style preferences",
                AdditionalInstructions = "Additional instructions"
            };

            // Act
            var isValid = ValidationHelper.IsValid(request);

            // Assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void CodeGenerationRequest_WithEmptyGuid_PassesValidation()
        {
            // Arrange
            var request = new CodeGenerationRequest
            {
                StoryGenerationId = Guid.Empty, // This is allowed since there's no Required attribute
                TechnicalPreferences = null,
                TargetFramework = null
            };

            // Act
            var isValid = ValidationHelper.IsValid(request);

            // Assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void CodeGenerationRequest_WithNullValues_PassesValidation()
        {
            // Arrange
            var request = new CodeGenerationRequest
            {
                StoryGenerationId = Guid.NewGuid(),
                TechnicalPreferences = null,
                TargetFramework = null,
                CodeStylePreferences = null,
                AdditionalInstructions = null
            };

            // Act
            var isValid = ValidationHelper.IsValid(request);

            // Assert
            isValid.Should().BeTrue();
        }
    }
}