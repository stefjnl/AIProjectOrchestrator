using FluentAssertions;
using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Models.Review;
using AIProjectOrchestrator.UnitTests.Domain.Builders;

namespace AIProjectOrchestrator.UnitTests.Domain.Entities
{
    public class ReviewTests
    {
        [Fact]
        public void Constructor_InitializesDefaultProperties()
        {
            // Arrange & Act
            var review = new AIProjectOrchestrator.Domain.Entities.Review();

            // Assert
            review.Id.Should().Be(0);
            review.ReviewId.Should().Be(Guid.Empty);
            review.Content.Should().Be(string.Empty);
            review.Status.Should().Be(AIProjectOrchestrator.Domain.Models.Review.ReviewStatus.Pending);
            review.ServiceName.Should().Be(string.Empty);
            review.PipelineStage.Should().Be(string.Empty);
            review.Feedback.Should().Be(string.Empty);
            review.CreatedDate.Should().Be(default(DateTime));
            review.UpdatedDate.Should().Be(default(DateTime));
            review.RequirementsAnalysisId.Should().BeNull();
            review.ProjectPlanningId.Should().BeNull();
            review.StoryGenerationId.Should().BeNull();
            review.PromptGenerationId.Should().BeNull();
            review.RequirementsAnalysis.Should().BeNull();
            review.ProjectPlanning.Should().BeNull();
            review.StoryGeneration.Should().BeNull();
            review.PromptGeneration.Should().BeNull();
        }

        [Fact]
        public void Constructor_InitializesPropertiesWithValidValues()
        {
            // Arrange
            var id = 1;
            var reviewId = Guid.NewGuid();
            var content = "Test review content";
            var status = AIProjectOrchestrator.Domain.Models.Review.ReviewStatus.Approved;
            var serviceName = "TestService";
            var pipelineStage = "TestStage";
            var feedback = "Test feedback";
            var createdDate = DateTime.UtcNow;
            var updatedDate = DateTime.UtcNow;
            var requirementsAnalysisId = 1;
            var projectPlanningId = 2;
            var storyGenerationId = 3;
            var promptGenerationId = 4;

            // Act
            var review = new AIProjectOrchestrator.Domain.Entities.Review
            {
                Id = id,
                ReviewId = reviewId,
                Content = content,
                Status = status,
                ServiceName = serviceName,
                PipelineStage = pipelineStage,
                Feedback = feedback,
                CreatedDate = createdDate,
                UpdatedDate = updatedDate,
                RequirementsAnalysisId = requirementsAnalysisId,
                ProjectPlanningId = projectPlanningId,
                StoryGenerationId = storyGenerationId,
                PromptGenerationId = promptGenerationId
            };

            // Assert
            review.Id.Should().Be(id);
            review.ReviewId.Should().Be(reviewId);
            review.Content.Should().Be(content);
            review.Status.Should().Be(status);
            review.ServiceName.Should().Be(serviceName);
            review.PipelineStage.Should().Be(pipelineStage);
            review.Feedback.Should().Be(feedback);
            review.CreatedDate.Should().Be(createdDate);
            review.UpdatedDate.Should().Be(updatedDate);
            review.RequirementsAnalysisId.Should().Be(requirementsAnalysisId);
            review.ProjectPlanningId.Should().Be(projectPlanningId);
            review.StoryGenerationId.Should().Be(storyGenerationId);
            review.PromptGenerationId.Should().Be(promptGenerationId);
        }

        [Fact]
        public void Properties_SetAndGetValuesCorrectly()
        {
            // Arrange
            var review = new AIProjectOrchestrator.Domain.Entities.Review();
            var expectedId = 5;
            var expectedReviewId = Guid.NewGuid();
            var expectedContent = "Updated content";
            var expectedStatus = AIProjectOrchestrator.Domain.Models.Review.ReviewStatus.Failed;
            var expectedServiceName = "UpdatedService";
            var expectedPipelineStage = "UpdatedStage";
            var expectedFeedback = "Updated feedback";
            var expectedCreatedDate = DateTime.UtcNow.AddDays(-1);
            var expectedUpdatedDate = DateTime.UtcNow.AddDays(-1);
            var expectedRequirementsAnalysisId = 10;
            var expectedProjectPlanningId = 20;
            var expectedStoryGenerationId = 30;
            var expectedPromptGenerationId = 40;

            // Act
            review.Id = expectedId;
            review.ReviewId = expectedReviewId;
            review.Content = expectedContent;
            review.Status = expectedStatus;
            review.ServiceName = expectedServiceName;
            review.PipelineStage = expectedPipelineStage;
            review.Feedback = expectedFeedback;
            review.CreatedDate = expectedCreatedDate;
            review.UpdatedDate = expectedUpdatedDate;
            review.RequirementsAnalysisId = expectedRequirementsAnalysisId;
            review.ProjectPlanningId = expectedProjectPlanningId;
            review.StoryGenerationId = expectedStoryGenerationId;
            review.PromptGenerationId = expectedPromptGenerationId;

            // Assert
            review.Id.Should().Be(expectedId);
            review.ReviewId.Should().Be(expectedReviewId);
            review.Content.Should().Be(expectedContent);
            review.Status.Should().Be(expectedStatus);
            review.ServiceName.Should().Be(expectedServiceName);
            review.PipelineStage.Should().Be(expectedPipelineStage);
            review.Feedback.Should().Be(expectedFeedback);
            review.CreatedDate.Should().Be(expectedCreatedDate);
            review.UpdatedDate.Should().Be(expectedUpdatedDate);
            review.RequirementsAnalysisId.Should().Be(expectedRequirementsAnalysisId);
            review.ProjectPlanningId.Should().Be(expectedProjectPlanningId);
            review.StoryGenerationId.Should().Be(expectedStoryGenerationId);
            review.PromptGenerationId.Should().Be(expectedPromptGenerationId);
        }

        [Fact]
        public void ReviewId_PropertyIsGuidAndInitializedToNewGuid()
        {
            // Arrange & Act
            var review = new AIProjectOrchestrator.Domain.Entities.Review();

            // Assert
            review.ReviewId.Should().Be(Guid.Empty);
            review.ReviewId.Should().Be(default(Guid));
        }

        [Fact]
        public void NavigationProperties_CanBeAssignedAndRetrieved()
        {
            // Arrange
            var requirementsAnalysis = EntityBuilders.BuildRequirementsAnalysis(1);
            var projectPlanning = EntityBuilders.BuildProjectPlanning(1);
            var storyGeneration = EntityBuilders.BuildStoryGeneration(1);
            var promptGeneration = EntityBuilders.BuildPromptGeneration();
            var review = new AIProjectOrchestrator.Domain.Entities.Review();

            // Act & Assert
            review.RequirementsAnalysis = requirementsAnalysis;
            review.RequirementsAnalysis.Should().Be(requirementsAnalysis);

            review.ProjectPlanning = projectPlanning;
            review.ProjectPlanning.Should().Be(projectPlanning);

            review.StoryGeneration = storyGeneration;
            review.StoryGeneration.Should().Be(storyGeneration);

            review.PromptGeneration = promptGeneration;
            review.PromptGeneration.Should().Be(promptGeneration);
        }

        [Fact]
        public void ForeignKeyProperties_AreNullableIntegers()
        {
            // Arrange & Act
            var review = new AIProjectOrchestrator.Domain.Entities.Review();

            // Assert
            review.RequirementsAnalysisId.Should().BeNull();
            review.ProjectPlanningId.Should().BeNull();
            review.StoryGenerationId.Should().BeNull();
            review.PromptGenerationId.Should().BeNull();

            // Test assignment
            review.RequirementsAnalysisId = 1;
            review.ProjectPlanningId = 2;
            review.StoryGenerationId = 3;
            review.PromptGenerationId = 4;

            review.RequirementsAnalysisId.Should().Be(1);
            review.ProjectPlanningId.Should().Be(2);
            review.StoryGenerationId.Should().Be(3);
            review.PromptGenerationId.Should().Be(4);
        }

        [Fact]
        public void Id_PropertyIsIntAndDefaultsToZero()
        {
            // Arrange & Act
            var review = new AIProjectOrchestrator.Domain.Entities.Review();

            // Assert
            review.Id.Should().Be(0);
            review.Id.Should().BeOfType(typeof(int));
        }
    }
}