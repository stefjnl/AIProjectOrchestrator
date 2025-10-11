using AIProjectOrchestrator.Domain.Models.Stories;
using AIProjectOrchestrator.UnitTests.Domain.Validation;
using FluentAssertions;
using Xunit;

namespace AIProjectOrchestrator.UnitTests.Domain.Validation
{
    public class EditStoryRequestValidationTests
    {
        [Fact]
        public void EditStoryRequest_ValidObject_PassesValidation()
        {
            // Arrange
            var request = new EditStoryRequest
            {
                UpdatedStory = new UpdateStoryDto
                {
                    Title = "Test Title",
                    Description = "Test Description",
                    AcceptanceCriteria = new System.Collections.Generic.List<string> { "Criteria 1" },
                    Priority = "High",
                    StoryPoints = 5,
                    Tags = new System.Collections.Generic.List<string> { "tag1" },
                    EstimatedComplexity = "Medium",
                    Status = StoryStatus.Draft
                }
            };

            // Act
            var isValid = ValidationHelper.IsValid(request);

            // Assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void EditStoryRequest_WithNullUpdatedStory_PassesValidation()
        {
            // Arrange
            var request = new EditStoryRequest();

            // Act
            var isValid = ValidationHelper.IsValid(request);

            // Assert
            isValid.Should().BeTrue(); // No Required attribute means null is allowed
        }

        [Fact]
        public void EditStoryRequest_WithEmptyUpdatedStory_PassesValidation()
        {
            // Arrange
            var request = new EditStoryRequest
            {
                UpdatedStory = new UpdateStoryDto()
            };

            // Act
            var isValid = ValidationHelper.IsValid(request);

            // Assert
            isValid.Should().BeTrue();
        }
    }
}