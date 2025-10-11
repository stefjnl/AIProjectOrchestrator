using FluentAssertions;
using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Models;
using AIProjectOrchestrator.UnitTests.Domain.Builders;

namespace AIProjectOrchestrator.UnitTests.Domain.Entities
{
    public class RequirementsAnalysisTests
    {
        [Fact]
        public void Constructor_InitializesDefaultProperties()
        {
            // Arrange & Act
            var requirementsAnalysis = new RequirementsAnalysis();

            // Assert
            requirementsAnalysis.Id.Should().Be(0);
            requirementsAnalysis.ProjectId.Should().Be(0);
            requirementsAnalysis.AnalysisId.Should().Be(string.Empty);
            requirementsAnalysis.Status.Should().Be(AIProjectOrchestrator.Domain.Models.RequirementsAnalysisStatus.NotStarted);
            requirementsAnalysis.Content.Should().Be(string.Empty);
            requirementsAnalysis.ReviewId.Should().Be(string.Empty);
            requirementsAnalysis.CreatedDate.Should().Be(default(DateTime));
            requirementsAnalysis.Project.Should().BeNull();
            requirementsAnalysis.Review.Should().BeNull();
            requirementsAnalysis.ProjectPlannings.Should().NotBeNull();
            requirementsAnalysis.ProjectPlannings.Should().BeEmpty();
        }

        [Fact]
        public void Constructor_InitializesPropertiesWithValidValues()
        {
            // Arrange
            var id = 1;
            var projectId = 2;
            var analysisId = "analysis-test";
            var status = AIProjectOrchestrator.Domain.Models.RequirementsAnalysisStatus.Approved;
            var content = "Test requirements analysis content";
            var reviewId = "review-test";
            var createdDate = DateTime.UtcNow;

            // Act
            var requirementsAnalysis = new RequirementsAnalysis
            {
                Id = id,
                ProjectId = projectId,
                AnalysisId = analysisId,
                Status = status,
                Content = content,
                ReviewId = reviewId,
                CreatedDate = createdDate
            };

            // Assert
            requirementsAnalysis.Id.Should().Be(id);
            requirementsAnalysis.ProjectId.Should().Be(projectId);
            requirementsAnalysis.AnalysisId.Should().Be(analysisId);
            requirementsAnalysis.Status.Should().Be(status);
            requirementsAnalysis.Content.Should().Be(content);
            requirementsAnalysis.ReviewId.Should().Be(reviewId);
            requirementsAnalysis.CreatedDate.Should().Be(createdDate);
        }

        [Fact]
        public void Properties_SetAndGetValuesCorrectly()
        {
            // Arrange
            var requirementsAnalysis = new RequirementsAnalysis();
            var expectedProjectId = 5;
            var expectedAnalysisId = "analysis-updated";
            var expectedStatus = AIProjectOrchestrator.Domain.Models.RequirementsAnalysisStatus.Failed;
            var expectedContent = "Updated content";
            var expectedReviewId = "review-updated";
            var expectedCreatedDate = DateTime.UtcNow.AddDays(-1);

            // Act
            requirementsAnalysis.ProjectId = expectedProjectId;
            requirementsAnalysis.AnalysisId = expectedAnalysisId;
            requirementsAnalysis.Status = expectedStatus;
            requirementsAnalysis.Content = expectedContent;
            requirementsAnalysis.ReviewId = expectedReviewId;
            requirementsAnalysis.CreatedDate = expectedCreatedDate;

            // Assert
            requirementsAnalysis.ProjectId.Should().Be(expectedProjectId);
            requirementsAnalysis.AnalysisId.Should().Be(expectedAnalysisId);
            requirementsAnalysis.Status.Should().Be(expectedStatus);
            requirementsAnalysis.Content.Should().Be(expectedContent);
            requirementsAnalysis.ReviewId.Should().Be(expectedReviewId);
            requirementsAnalysis.CreatedDate.Should().Be(expectedCreatedDate);
        }

        [Fact]
        public void ProjectNavigationProperty_CanBeAssignedAndRetrieved()
        {
            // Arrange
            var project = EntityBuilders.BuildProject(1);
            var requirementsAnalysis = new RequirementsAnalysis();

            // Act
            requirementsAnalysis.Project = project;

            // Assert
            requirementsAnalysis.Project.Should().Be(project);
            // ProjectId is not automatically updated when Project navigation property is set without EF Core context
            requirementsAnalysis.ProjectId.Should().Be(0);
        }

        [Fact]
        public void ReviewNavigationProperty_CanBeAssignedAndRetrieved()
        {
            // Arrange
            var review = EntityBuilders.BuildReview(1);
            var requirementsAnalysis = new RequirementsAnalysis();

            // Act
            requirementsAnalysis.Review = review;

            // Assert
            requirementsAnalysis.Review.Should().Be(review);
        }

        [Fact]
        public void ProjectPlannings_CollectionIsInitialized()
        {
            // Arrange & Act
            var requirementsAnalysis = new RequirementsAnalysis();

            // Assert
            requirementsAnalysis.ProjectPlannings.Should().NotBeNull();
            requirementsAnalysis.ProjectPlannings.Should().BeAssignableTo<ICollection<ProjectPlanning>>();
            requirementsAnalysis.ProjectPlannings.Should().BeEmpty();
        }

        [Fact]
        public void ProjectPlannings_CollectionSupportsAddRemoveOperations()
        {
            // Arrange
            var requirementsAnalysis = new RequirementsAnalysis();
            var projectPlanning = EntityBuilders.BuildProjectPlanning();

            // Act
            requirementsAnalysis.ProjectPlannings.Add(projectPlanning);

            // Assert
            requirementsAnalysis.ProjectPlannings.Should().Contain(projectPlanning);
            requirementsAnalysis.ProjectPlannings.Count.Should().Be(1);

            // Act - Remove
            requirementsAnalysis.ProjectPlannings.Remove(projectPlanning);

            // Assert
            requirementsAnalysis.ProjectPlannings.Should().NotContain(projectPlanning);
            requirementsAnalysis.ProjectPlannings.Count.Should().Be(0);
        }

        [Fact]
        public void Id_PropertyIsIntAndDefaultsToZero()
        {
            // Arrange & Act
            var requirementsAnalysis = new RequirementsAnalysis();

            // Assert
            requirementsAnalysis.Id.Should().Be(0);
            requirementsAnalysis.Id.Should().BeOfType(typeof(int));
        }
    }
}