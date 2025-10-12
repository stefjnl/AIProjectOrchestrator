using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Interfaces;
using AIProjectOrchestrator.Domain.Models.PromptGeneration;
using AIProjectOrchestrator.Infrastructure.Repositories;
using AIProjectOrchestrator.UnitTests.Domain.Builders;
using AIProjectOrchestrator.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AIProjectOrchestrator.UnitTests.Infrastructure.Repositories
{
    public class PromptGenerationRepositoryTests : IAsyncLifetime
    {
        private readonly AppDbContext _sharedContext;

        public PromptGenerationRepositoryTests()
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
            var repository = new PromptGenerationRepository(context);
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

            // Act
            var result = await repository.AddAsync(promptGeneration, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().BeGreaterThan(0);
            result.UserStoryId.Should().Be(userStory.Id);
            
            var savedEntity = await context.PromptGenerations.FindAsync(result.Id);
            savedEntity.Should().NotBeNull();
            savedEntity?.PromptId.Should().Be(promptGeneration.PromptId);
            
            context.Dispose();
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsEntity()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContext();
            var repository = new PromptGenerationRepository(context);
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
            var addedEntity = await repository.AddAsync(promptGeneration, CancellationToken.None);

            // Act
            var result = await repository.GetByIdAsync(addedEntity.Id, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result?.Id.Should().Be(addedEntity.Id);
            result?.PromptId.Should().Be(promptGeneration.PromptId);
            
            context.Dispose();
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContext();
            var repository = new PromptGenerationRepository(context);

            // Act
            var result = await repository.GetByIdAsync(999, CancellationToken.None);

            // Assert
            result.Should().BeNull();
            
            context.Dispose();
        }

        [Fact]
        public async Task GetByPromptIdAsync_WithValidId_ReturnsEntity()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContext();
            var repository = new PromptGenerationRepository(context);
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
            
            var promptGeneration = EntityBuilders.BuildPromptGeneration(userStoryId: userStory.Id, promptId: "test-prompt-123");
            await repository.AddAsync(promptGeneration, CancellationToken.None);

            // Act
            var result = await repository.GetByPromptIdAsync("test-prompt-123", CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result?.PromptId.Should().Be("test-prompt-123");
            
            context.Dispose();
        }

        [Fact]
        public async Task GetByPromptIdAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContext();
            var repository = new PromptGenerationRepository(context);

            // Act
            var result = await repository.GetByPromptIdAsync("invalid-id", CancellationToken.None);

            // Assert
            result.Should().BeNull();
            
            context.Dispose();
        }

        [Fact]
        public async Task GetByUserStoryIdAndIndexAsync_WithValidId_ReturnsEntity()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContext();
            var repository = new PromptGenerationRepository(context);
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
            
            var promptGeneration = EntityBuilders.BuildPromptGeneration(userStoryId: userStory.Id, storyIndex: 1);
            await repository.AddAsync(promptGeneration, CancellationToken.None);

            // Act
            var result = await repository.GetByUserStoryIdAndIndexAsync(userStory.Id, 1, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result?.UserStoryId.Should().Be(userStory.Id);
            result?.StoryIndex.Should().Be(1);
            
            context.Dispose();
        }

        [Fact]
        public async Task GetByUserStoryIdAndIndexAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContext();
            var repository = new PromptGenerationRepository(context);

            // Act
            var result = await repository.GetByUserStoryIdAndIndexAsync(Guid.NewGuid(), 1, CancellationToken.None);

            // Assert
            result.Should().BeNull();
            
            context.Dispose();
        }

        [Fact]
        public async Task GetByUserStoryIdAsync_WithValidId_ReturnsEntities()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContext();
            var repository = new PromptGenerationRepository(context);
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
            await repository.AddAsync(promptGeneration, CancellationToken.None);

            // Act
            var result = await repository.GetByUserStoryIdAsync(userStory.Id, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            var promptGenerations = result.ToList();
            promptGenerations.Should().HaveCount(1);
            promptGenerations.First()?.UserStoryId.Should().Be(userStory.Id);
            
            context.Dispose();
        }

        [Fact]
        public async Task GetByUserStoryIdAsync_WithInvalidId_ReturnsEmptyCollection()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContext();
            var repository = new PromptGenerationRepository(context);

            // Act
            var result = await repository.GetByUserStoryIdAsync(Guid.NewGuid(), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
            
            context.Dispose();
        }

        [Fact]
        public async Task GetByProjectIdAsync_WithValidId_ReturnsEntities()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContext();
            var repository = new PromptGenerationRepository(context);
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
            await repository.AddAsync(promptGeneration, CancellationToken.None);

            // Act
            var result = await repository.GetByProjectIdAsync(project.Id, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            var promptGenerations = result.ToList();
            promptGenerations.Should().HaveCount(1);
            promptGenerations.First()?.UserStoryId.Should().Be(userStory.Id);
            
            context.Dispose();
        }

        [Fact]
        public async Task GetByProjectIdAsync_WithInvalidId_ReturnsEmptyCollection()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContext();
            var repository = new PromptGenerationRepository(context);

            // Act
            var result = await repository.GetByProjectIdAsync(999, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
            
            context.Dispose();
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllEntities()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContext();
            var repository = new PromptGenerationRepository(context);
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
            
            var generation1 = EntityBuilders.BuildPromptGeneration(userStoryId: userStory.Id, promptId: "prompt-1");
            var generation2 = EntityBuilders.BuildPromptGeneration(userStoryId: userStory.Id, promptId: "prompt-2");
            await repository.AddAsync(generation1, CancellationToken.None);
            await repository.AddAsync(generation2, CancellationToken.None);

            // Act
            var result = await repository.GetAllAsync(CancellationToken.None);

            // Assert
            result.Should().HaveCount(2);
            var generations = result.ToList();
            generations.Should().ContainSingle(pg => pg.PromptId == "prompt-1");
            generations.Should().ContainSingle(pg => pg.PromptId == "prompt-2");
            
            context.Dispose();
        }

        [Fact]
        public async Task UpdateAsync_UpdatesEntity()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContext();
            var repository = new PromptGenerationRepository(context);
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
            var addedEntity = await repository.AddAsync(promptGeneration, CancellationToken.None);
            addedEntity.Content = "Updated Content";

            // Act
            await repository.UpdateAsync(addedEntity, CancellationToken.None);

            // Assert
            var updatedEntity = await context.PromptGenerations.FindAsync(addedEntity.Id);
            updatedEntity.Should().NotBeNull();
            updatedEntity?.Content.Should().Be("Updated Content");
            
            context.Dispose();
        }

        [Fact]
        public async Task DeleteAsync_RemovesEntity()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContext();
            var repository = new PromptGenerationRepository(context);
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
            var addedEntity = await repository.AddAsync(promptGeneration, CancellationToken.None);

            // Act
            await repository.DeleteAsync(addedEntity.Id, CancellationToken.None);

            // Assert
            var deletedEntity = await context.PromptGenerations.FindAsync(addedEntity.Id);
            deletedEntity.Should().BeNull();
            
            context.Dispose();
        }

        // The Dispose method is not needed when implementing IAsyncLifetime
        // Use DisposeAsync instead which is already implemented above
    }
}