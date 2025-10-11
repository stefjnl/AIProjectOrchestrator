using System.ComponentModel.DataAnnotations;
using AIProjectOrchestrator.Domain.Models;
using AIProjectOrchestrator.UnitTests.Domain.Validation;
using FluentAssertions;
using Xunit;

namespace AIProjectOrchestrator.UnitTests.Domain.Validation
{
    public class RequirementsAnalysisRequestValidationTests
    {
        [Fact]
        public void RequirementsAnalysisRequest_ValidObject_PassesValidation()
        {
            // Arrange
            var request = new RequirementsAnalysisRequest
            {
                ProjectDescription = "This is a valid project description with at least 10 characters"
            };

            // Act
            var isValid = ValidationHelper.IsValid(request);

            // Assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void RequirementsAnalysisRequest_EmptyProjectDescription_FailsValidation()
        {
            // Arrange
            var request = new RequirementsAnalysisRequest
            {
                ProjectDescription = string.Empty
            };

            // Act
            var validationResults = ValidationHelper.ValidateObject(request);

            // Assert
            validationResults.Should().NotBeEmpty();
            validationResults.Should().Contain(vr => vr.ErrorMessage != null && vr.ErrorMessage.Contains("The ProjectDescription field is required"));
        }

        [Fact]
        public void RequirementsAnalysisRequest_NullProjectDescription_FailsValidation()
        {
            // Arrange
            var request = new RequirementsAnalysisRequest();

            // Act
            var validationResults = ValidationHelper.ValidateObject(request);

            // Assert
            validationResults.Should().NotBeEmpty();
            validationResults.Should().Contain(vr => vr.ErrorMessage != null && vr.ErrorMessage.Contains("The ProjectDescription field is required"));
        }

        [Fact]
        public void RequirementsAnalysisRequest_ShortProjectDescription_FailsValidation()
        {
            // Arrange
            var request = new RequirementsAnalysisRequest
            {
                ProjectDescription = "Short" // Less than 10 characters
            };

            // Act
            var validationResults = ValidationHelper.ValidateObject(request);

            // Assert
            validationResults.Should().NotBeEmpty();
            validationResults.Should().Contain(vr => vr.ErrorMessage != null && vr.ErrorMessage.Contains("Project description must be at least 10 characters long"));
        }

        [Fact]
        public void RequirementsAnalysisRequest_ProjectDescriptionWith10Chars_PassesValidation()
        {
            // Arrange
            var request = new RequirementsAnalysisRequest
            {
                ProjectDescription = "1234567890" // Exactly 10 characters
            };

            // Act
            var isValid = ValidationHelper.IsValid(request);

            // Assert
            isValid.Should().BeTrue();
        }
    }
}