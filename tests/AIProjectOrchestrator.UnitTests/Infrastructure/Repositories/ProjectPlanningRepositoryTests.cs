using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Interfaces;
using AIProjectOrchestrator.Infrastructure.Repositories;
using AIProjectOrchestrator.UnitTests.Domain.Builders;
using AIProjectOrchestrator.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AIProjectOrchestrator.UnitTests.Infrastructure.Repositories
{
    public class ProjectPlanningRepositoryTests : IDisposable
    {
        private readonly IProjectPlanningRepository _repository;
        private readonly AppDbContext _context;

        public ProjectPlanningRepositoryTests()
        {
            _context = TestDbContextFactory.CreateContext();
            _repository = new ProjectPlanningRepository(_context);
        }

        [Fact]
        public async Task AddAsync_PersistsEntity()
        {
            // Arrange
            var project = EntityBuilders.BuildProject();
            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();
            
            var requirementsAnalysis = EntityBuilders.BuildRequirementsAnalysis(projectId: project.Id);
            await _context.RequirementsAnalyses.AddAsync(requirementsAnalysis);
            await _context.SaveChangesAsync();
            
            var projectPlanning = EntityBuilders.BuildProjectPlanning(requirementsAnalysisId: requirementsAnalysis.Id);

            // Act
            var result = await _repository.AddAsync(projectPlanning, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().BeGreaterThan(0);
            result.RequirementsAnalysisId.Should().Be(requirementsAnalysis.Id);
            
            var savedEntity = await _context.ProjectPlannings.FindAsync(result.Id);
            savedEntity.Should().NotBeNull();
            savedEntity.PlanningId.Should().Be(projectPlanning.PlanningId);
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsEntity()
        {
            // Arrange
            var project = EntityBuilders.BuildProject();
            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();
            
            var requirementsAnalysis = EntityBuilders.BuildRequirementsAnalysis(projectId: project.Id);
            await _context.RequirementsAnalyses.AddAsync(requirementsAnalysis);
            await _context.SaveChangesAsync();
            
            var projectPlanning = EntityBuilders.BuildProjectPlanning(requirementsAnalysisId: requirementsAnalysis.Id);
            var addedEntity = await _repository.AddAsync(projectPlanning, CancellationToken.None);

            // Act
            var result = await _repository.GetByIdAsync(addedEntity.Id, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(addedEntity.Id);
            result.PlanningId.Should().Be(projectPlanning.PlanningId);
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Act
            var result = await _repository.GetByIdAsync(999, CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByPlanningIdAsync_WithValidId_ReturnsEntity()
        {
            // Arrange
            var project = EntityBuilders.BuildProject();
            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();
            
            var requirementsAnalysis = EntityBuilders.BuildRequirementsAnalysis(projectId: project.Id);
            await _context.RequirementsAnalyses.AddAsync(requirementsAnalysis);
            await _context.SaveChangesAsync();
            
            var projectPlanning = EntityBuilders.BuildProjectPlanning(requirementsAnalysisId: requirementsAnalysis.Id, planningId: "test-planning-123");
            await _repository.AddAsync(projectPlanning, CancellationToken.None);

            // Act
            var result = await _repository.GetByPlanningIdAsync("test-planning-123", CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.PlanningId.Should().Be("test-planning-123");
        }

        [Fact]
        public async Task GetByPlanningIdAsync_WithInvalidId_ReturnsNull()
        {
            // Act
            var result = await _repository.GetByPlanningIdAsync("invalid-id", CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByProjectIdAsync_WithValidId_ReturnsEntity()
        {
            // Arrange
            var project = EntityBuilders.BuildProject();
            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();
            
            var requirementsAnalysis = EntityBuilders.BuildRequirementsAnalysis(projectId: project.Id);
            await _context.RequirementsAnalyses.AddAsync(requirementsAnalysis);
            await _context.SaveChangesAsync();
            
            var projectPlanning = EntityBuilders.BuildProjectPlanning(requirementsAnalysisId: requirementsAnalysis.Id);
            await _repository.AddAsync(projectPlanning, CancellationToken.None);

            // Act
            var result = await _repository.GetByProjectIdAsync(project.Id, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.RequirementsAnalysisId.Should().Be(requirementsAnalysis.Id);
        }

        [Fact]
        public async Task GetByProjectIdAsync_WithInvalidId_ReturnsNull()
        {
            // Act
            var result = await _repository.GetByProjectIdAsync(999, CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllEntities()
        {
            // Arrange
            var project = EntityBuilders.BuildProject();
            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();
            
            var requirementsAnalysis = EntityBuilders.BuildRequirementsAnalysis(projectId: project.Id);
            await _context.RequirementsAnalyses.AddAsync(requirementsAnalysis);
            await _context.SaveChangesAsync();
            
            var planning1 = EntityBuilders.BuildProjectPlanning(requirementsAnalysisId: requirementsAnalysis.Id, planningId: "planning-1");
            var planning2 = EntityBuilders.BuildProjectPlanning(requirementsAnalysisId: requirementsAnalysis.Id, planningId: "planning-2");
            await _repository.AddAsync(planning1, CancellationToken.None);
            await _repository.AddAsync(planning2, CancellationToken.None);

            // Act
            var result = await _repository.GetAllAsync(CancellationToken.None);

            // Assert
            result.Should().HaveCount(2);
            var plannings = result.ToList();
            plannings.Should().ContainSingle(pp => pp.PlanningId == "planning-1");
            plannings.Should().ContainSingle(pp => pp.PlanningId == "planning-2");
        }

        [Fact]
        public async Task UpdateAsync_UpdatesEntity()
        {
            // Arrange
            var project = EntityBuilders.BuildProject();
            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();
            
            var requirementsAnalysis = EntityBuilders.BuildRequirementsAnalysis(projectId: project.Id);
            await _context.RequirementsAnalyses.AddAsync(requirementsAnalysis);
            await _context.SaveChangesAsync();
            
            var projectPlanning = EntityBuilders.BuildProjectPlanning(requirementsAnalysisId: requirementsAnalysis.Id);
            var addedEntity = await _repository.AddAsync(projectPlanning, CancellationToken.None);
            addedEntity.Content = "Updated Content";

            // Act
            await _repository.UpdateAsync(addedEntity, CancellationToken.None);

            // Assert
            var updatedEntity = await _context.ProjectPlannings.FindAsync(addedEntity.Id);
            updatedEntity.Should().NotBeNull();
            updatedEntity.Content.Should().Be("Updated Content");
        }

        [Fact]
        public async Task DeleteAsync_RemovesEntity()
        {
            // Arrange
            var project = EntityBuilders.BuildProject();
            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();
            
            var requirementsAnalysis = EntityBuilders.BuildRequirementsAnalysis(projectId: project.Id);
            await _context.RequirementsAnalyses.AddAsync(requirementsAnalysis);
            await _context.SaveChangesAsync();
            
            var projectPlanning = EntityBuilders.BuildProjectPlanning(requirementsAnalysisId: requirementsAnalysis.Id);
            var addedEntity = await _repository.AddAsync(projectPlanning, CancellationToken.None);

            // Act
            await _repository.DeleteAsync(addedEntity.Id, CancellationToken.None);

            // Assert
            var deletedEntity = await _context.ProjectPlannings.FindAsync(addedEntity.Id);
            deletedEntity.Should().BeNull();
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}