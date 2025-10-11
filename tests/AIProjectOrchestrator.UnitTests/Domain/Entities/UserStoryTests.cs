using FluentAssertions;
using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Models.Stories;
using AIProjectOrchestrator.UnitTests.Domain.Builders;

namespace AIProjectOrchestrator.UnitTests.Domain.Entities
{
    public class UserStoryTests
    {
        [Fact]
        public void Constructor_InitializesDefaultProperties()
        {
            // Arrange & Act
            var userStory = new UserStory();

            // Assert
            userStory.Id.Should().NotBeEmpty();
            userStory.StoryGenerationId.Should().Be(0);
            userStory.Title.Should().Be(string.Empty);
            userStory.Description.Should().Be(string.Empty);
            userStory.AcceptanceCriteria.Should().NotBeNull();
            userStory.AcceptanceCriteria.Should().BeEmpty();
            userStory.Priority.Should().Be(string.Empty);
            userStory.StoryPoints.Should().BeNull();
            userStory.Tags.Should().NotBeNull();
            userStory.Tags.Should().BeEmpty();
            userStory.EstimatedComplexity.Should().BeNull();
            userStory.Status.Should().Be(AIProjectOrchestrator.Domain.Models.Stories.StoryStatus.Draft);
            userStory.HasPrompt.Should().BeFalse();
            userStory.PromptId.Should().BeNull();
            userStory.StoryGeneration.Should().BeNull();
            userStory.PromptGenerations.Should().NotBeNull();
            userStory.PromptGenerations.Should().BeEmpty();
        }

        [Fact]
        public void Constructor_InitializesPropertiesWithValidValues()
        {
            // Arrange
            var id = Guid.NewGuid();
            var storyGenerationId = 2;
            var title = "Test User Story";
            var description = "Test description";
            var acceptanceCriteria = new List<string> { "Criteria 1", "Criteria 2" };
            var priority = "High";
            var storyPoints = 5;
            var tags = new List<string> { "tag1", "tag2" };
            var estimatedComplexity = "High";
            var status = AIProjectOrchestrator.Domain.Models.Stories.StoryStatus.Approved;
            var hasPrompt = true;
            var promptId = "prompt-1";

            // Act
            var userStory = new UserStory
            {
                Id = id,
                StoryGenerationId = storyGenerationId,
                Title = title,
                Description = description,
                AcceptanceCriteria = acceptanceCriteria,
                Priority = priority,
                StoryPoints = storyPoints,
                Tags = tags,
                EstimatedComplexity = estimatedComplexity,
                Status = status,
                HasPrompt = hasPrompt,
                PromptId = promptId
            };

            // Assert
            userStory.Id.Should().Be(id);
            userStory.StoryGenerationId.Should().Be(storyGenerationId);
            userStory.Title.Should().Be(title);
            userStory.Description.Should().Be(description);
            userStory.AcceptanceCriteria.Should().Contain(acceptanceCriteria);
            userStory.Priority.Should().Be(priority);
            userStory.StoryPoints.Should().Be(storyPoints);
            userStory.Tags.Should().Contain(tags);
            userStory.EstimatedComplexity.Should().Be(estimatedComplexity);
            userStory.Status.Should().Be(status);
            userStory.HasPrompt.Should().Be(hasPrompt);
            userStory.PromptId.Should().Be(promptId);
        }

        [Fact]
        public void Properties_SetAndGetValuesCorrectly()
        {
            // Arrange
            var userStory = new UserStory();
            var expectedId = Guid.NewGuid();
            var expectedStoryGenerationId = 5;
            var expectedTitle = "Updated Title";
            var expectedDescription = "Updated Description";
            var expectedAcceptanceCriteria = new List<string> { "Updated Criteria" };
            var expectedPriority = "Low";
            var expectedStoryPoints = 8;
            var expectedTags = new List<string> { "updated-tag" };
            var expectedEstimatedComplexity = "Medium";
            var expectedStatus = AIProjectOrchestrator.Domain.Models.Stories.StoryStatus.Approved;
            var expectedHasPrompt = true;
            var expectedPromptId = "prompt-updated";

            // Act
            userStory.Id = expectedId;
            userStory.StoryGenerationId = expectedStoryGenerationId;
            userStory.Title = expectedTitle;
            userStory.Description = expectedDescription;
            userStory.AcceptanceCriteria = expectedAcceptanceCriteria;
            userStory.Priority = expectedPriority;
            userStory.StoryPoints = expectedStoryPoints;
            userStory.Tags = expectedTags;
            userStory.EstimatedComplexity = expectedEstimatedComplexity;
            userStory.Status = expectedStatus;
            userStory.HasPrompt = expectedHasPrompt;
            userStory.PromptId = expectedPromptId;

            // Assert
            userStory.Id.Should().Be(expectedId);
            userStory.StoryGenerationId.Should().Be(expectedStoryGenerationId);
            userStory.Title.Should().Be(expectedTitle);
            userStory.Description.Should().Be(expectedDescription);
            userStory.AcceptanceCriteria.Should().Contain(expectedAcceptanceCriteria);
            userStory.Priority.Should().Be(expectedPriority);
            userStory.StoryPoints.Should().Be(expectedStoryPoints);
            userStory.Tags.Should().Contain(expectedTags);
            userStory.EstimatedComplexity.Should().Be(expectedEstimatedComplexity);
            userStory.Status.Should().Be(expectedStatus);
            userStory.HasPrompt.Should().Be(expectedHasPrompt);
            userStory.PromptId.Should().Be(expectedPromptId);
        }

        [Fact]
        public void Id_PropertyIsGuidAndInitializedToNewGuid()
        {
            // Arrange & Act
            var userStory = new UserStory();

            // Assert
            userStory.Id.Should().NotBeEmpty();
            userStory.Id.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public void StoryGenerationNavigationProperty_CanBeAssignedAndRetrieved()
        {
            // Arrange
            var storyGeneration = EntityBuilders.BuildStoryGeneration(1);
            var userStory = new UserStory();

            // Act
            userStory.StoryGeneration = storyGeneration;

            // Assert
            userStory.StoryGeneration.Should().Be(storyGeneration);
            // StoryGenerationId is not automatically updated when StoryGeneration navigation property is set without EF Core context
            userStory.StoryGenerationId.Should().Be(0);
        }

        [Fact]
        public void AcceptanceCriteria_CollectionIsInitialized()
        {
            // Arrange & Act
            var userStory = new UserStory();

            // Assert
            userStory.AcceptanceCriteria.Should().NotBeNull();
            userStory.AcceptanceCriteria.Should().BeAssignableTo<List<string>>();
            userStory.AcceptanceCriteria.Should().BeEmpty();
        }

        [Fact]
        public void Tags_CollectionIsInitialized()
        {
            // Arrange & Act
            var userStory = new UserStory();

            // Assert
            userStory.Tags.Should().NotBeNull();
            userStory.Tags.Should().BeAssignableTo<List<string>>();
            userStory.Tags.Should().BeEmpty();
        }

        [Fact]
        public void PromptGenerations_CollectionIsInitialized()
        {
            // Arrange & Act
            var userStory = new UserStory();

            // Assert
            userStory.PromptGenerations.Should().NotBeNull();
            userStory.PromptGenerations.Should().BeAssignableTo<List<PromptGeneration>>();
            userStory.PromptGenerations.Should().BeEmpty();
        }

        [Fact]
        public void PromptGenerations_CollectionSupportsAddRemoveOperations()
        {
            // Arrange
            var userStory = new UserStory();
            var promptGeneration = EntityBuilders.BuildPromptGeneration();

            // Act
            userStory.PromptGenerations.Add(promptGeneration);

            // Assert
            userStory.PromptGenerations.Should().Contain(promptGeneration);
            userStory.PromptGenerations.Count.Should().Be(1);

            // Act - Remove
            userStory.PromptGenerations.Remove(promptGeneration);

            // Assert
            userStory.PromptGenerations.Should().NotContain(promptGeneration);
            userStory.PromptGenerations.Count.Should().Be(0);
        }
    }
}