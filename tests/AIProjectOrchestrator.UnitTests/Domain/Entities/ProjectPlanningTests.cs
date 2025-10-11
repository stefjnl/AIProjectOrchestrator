using FluentAssertions;
using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Models;
using AIProjectOrchestrator.UnitTests.Domain.Builders;

namespace AIProjectOrchestrator.UnitTests.Domain.Entities
{
    public class ProjectPlanningTests
    {
        [Fact]
        public void Constructor_InitializesDefaultProperties()
        {
            // Arrange & Act
            var projectPlanning = new ProjectPlanning();

            // Assert
            projectPlanning.Id.Should().Be(0);
            projectPlanning.RequirementsAnalysisId.Should().Be(0);
            projectPlanning.PlanningId.Should().Be(string.Empty);
            projectPlanning.Status.Should().Be(AIProjectOrchestrator.Domain.Models.ProjectPlanningStatus.NotStarted);
            projectPlanning.Content.Should().Be(string.Empty);
            projectPlanning.ReviewId.Should().Be(string.Empty);
            projectPlanning.CreatedDate.Should().Be(default(DateTime));
            projectPlanning.RequirementsAnalysis.Should().BeNull();
            projectPlanning.Review.Should().BeNull();
            projectPlanning.StoryGenerations.Should().NotBeNull();
            projectPlanning.StoryGenerations.Should().BeEmpty();
        }

        [Fact]
        public void Constructor_InitializesPropertiesWithValidValues()
        {
            // Arrange
            var id = 1;
            var requirementsAnalysisId = 2;
            var planningId = "planning-test";
            var status = AIProjectOrchestrator.Domain.Models.ProjectPlanningStatus.Approved;
            var content = "Test project planning content";
            var reviewId = "review-test";
            var createdDate = DateTime.UtcNow;

            // Act
            var projectPlanning = new ProjectPlanning
            {
                Id = id,
                RequirementsAnalysisId = requirementsAnalysisId,
                PlanningId = planningId,
                Status = status,
                Content = content,
                ReviewId = reviewId,
                CreatedDate = createdDate
            };

            // Assert
            projectPlanning.Id.Should().Be(id);
            projectPlanning.RequirementsAnalysisId.Should().Be(requirementsAnalysisId);
            projectPlanning.PlanningId.Should().Be(planningId);
            projectPlanning.Status.Should().Be(status);
            projectPlanning.Content.Should().Be(content);
            projectPlanning.ReviewId.Should().Be(reviewId);
            projectPlanning.CreatedDate.Should().Be(createdDate);
        }

        [Fact]
        public void Properties_SetAndGetValuesCorrectly()
        {
            // Arrange
            var projectPlanning = new ProjectPlanning();
            var expectedRequirementsAnalysisId = 5;
            var expectedPlanningId = "planning-updated";
            var expectedStatus = AIProjectOrchestrator.Domain.Models.ProjectPlanningStatus.Failed;
            var expectedContent = "Updated content";
            var expectedReviewId = "review-updated";
            var expectedCreatedDate = DateTime.UtcNow.AddDays(-1);

            // Act
            projectPlanning.RequirementsAnalysisId = expectedRequirementsAnalysisId;
            projectPlanning.PlanningId = expectedPlanningId;
            projectPlanning.Status = expectedStatus;
            projectPlanning.Content = expectedContent;
            projectPlanning.ReviewId = expectedReviewId;
            projectPlanning.CreatedDate = expectedCreatedDate;

            // Assert
            projectPlanning.RequirementsAnalysisId.Should().Be(expectedRequirementsAnalysisId);
            projectPlanning.PlanningId.Should().Be(expectedPlanningId);
            projectPlanning.Status.Should().Be(expectedStatus);
            projectPlanning.Content.Should().Be(expectedContent);
            projectPlanning.ReviewId.Should().Be(expectedReviewId);
            projectPlanning.CreatedDate.Should().Be(expectedCreatedDate);
        }

        [Fact]
        public void RequirementsAnalysisNavigationProperty_CanBeAssignedAndRetrieved()
        {
            // Arrange
            var requirementsAnalysis = EntityBuilders.BuildRequirementsAnalysis(1);
            var projectPlanning = new ProjectPlanning();

            // Act
            projectPlanning.RequirementsAnalysis = requirementsAnalysis;

            // Assert
            projectPlanning.RequirementsAnalysis.Should().Be(requirementsAnalysis);
            // RequirementsAnalysisId is not automatically updated when RequirementsAnalysis navigation property is set without EF Core context
            projectPlanning.RequirementsAnalysisId.Should().Be(0);
        }

        [Fact]
        public void ReviewNavigationProperty_CanBeAssignedAndRetrieved()
        {
            // Arrange
            var review = EntityBuilders.BuildReview(1);
            var projectPlanning = new ProjectPlanning();

            // Act
            projectPlanning.Review = review;

            // Assert
            projectPlanning.Review.Should().Be(review);
        }

        [Fact]
        public void StoryGenerations_CollectionIsInitialized()
        {
            // Arrange & Act
            var projectPlanning = new ProjectPlanning();

            // Assert
            projectPlanning.StoryGenerations.Should().NotBeNull();
            projectPlanning.StoryGenerations.Should().BeAssignableTo<ICollection<StoryGeneration>>();
            projectPlanning.StoryGenerations.Should().BeEmpty();
        }

        [Fact]
        public void StoryGenerations_CollectionSupportsAddRemoveOperations()
        {
            // Arrange
            var projectPlanning = new ProjectPlanning();
            var storyGeneration = EntityBuilders.BuildStoryGeneration();

            // Act
            projectPlanning.StoryGenerations.Add(storyGeneration);

            // Assert
            projectPlanning.StoryGenerations.Should().Contain(storyGeneration);
            projectPlanning.StoryGenerations.Count.Should().Be(1);

            // Act - Remove
            projectPlanning.StoryGenerations.Remove(storyGeneration);

            // Assert
            projectPlanning.StoryGenerations.Should().NotContain(storyGeneration);
            projectPlanning.StoryGenerations.Count.Should().Be(0);
        }

        [Fact]
        public void Id_PropertyIsIntAndDefaultsToZero()
        {
            // Arrange & Act
            var projectPlanning = new ProjectPlanning();

            // Assert
            projectPlanning.Id.Should().Be(0);
            projectPlanning.Id.Should().BeOfType(typeof(int));
        }
    }
}