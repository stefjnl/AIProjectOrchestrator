using System;
using System.Collections.Generic;
using AIProjectOrchestrator.Domain.Models.Stories;
using AIProjectOrchestrator.UnitTests.Domain.Validation;
using FluentAssertions;
using Xunit;

namespace AIProjectOrchestrator.UnitTests.Domain.Validation
{
    public class UserStoryDtoValidationTests
    {
        [Fact]
        public void UserStoryDto_ValidObject_PassesValidation()
        {
            // Arrange
            var story = new UserStoryDto
            {
                Id = Guid.NewGuid(),
                Index = 1,
                Title = "Test Story",
                Description = "Test Description",
                AcceptanceCriteria = new List<string> { "Criteria 1", "Criteria 2" },
                Priority = "High",
                StoryPoints = 5,
                Tags = new List<string> { "tag1", "tag2" },
                EstimatedComplexity = "Medium",
                Status = StoryStatus.Draft
            };

            // Act
            var isValid = ValidationHelper.IsValid(story);

            // Assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void UserStoryDto_WithEmptyValues_PassesValidation()
        {
            // Arrange
            var story = new UserStoryDto
            {
                Id = Guid.Empty,
                Index = 0,
                Title = string.Empty,
                Description = string.Empty,
                AcceptanceCriteria = new List<string>(),
                Priority = string.Empty,
                StoryPoints = null,
                Tags = new List<string>(),
                EstimatedComplexity = null,
                Status = StoryStatus.Draft
            };

            // Act
            var isValid = ValidationHelper.IsValid(story);

            // Assert
            isValid.Should().BeTrue();
        }
    }
}