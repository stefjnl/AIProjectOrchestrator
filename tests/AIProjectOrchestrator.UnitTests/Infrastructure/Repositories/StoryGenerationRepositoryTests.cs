using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Interfaces;
using AIProjectOrchestrator.Domain.Models.Stories;
using AIProjectOrchestrator.Infrastructure.Repositories;
using AIProjectOrchestrator.UnitTests.Domain.Builders;
using AIProjectOrchestrator.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AIProjectOrchestrator.UnitTests.Infrastructure.Repositories
{
    public class StoryGenerationRepositoryTests : IAsyncLifetime
    {
        private Mock<ILogger<StoryGenerationRepository>> CreateLoggerMock()
        {
            return new Mock<ILogger<StoryGenerationRepository>>();
        }

        [Fact]
        public async Task AddAsync_PersistsEntity()
        {
            // Arrange
            var loggerMock = CreateLoggerMock();
            var repository = CreateRepository(Context, loggerMock);
            
            var project = EntityBuilders.BuildProject();
            await Context.Projects.AddAsync(project);
            await Context.SaveChangesAsync();
            
            var requirementsAnalysis = EntityBuilders.BuildRequirementsAnalysis(projectId: project.Id);
            await Context.RequirementsAnalyses.AddAsync(requirementsAnalysis);
            await Context.SaveChangesAsync();
            
            var projectPlanning = EntityBuilders.BuildProjectPlanning(requirementsAnalysisId: requirementsAnalysis.Id);
            await Context.ProjectPlannings.AddAsync(projectPlanning);
            await Context.SaveChangesAsync();
            
            var storyGeneration = EntityBuilders.BuildStoryGeneration(projectPlanningId: projectPlanning.Id);

            // Act
            var result = await repository.AddAsync(storyGeneration, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().BeGreaterThan(0);
            result.ProjectPlanningId.Should().Be(projectPlanning.Id);
            
            var savedEntity = await Context.StoryGenerations.FindAsync(result.Id);
            savedEntity.Should().NotBeNull();
            savedEntity?.GenerationId.Should().Be(storyGeneration.GenerationId);
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsEntity()
        {
            // Arrange
            var context = CreateNewContext();
            var loggerMock = CreateLoggerMock();
            var repository = CreateRepository(context, loggerMock);
            
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
            var addedEntity = await repository.AddAsync(storyGeneration, CancellationToken.None);

            // Act
            var result = await repository.GetByIdAsync(addedEntity.Id, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result?.Id.Should().Be(addedEntity.Id);
            result?.GenerationId.Should().Be(storyGeneration.GenerationId);
            
            context.Dispose();
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var context = CreateNewContext();
            var loggerMock = CreateLoggerMock();
            var repository = CreateRepository(context, loggerMock);

            // Act
            var result = await repository.GetByIdAsync(999, CancellationToken.None);

            // Assert
            result.Should().BeNull();
            
            context.Dispose();
        }

        [Fact]
        public async Task GetByGenerationIdAsync_WithValidId_ReturnsEntity()
        {
            // Arrange
            var context = CreateNewContext();
            var loggerMock = CreateLoggerMock();
            var repository = CreateRepository(context, loggerMock);
            
            var project = EntityBuilders.BuildProject();
            await context.Projects.AddAsync(project);
            await context.SaveChangesAsync();
            
            var requirementsAnalysis = EntityBuilders.BuildRequirementsAnalysis(projectId: project.Id);
            await context.RequirementsAnalyses.AddAsync(requirementsAnalysis);
            await context.SaveChangesAsync();
            
            var projectPlanning = EntityBuilders.BuildProjectPlanning(requirementsAnalysisId: requirementsAnalysis.Id);
            await context.ProjectPlannings.AddAsync(projectPlanning);
            await context.SaveChangesAsync();
            
            var testGenerationId = Guid.NewGuid().ToString();
            var storyGeneration = EntityBuilders.BuildStoryGeneration(projectPlanningId: projectPlanning.Id, generationId: testGenerationId);
            await repository.AddAsync(storyGeneration, CancellationToken.None);

            // Act
            var result = await repository.GetByGenerationIdAsync(testGenerationId, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result?.GenerationId.Should().Be(testGenerationId);
            
            context.Dispose();
        }

        [Fact]
        public async Task GetByGenerationIdAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var context = CreateNewContext();
            var loggerMock = CreateLoggerMock();
            var repository = CreateRepository(context, loggerMock);

            // Act
            var result = await repository.GetByGenerationIdAsync(Guid.NewGuid().ToString(), CancellationToken.None);

            // Assert
            result.Should().BeNull();
            
            context.Dispose();
        }

        [Fact]
        public async Task GetByProjectPlanningIdAsync_WithValidId_ReturnsEntity()
        {
            // Arrange
            var context = CreateNewContext();
            var loggerMock = CreateLoggerMock();
            var repository = CreateRepository(context, loggerMock);
            
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
            await repository.AddAsync(storyGeneration, CancellationToken.None);

            // Act
            var result = await repository.GetByProjectPlanningIdAsync(projectPlanning.Id, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result?.ProjectPlanningId.Should().Be(projectPlanning.Id);
            
            context.Dispose();
        }

        [Fact]
        public async Task GetByProjectPlanningIdAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var context = CreateNewContext();
            var loggerMock = CreateLoggerMock();
            var repository = CreateRepository(context, loggerMock);

            // Act
            var result = await repository.GetByProjectPlanningIdAsync(999, CancellationToken.None);

            // Assert
            result.Should().BeNull();
            
            context.Dispose();
        }

        [Fact]
        public async Task GetByProjectIdAsync_WithValidId_ReturnsEntities()
        {
            // Arrange
            var context = CreateNewContext();
            var loggerMock = CreateLoggerMock();
            var repository = CreateRepository(context, loggerMock);
            
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
            await repository.AddAsync(storyGeneration, CancellationToken.None);

            // Act
            var result = await repository.GetByProjectIdAsync(project.Id, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            var storyGenerations = result.ToList();
            storyGenerations.Should().HaveCount(1);
            storyGenerations.First()?.ProjectPlanningId.Should().Be(projectPlanning.Id);
            
            context.Dispose();
        }

        [Fact]
        public async Task GetByProjectIdAsync_WithInvalidId_ReturnsEmptyCollection()
        {
            // Arrange
            var context = CreateNewContext();
            var loggerMock = CreateLoggerMock();
            var repository = CreateRepository(context, loggerMock);

            // Act
            var result = await repository.GetByProjectIdAsync(999, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
            
            context.Dispose();
        }

        [Fact]
        public async Task GetStoryByIdAsync_WithValidId_ReturnsEntity()
        {
            // Arrange
            var context = CreateNewContext();
            var loggerMock = CreateLoggerMock();
            var repository = CreateRepository(context, loggerMock);
            
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

            // Act
            var result = await repository.GetStoryByIdAsync(userStory.Id, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result?.Id.Should().Be(userStory.Id);
            
            context.Dispose();
        }

        [Fact]
        public async Task GetStoryByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var context = CreateNewContext();
            var loggerMock = CreateLoggerMock();
            var repository = CreateRepository(context, loggerMock);

            // Act
            var result = await repository.GetStoryByIdAsync(Guid.NewGuid(), CancellationToken.None);

            // Assert
            result.Should().BeNull();
            
            context.Dispose();
        }

        [Fact]
        public async Task UpdateStoryAsync_UpdatesEntity()
        {
            // Arrange
            var context = CreateNewContext();
            var loggerMock = CreateLoggerMock();
            var repository = CreateRepository(context, loggerMock);
            
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
            
            userStory.Title = "Updated Title";

            // Act
            await repository.UpdateStoryAsync(userStory, CancellationToken.None);

            // Assert
            var updatedEntity = await context.UserStories.FindAsync(userStory.Id);
            updatedEntity.Should().NotBeNull();
            updatedEntity?.Title.Should().Be("Updated Title");
            
            context.Dispose();
        }

        [Fact]
        public async Task GetStoriesByGenerationIdAsync_WithValidId_ReturnsEntities()
        {
            // Arrange
            var context = CreateNewContext();
            var loggerMock = CreateLoggerMock();
            var repository = CreateRepository(context, loggerMock);
            
            var project = EntityBuilders.BuildProject();
            await context.Projects.AddAsync(project);
            await context.SaveChangesAsync();
            
            var requirementsAnalysis = EntityBuilders.BuildRequirementsAnalysis(projectId: project.Id);
            await context.RequirementsAnalyses.AddAsync(requirementsAnalysis);
            await context.SaveChangesAsync();
            
            var projectPlanning = EntityBuilders.BuildProjectPlanning(requirementsAnalysisId: requirementsAnalysis.Id);
            await context.ProjectPlannings.AddAsync(projectPlanning);
            await context.SaveChangesAsync();
            
            var storyGeneration = EntityBuilders.BuildStoryGeneration(projectPlanningId: projectPlanning.Id, generationId: Guid.NewGuid().ToString());
            await context.StoryGenerations.AddAsync(storyGeneration);
            await context.SaveChangesAsync();
            
            var userStory = EntityBuilders.BuildUserStory();
            userStory.StoryGenerationId = storyGeneration.Id;
            await context.UserStories.AddAsync(userStory);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetStoriesByGenerationIdAsync(Guid.Parse(storyGeneration.GenerationId), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result?.Should().ContainSingle(s => s.Id == userStory.Id);
            
            context.Dispose();
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllEntities()
        {
            // Arrange
            var context = CreateNewContext();
            var loggerMock = CreateLoggerMock();
            var repository = CreateRepository(context, loggerMock);
            
            var project = EntityBuilders.BuildProject();
            await context.Projects.AddAsync(project);
            await context.SaveChangesAsync();
            
            var requirementsAnalysis = EntityBuilders.BuildRequirementsAnalysis(projectId: project.Id);
            await context.RequirementsAnalyses.AddAsync(requirementsAnalysis);
            await context.SaveChangesAsync();
            
            var projectPlanning = EntityBuilders.BuildProjectPlanning(requirementsAnalysisId: requirementsAnalysis.Id);
            await context.ProjectPlannings.AddAsync(projectPlanning);
            await context.SaveChangesAsync();
            
            var generation1 = EntityBuilders.BuildStoryGeneration(projectPlanningId: projectPlanning.Id, generationId: Guid.NewGuid().ToString());
            var generation2 = EntityBuilders.BuildStoryGeneration(projectPlanningId: projectPlanning.Id, generationId: Guid.NewGuid().ToString());
            await repository.AddAsync(generation1, CancellationToken.None);
            await repository.AddAsync(generation2, CancellationToken.None);

            // Act
            var result = await repository.GetAllAsync(CancellationToken.None);

            // Assert
            result.Should().HaveCount(2);
            var generations = result.ToList();
            generations.Select(sg => sg.GenerationId).Should().HaveCount(2).And.Contain(generations.Select(sg => sg.GenerationId));
            
            context.Dispose();
        }

        [Fact]
        public async Task DeleteAsync_RemovesEntity()
        {
            // Arrange
            var context = CreateNewContext();
            var loggerMock = CreateLoggerMock();
            var repository = CreateRepository(context, loggerMock);
            
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
            var addedEntity = await repository.AddAsync(storyGeneration, CancellationToken.None);

            // Act
            await repository.DeleteAsync(addedEntity.Id, CancellationToken.None);

            // Assert
            var deletedEntity = await context.StoryGenerations.FindAsync(addedEntity.Id);
            deletedEntity.Should().BeNull();
            
            context.Dispose();
        }

        private AppDbContext CreateNewContext()
        {
            return TestDbContextFactory.CreateContext();
        }

        public AppDbContext Context { get; private set; } = null!;
        
        public async Task InitializeAsync()
        {
            Context = TestDbContextFactory.CreateContext();
            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            Context?.Dispose();
            await Task.CompletedTask;
        }
        
        private IStoryGenerationRepository CreateRepository(AppDbContext? context = null, Mock<ILogger<StoryGenerationRepository>>? loggerMock = null)
        {
            context ??= Context;
            loggerMock ??= CreateLoggerMock();
            return new StoryGenerationRepository(context, loggerMock.Object);
        }
    }
}