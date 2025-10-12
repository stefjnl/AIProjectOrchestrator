using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Interfaces;
using AIProjectOrchestrator.Domain.Models.Review;
using AIProjectOrchestrator.Infrastructure.Repositories;
using AIProjectOrchestrator.UnitTests.Domain.Builders;
using AIProjectOrchestrator.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AIProjectOrchestrator.UnitTests.Infrastructure.Repositories
{
    public class ReviewRepositoryTests : IAsyncLifetime
    {
        private readonly AppDbContext _sharedContext;

        public ReviewRepositoryTests()
        {
            _sharedContext = TestDbContextFactory.CreateContext();
        }

        public async Task InitializeAsync()
        {
            // Initialize resources if needed
            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            _sharedContext?.Dispose();
            await Task.CompletedTask;
        }

        [Fact]
        public async Task AddAsync_PersistsEntity()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContext();
            var repository = new ReviewRepository(context);
            var review = EntityBuilders.BuildReview();

            // Act
            var result = await repository.AddAsync(review, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().BeGreaterThan(0);
            result.ReviewId.Should().NotBeEmpty();
            
            var savedEntity = await context.Reviews.FindAsync(result.Id);
            savedEntity.Should().NotBeNull();
            savedEntity?.Content.Should().Be(review.Content);
            
            context.Dispose();
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsEntity()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContext();
            var repository = new ReviewRepository(context);
            var review = EntityBuilders.BuildReview();
            var addedEntity = await repository.AddAsync(review, CancellationToken.None);

            // Act
            var result = await repository.GetByIdAsync(addedEntity.Id, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result?.Id.Should().Be(addedEntity.Id);
            result?.Content.Should().Be(review.Content);
            
            context.Dispose();
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContext();
            var repository = new ReviewRepository(context);

            // Act
            var result = await repository.GetByIdAsync(999, CancellationToken.None);

            // Assert
            result.Should().BeNull();
            
            context.Dispose();
        }

        [Fact]
        public async Task GetByReviewIdAsync_WithValidId_ReturnsEntity()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContext();
            var repository = new ReviewRepository(context);
            var review = EntityBuilders.BuildReview(reviewId: Guid.NewGuid());
            var addedEntity = await repository.AddAsync(review, CancellationToken.None);

            // Act
            var result = await repository.GetByReviewIdAsync(addedEntity.ReviewId, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result?.ReviewId.Should().Be(addedEntity.ReviewId);
            
            context.Dispose();
        }

        [Fact]
        public async Task GetByReviewIdAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContext();
            var repository = new ReviewRepository(context);

            // Act
            var result = await repository.GetByReviewIdAsync(Guid.NewGuid(), CancellationToken.None);

            // Assert
            result.Should().BeNull();
            
            context.Dispose();
        }

        [Fact]
        public async Task GetByWorkflowEntityIdAsync_WithValidRequirementsAnalysis_ReturnsEntity()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContext();
            var repository = new ReviewRepository(context);
            var project = EntityBuilders.BuildProject();
            await context.Projects.AddAsync(project);
            await context.SaveChangesAsync();
            
            var requirementsAnalysis = EntityBuilders.BuildRequirementsAnalysis(projectId: project.Id);
            await context.RequirementsAnalyses.AddAsync(requirementsAnalysis);
            await context.SaveChangesAsync();
            
            var review = EntityBuilders.BuildReview(requirementsAnalysisId: requirementsAnalysis.Id);
            var addedEntity = await repository.AddAsync(review, CancellationToken.None);

            // Act
            var result = await repository.GetByWorkflowEntityIdAsync(requirementsAnalysis.Id, "RequirementsAnalysis", CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result?.Id.Should().Be(addedEntity?.Id);
            
            context.Dispose();
        }

        [Fact]
        public async Task GetByWorkflowEntityIdAsync_WithValidProjectPlanning_ReturnsEntity()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContext();
            var repository = new ReviewRepository(context);
            var project = EntityBuilders.BuildProject();
            await context.Projects.AddAsync(project);
            await context.SaveChangesAsync();
            
            var requirementsAnalysis = EntityBuilders.BuildRequirementsAnalysis(projectId: project.Id);
            await context.RequirementsAnalyses.AddAsync(requirementsAnalysis);
            await context.SaveChangesAsync();
            
            var projectPlanning = EntityBuilders.BuildProjectPlanning(requirementsAnalysisId: requirementsAnalysis.Id);
            await context.ProjectPlannings.AddAsync(projectPlanning);
            await context.SaveChangesAsync();
            
            var review = EntityBuilders.BuildReview(projectPlanningId: projectPlanning.Id);
            var addedEntity = await repository.AddAsync(review, CancellationToken.None);

            // Act
            var result = await repository.GetByWorkflowEntityIdAsync(projectPlanning.Id, "ProjectPlanning", CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result?.Id.Should().Be(addedEntity?.Id);
            
            context.Dispose();
        }

        [Fact]
        public async Task GetByWorkflowEntityIdAsync_WithValidStoryGeneration_ReturnsEntity()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContext();
            var repository = new ReviewRepository(context);
            var project = EntityBuilders.BuildProject();
            await context.Projects.AddAsync(project);
            await context.SaveChangesAsync();
            
            var requirementsAnalysis = EntityBuilders.BuildRequirementsAnalysis(projectId: project.Id);
            await context.RequirementsAnalyses.AddAsync(requirementsAnalysis);
            await context.SaveChangesAsync();
            
            var projectPlanning = EntityBuilders.BuildProjectPlanning(requirementsAnalysisId: requirementsAnalysis.Id);
            await context.ProjectPlannings.AddAsync(projectPlanning);
            await context.SaveChangesAsync();
            
            var storyGeneration = EntityBuilders.BuildStoryGeneration(projectPlanningId: projectPlanning.Id);
            await context.StoryGenerations.AddAsync(storyGeneration);
            await context.SaveChangesAsync();
            
            var review = EntityBuilders.BuildReview(storyGenerationId: storyGeneration.Id);
            var addedEntity = await repository.AddAsync(review, CancellationToken.None);

            // Act
            var result = await repository.GetByWorkflowEntityIdAsync(storyGeneration.Id, "StoryGeneration", CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result?.Id.Should().Be(addedEntity?.Id);
            
            context.Dispose();
        }

        [Fact]
        public async Task GetByWorkflowEntityIdAsync_WithValidPromptGeneration_ReturnsEntity()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContext();
            var repository = new ReviewRepository(context);
            var project = EntityBuilders.BuildProject();
            await context.Projects.AddAsync(project);
            await context.SaveChangesAsync();
            
            var requirementsAnalysis = EntityBuilders.BuildRequirementsAnalysis(projectId: project.Id);
            await context.RequirementsAnalyses.AddAsync(requirementsAnalysis);
            await context.SaveChangesAsync();
            
            var projectPlanning = EntityBuilders.BuildProjectPlanning(requirementsAnalysisId: requirementsAnalysis.Id);
            await context.ProjectPlannings.AddAsync(projectPlanning);
            await context.SaveChangesAsync();
            
            var storyGeneration = EntityBuilders.BuildStoryGeneration(projectPlanningId: projectPlanning.Id);
            await context.StoryGenerations.AddAsync(storyGeneration);
            await context.SaveChangesAsync();
            
            var userStory = EntityBuilders.BuildUserStory();
            userStory.StoryGenerationId = storyGeneration.Id;
            await context.UserStories.AddAsync(userStory);
            await context.SaveChangesAsync();
            
            var promptGeneration = EntityBuilders.BuildPromptGeneration(userStoryId: userStory.Id);
            await context.PromptGenerations.AddAsync(promptGeneration);
            await context.SaveChangesAsync();
            
            var review = EntityBuilders.BuildReview(promptGenerationId: promptGeneration.Id);
            var addedEntity = await repository.AddAsync(review, CancellationToken.None);

            // Act
            var result = await repository.GetByWorkflowEntityIdAsync(promptGeneration.Id, "PromptGeneration", CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result?.Id.Should().Be(addedEntity?.Id);
            
            context.Dispose();
        }

        [Fact]
        public async Task GetByWorkflowEntityIdAsync_WithInvalidEntityId_ReturnsNull()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContext();
            var repository = new ReviewRepository(context);

            // Act
            var result = await repository.GetByWorkflowEntityIdAsync(999, "RequirementsAnalysis", CancellationToken.None);

            // Assert
            result.Should().BeNull();
            
            context.Dispose();
        }

        [Fact]
        public async Task GetByWorkflowEntityIdAsync_WithInvalidEntityType_ReturnsNull()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContext();
            var repository = new ReviewRepository(context);

            // Act
            var result = await repository.GetByWorkflowEntityIdAsync(1, "InvalidType", CancellationToken.None);

            // Assert
            result.Should().BeNull();
            
            context.Dispose();
        }

        [Fact]
        public async Task GetPendingReviewsAsync_ReturnsPendingReviews()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContext();
            var repository = new ReviewRepository(context);
            var pendingReview = EntityBuilders.BuildReview(status: ReviewStatus.Pending);
            var completedReview = EntityBuilders.BuildReview(status: ReviewStatus.Approved);
            await repository.AddAsync(pendingReview, CancellationToken.None);
            await repository.AddAsync(completedReview, CancellationToken.None);

            // Act
            var result = await repository.GetPendingReviewsAsync(CancellationToken.None);

            // Assert
            result.Should().ContainSingle(r => r.Id == pendingReview.Id);
            result.Should().NotContain(r => r.Id == completedReview.Id);
            
            context.Dispose();
        }

        [Fact]
        public async Task GetReviewsByServiceAsync_ReturnsReviewsByService()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContext();
            var repository = new ReviewRepository(context);
            var testServiceReview = EntityBuilders.BuildReview(serviceName: "TestService");
            var otherServiceReview = EntityBuilders.BuildReview(serviceName: "OtherService");
            await repository.AddAsync(testServiceReview, CancellationToken.None);
            await repository.AddAsync(otherServiceReview, CancellationToken.None);

            // Act
            var result = await repository.GetReviewsByServiceAsync("TestService", CancellationToken.None);

            // Assert
            result.Should().ContainSingle(r => r.Id == testServiceReview.Id);
            result.Should().NotContain(r => r.Id == otherServiceReview.Id);
            
            context.Dispose();
        }

        [Fact]
        public async Task GetReviewsByPipelineStageAsync_ReturnsReviewsByPipelineStage()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContext();
            var repository = new ReviewRepository(context);
            var testStageReview = EntityBuilders.BuildReview(pipelineStage: "TestStage");
            var otherStageReview = EntityBuilders.BuildReview(pipelineStage: "OtherStage");
            await repository.AddAsync(testStageReview, CancellationToken.None);
            await repository.AddAsync(otherStageReview, CancellationToken.None);

            // Act
            var result = await repository.GetReviewsByPipelineStageAsync("TestStage", CancellationToken.None);

            // Assert
            result.Should().ContainSingle(r => r.Id == testStageReview.Id);
            result.Should().NotContain(r => r.Id == otherStageReview.Id);
            
            context.Dispose();
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllEntities()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContext();
            var repository = new ReviewRepository(context);
            var review1 = EntityBuilders.BuildReview(content: "Review 1");
            var review2 = EntityBuilders.BuildReview(content: "Review 2");
            await repository.AddAsync(review1, CancellationToken.None);
            await repository.AddAsync(review2, CancellationToken.None);

            // Act
            var result = await repository.GetAllAsync(CancellationToken.None);

            // Assert
            result.Should().HaveCount(2);
            var reviews = result.ToList();
            reviews.Should().ContainSingle(r => r.Content == "Review 1");
            reviews.Should().ContainSingle(r => r.Content == "Review 2");
            
            context.Dispose();
        }

        [Fact]
        public async Task UpdateAsync_UpdatesEntity()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContext();
            var repository = new ReviewRepository(context);
            var review = EntityBuilders.BuildReview(content: "Original Content");
            var addedEntity = await repository.AddAsync(review, CancellationToken.None);
            addedEntity.Content = "Updated Content";

            // Act
            await repository.UpdateAsync(addedEntity, CancellationToken.None);

            // Assert
            var updatedEntity = await context.Reviews.FindAsync(addedEntity.Id);
            updatedEntity.Should().NotBeNull();
            updatedEntity?.Content.Should().Be("Updated Content");
            
            context.Dispose();
        }

        [Fact]
        public async Task DeleteAsync_RemovesEntity()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContext();
            var repository = new ReviewRepository(context);
            var review = EntityBuilders.BuildReview();
            var addedEntity = await repository.AddAsync(review, CancellationToken.None);

            // Act
            await repository.DeleteAsync(addedEntity.Id, CancellationToken.None);

            // Assert
            var deletedEntity = await context.Reviews.FindAsync(addedEntity.Id);
            deletedEntity.Should().BeNull();
            
            context.Dispose();
        }

        // The Dispose method is not needed when implementing IAsyncLifetime
        // Use DisposeAsync instead which is already implemented above
    }
}