using FluentAssertions;
using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Models;
using AIProjectOrchestrator.Domain.Models.PromptGeneration;
using AIProjectOrchestrator.UnitTests.Domain.Builders;

namespace AIProjectOrchestrator.UnitTests.Domain.Entities
{
    public class PromptGenerationTests
    {
        [Fact]
        public void Constructor_InitializesDefaultProperties()
        {
            // Arrange & Act
            var promptGeneration = new PromptGeneration();

            // Assert
            promptGeneration.Id.Should().Be(0);
            promptGeneration.UserStoryId.Should().Be(Guid.Empty);
            promptGeneration.StoryIndex.Should().Be(0);
            promptGeneration.PromptId.Should().Be(string.Empty);
            promptGeneration.Status.Should().Be(AIProjectOrchestrator.Domain.Models.PromptGeneration.PromptGenerationStatus.NotStarted);
            promptGeneration.Content.Should().Be(string.Empty);
            promptGeneration.ReviewId.Should().Be(string.Empty);
            promptGeneration.CreatedDate.Should().Be(default(DateTime));
            promptGeneration.UserStory.Should().BeNull();
            promptGeneration.Review.Should().BeNull();
        }

        [Fact]
        public void Constructor_InitializesPropertiesWithValidValues()
        {
            // Arrange
            var id = 1;
            var userStoryId = Guid.NewGuid();
            var storyIndex = 2;
            var promptId = "prompt-test";
            var status = AIProjectOrchestrator.Domain.Models.PromptGeneration.PromptGenerationStatus.Approved;
            var content = "Test prompt content";
            var reviewId = "review-test";
            var createdDate = DateTime.UtcNow;

            // Act
            var promptGeneration = new PromptGeneration
            {
                Id = id,
                UserStoryId = userStoryId,
                StoryIndex = storyIndex,
                PromptId = promptId,
                Status = status,
                Content = content,
                ReviewId = reviewId,
                CreatedDate = createdDate
            };

            // Assert
            promptGeneration.Id.Should().Be(id);
            promptGeneration.UserStoryId.Should().Be(userStoryId);
            promptGeneration.StoryIndex.Should().Be(storyIndex);
            promptGeneration.PromptId.Should().Be(promptId);
            promptGeneration.Status.Should().Be(status);
            promptGeneration.Content.Should().Be(content);
            promptGeneration.ReviewId.Should().Be(reviewId);
            promptGeneration.CreatedDate.Should().Be(createdDate);
        }

        [Fact]
        public void Properties_SetAndGetValuesCorrectly()
        {
            // Arrange
            var promptGeneration = new PromptGeneration();
            var expectedId = 5;
            var expectedUserStoryId = Guid.NewGuid();
            var expectedStoryIndex = 3;
            var expectedPromptId = "prompt-updated";
            var expectedStatus = AIProjectOrchestrator.Domain.Models.PromptGeneration.PromptGenerationStatus.Failed;
            var expectedContent = "Updated content";
            var expectedReviewId = "review-updated";
            var expectedCreatedDate = DateTime.UtcNow.AddDays(-1);

            // Act
            promptGeneration.Id = expectedId;
            promptGeneration.UserStoryId = expectedUserStoryId;
            promptGeneration.StoryIndex = expectedStoryIndex;
            promptGeneration.PromptId = expectedPromptId;
            promptGeneration.Status = expectedStatus;
            promptGeneration.Content = expectedContent;
            promptGeneration.ReviewId = expectedReviewId;
            promptGeneration.CreatedDate = expectedCreatedDate;

            // Assert
            promptGeneration.Id.Should().Be(expectedId);
            promptGeneration.UserStoryId.Should().Be(expectedUserStoryId);
            promptGeneration.StoryIndex.Should().Be(expectedStoryIndex);
            promptGeneration.PromptId.Should().Be(expectedPromptId);
            promptGeneration.Status.Should().Be(expectedStatus);
            promptGeneration.Content.Should().Be(expectedContent);
            promptGeneration.ReviewId.Should().Be(expectedReviewId);
            promptGeneration.CreatedDate.Should().Be(expectedCreatedDate);
        }

        [Fact]
        public void UserStoryNavigationProperty_CanBeAssignedAndRetrieved()
        {
            // Arrange
            var userStory = EntityBuilders.BuildUserStory();
            var promptGeneration = new PromptGeneration();

            // Act
            promptGeneration.UserStory = userStory;

            // Assert
            promptGeneration.UserStory.Should().Be(userStory);
            // UserStoryId is not automatically updated when UserStory navigation property is set without EF Core context
            promptGeneration.UserStoryId.Should().Be(Guid.Empty);
        }

        [Fact]
        public void ReviewNavigationProperty_CanBeAssignedAndRetrieved()
        {
            // Arrange
            var review = EntityBuilders.BuildReview(1);
            var promptGeneration = new PromptGeneration();

            // Act
            promptGeneration.Review = review;

            // Assert
            promptGeneration.Review.Should().Be(review);
        }

        [Fact]
        public void Id_PropertyIsIntAndDefaultsToZero()
        {
            // Arrange & Act
            var promptGeneration = new PromptGeneration();

            // Assert
            promptGeneration.Id.Should().Be(0);
            promptGeneration.Id.Should().BeOfType(typeof(int));
        }

        [Fact]
        public void UserStoryId_PropertyIsGuidAndDefaultsToEmpty()
        {
            // Arrange & Act
            var promptGeneration = new PromptGeneration();

            // Assert
            promptGeneration.UserStoryId.Should().Be(Guid.Empty);
            promptGeneration.UserStoryId.Should().Be(Guid.Empty);
        }

        [Fact]
        public void StoryIndex_PropertyIsIntAndDefaultsToZero()
        {
            // Arrange & Act
            var promptGeneration = new PromptGeneration();

            // Assert
            promptGeneration.StoryIndex.Should().Be(0);
            promptGeneration.StoryIndex.Should().BeOfType(typeof(int));
        }
    }
}