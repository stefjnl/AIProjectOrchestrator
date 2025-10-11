using System;
using AIProjectOrchestrator.Domain.Models;
using AIProjectOrchestrator.UnitTests.Domain.Validation;
using FluentAssertions;
using Xunit;

namespace AIProjectOrchestrator.UnitTests.Domain.Validation
{
    public class InstructionContentValidationTests
    {
        [Fact]
        public void InstructionContent_ValidObject_PassesValidation()
        {
            // Arrange
            var content = new InstructionContent
            {
                ServiceName = "TestService",
                Content = "Test content",
                LastModified = DateTime.UtcNow,
                IsValid = true,
                ValidationMessage = "Validation message"
            };

            // Act
            var isValid = ValidationHelper.IsValid(content);

            // Assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void InstructionContent_WithEmptyValues_PassesValidation()
        {
            // Arrange
            var content = new InstructionContent
            {
                ServiceName = string.Empty,
                Content = string.Empty,
                LastModified = DateTime.MinValue,
                IsValid = false,
                ValidationMessage = string.Empty
            };

            // Act
            var isValid = ValidationHelper.IsValid(content);

            // Assert
            isValid.Should().BeTrue(); // No validation attributes means empty values are allowed
        }
    }
}