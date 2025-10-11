using FluentAssertions;
using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Models;
using AIProjectOrchestrator.Domain.Models.Stories;
using AIProjectOrchestrator.UnitTests.Domain.Builders;

namespace AIProjectOrchestrator.UnitTests.Domain.Entities
{
    public class StoryGenerationTests
    {
        [Fact]
        public void Constructor_InitializesDefaultProperties()
        {
            // Arrange & Act
            var storyGeneration = new StoryGeneration();

            // Assert
            storyGeneration.Id.Should().Be(0);
            storyGeneration.ProjectPlanningId.Should().Be(0);
            storyGeneration.GenerationId.Should().Be(string.Empty);
            storyGeneration.Status.Should().Be(AIProjectOrchestrator.Domain.Models.Stories.StoryGenerationStatus.NotStarted);
            storyGeneration.Content.Should().Be(string.Empty);
            storyGeneration.ReviewId.Should().Be(string.Empty);
            storyGeneration.CreatedDate.Should().Be(default(DateTime));
            storyGeneration.StoriesJson.Should().Be(string.Empty);
            storyGeneration.ProjectPlanning.Should().BeNull();
            storyGeneration.Review.Should().BeNull();
            storyGeneration.PromptGenerations.Should().NotBeNull();
            storyGeneration.PromptGenerations.Should().BeEmpty();
            storyGeneration.Stories.Should().NotBeNull();
            storyGeneration.Stories.Should().BeEmpty();
        }

        [Fact]
        public void Constructor_InitializesPropertiesWithValidValues()
        {
            // Arrange
            var id = 1;
            var projectPlanningId = 2;
            var generationId = "generation-test";
            var status = AIProjectOrchestrator.Domain.Models.Stories.StoryGenerationStatus.Approved;
            var content = "Test story generation content";
            var reviewId = "review-test";
            var storiesJson = "[{\"id\":\"1\", \"title\":\"Test Story\"}]";
            var createdDate = DateTime.UtcNow;

            // Act
            var storyGeneration = new StoryGeneration
            {
                Id = id,
                ProjectPlanningId = projectPlanningId,
                GenerationId = generationId,
                Status = status,
                Content = content,
                ReviewId = reviewId,
                StoriesJson = storiesJson,
                CreatedDate = createdDate
            };

            // Assert
            storyGeneration.Id.Should().Be(id);
            storyGeneration.ProjectPlanningId.Should().Be(projectPlanningId);
            storyGeneration.GenerationId.Should().Be(generationId);
            storyGeneration.Status.Should().Be(status);
            storyGeneration.Content.Should().Be(content);
            storyGeneration.ReviewId.Should().Be(reviewId);
            storyGeneration.StoriesJson.Should().Be(storiesJson);
            storyGeneration.CreatedDate.Should().Be(createdDate);
        }

        [Fact]
        public void Properties_SetAndGetValuesCorrectly()
        {
            // Arrange
            var storyGeneration = new StoryGeneration();
            var expectedProjectPlanningId = 5;
            var expectedGenerationId = "generation-updated";
            var expectedStatus = AIProjectOrchestrator.Domain.Models.Stories.StoryGenerationStatus.Failed;
            var expectedContent = "Updated content";
            var expectedReviewId = "review-updated";
            var expectedStoriesJson = "[{\"id\":\"2\", \"title\":\"Updated Story\"}]";
            var expectedCreatedDate = DateTime.UtcNow.AddDays(-1);

            // Act
            storyGeneration.ProjectPlanningId = expectedProjectPlanningId;
            storyGeneration.GenerationId = expectedGenerationId;
            storyGeneration.Status = expectedStatus;
            storyGeneration.Content = expectedContent;
            storyGeneration.ReviewId = expectedReviewId;
            storyGeneration.StoriesJson = expectedStoriesJson;
            storyGeneration.CreatedDate = expectedCreatedDate;

            // Assert
            storyGeneration.ProjectPlanningId.Should().Be(expectedProjectPlanningId);
            storyGeneration.GenerationId.Should().Be(expectedGenerationId);
            storyGeneration.Status.Should().Be(expectedStatus);
            storyGeneration.Content.Should().Be(expectedContent);
            storyGeneration.ReviewId.Should().Be(expectedReviewId);
            storyGeneration.StoriesJson.Should().Be(expectedStoriesJson);
            storyGeneration.CreatedDate.Should().Be(expectedCreatedDate);
        }

        [Fact]
        public void ProjectPlanningNavigationProperty_CanBeAssignedAndRetrieved()
        {
            // Arrange
            var projectPlanning = EntityBuilders.BuildProjectPlanning(1);
            var storyGeneration = new StoryGeneration();

            // Act
            storyGeneration.ProjectPlanning = projectPlanning;

            // Assert
            storyGeneration.ProjectPlanning.Should().Be(projectPlanning);
            // ProjectPlanningId is not automatically updated when ProjectPlanning navigation property is set without EF Core context
            storyGeneration.ProjectPlanningId.Should().Be(0);
        }

        [Fact]
        public void ReviewNavigationProperty_CanBeAssignedAndRetrieved()
        {
            // Arrange
            var review = EntityBuilders.BuildReview(1);
            var storyGeneration = new StoryGeneration();

            // Act
            storyGeneration.Review = review;

            // Assert
            storyGeneration.Review.Should().Be(review);
        }

        [Fact]
        public void PromptGenerations_CollectionIsInitialized()
        {
            // Arrange & Act
            var storyGeneration = new StoryGeneration();

            // Assert
            storyGeneration.PromptGenerations.Should().NotBeNull();
            storyGeneration.PromptGenerations.Should().BeAssignableTo<ICollection<PromptGeneration>>();
            storyGeneration.PromptGenerations.Should().BeEmpty();
        }

        [Fact]
        public void PromptGenerations_CollectionSupportsAddRemoveOperations()
        {
            // Arrange
            var storyGeneration = new StoryGeneration();
            var promptGeneration = EntityBuilders.BuildPromptGeneration();

            // Act
            storyGeneration.PromptGenerations.Add(promptGeneration);

            // Assert
            storyGeneration.PromptGenerations.Should().Contain(promptGeneration);
            storyGeneration.PromptGenerations.Count.Should().Be(1);

            // Act - Remove
            storyGeneration.PromptGenerations.Remove(promptGeneration);

            // Assert
            storyGeneration.PromptGenerations.Should().NotContain(promptGeneration);
            storyGeneration.PromptGenerations.Count.Should().Be(0);
        }

        [Fact]
        public void Stories_CollectionIsInitialized()
        {
            // Arrange & Act
            var storyGeneration = new StoryGeneration();

            // Assert
            storyGeneration.Stories.Should().NotBeNull();
            storyGeneration.Stories.Should().BeAssignableTo<ICollection<UserStory>>();
            storyGeneration.Stories.Should().BeEmpty();
        }

        [Fact]
        public void Stories_CollectionSupportsAddRemoveOperations()
        {
            // Arrange
            var storyGeneration = new StoryGeneration();
            var userStory = EntityBuilders.BuildUserStory();

            // Act
            storyGeneration.Stories.Add(userStory);

            // Assert
            storyGeneration.Stories.Should().Contain(userStory);
            storyGeneration.Stories.Count.Should().Be(1);

            // Act - Remove
            storyGeneration.Stories.Remove(userStory);

            // Assert
            storyGeneration.Stories.Should().NotContain(userStory);
            storyGeneration.Stories.Count.Should().Be(0);
        }

        [Fact]
        public void Id_PropertyIsIntAndDefaultsToZero()
        {
            // Arrange & Act
            var storyGeneration = new StoryGeneration();

            // Assert
            storyGeneration.Id.Should().Be(0);
            storyGeneration.Id.Should().BeOfType(typeof(int));
        }
    }
}