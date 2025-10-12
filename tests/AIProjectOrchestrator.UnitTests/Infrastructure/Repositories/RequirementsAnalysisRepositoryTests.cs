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
    public class RequirementsAnalysisRepositoryTests : IAsyncLifetime
    {
        private readonly IRequirementsAnalysisRepository _repository;
        private readonly AppDbContext _context;

        public RequirementsAnalysisRepositoryTests()
        {
            _context = TestDbContextFactory.CreateContext();
            _repository = new RequirementsAnalysisRepository(_context);
        }

        [Fact]
        public async Task AddAsync_PersistsEntity()
        {
            // Arrange
            var project = EntityBuilders.BuildProject();
            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();
            
            var requirementsAnalysis = EntityBuilders.BuildRequirementsAnalysis(projectId: project.Id);

            // Act
            var result = await _repository.AddAsync(requirementsAnalysis, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().BeGreaterThan(0);
            result.ProjectId.Should().Be(project.Id);
            
            var savedEntity = await _context.RequirementsAnalyses.FindAsync(result.Id);
            savedEntity.Should().NotBeNull();
            savedEntity?.AnalysisId.Should().Be(requirementsAnalysis.AnalysisId);
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsEntity()
        {
            // Arrange
            var project = EntityBuilders.BuildProject();
            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();
            
            var requirementsAnalysis = EntityBuilders.BuildRequirementsAnalysis(projectId: project.Id);
            var addedEntity = await _repository.AddAsync(requirementsAnalysis, CancellationToken.None);

            // Act
            var result = await _repository.GetByIdAsync(addedEntity.Id, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result?.Id.Should().Be(addedEntity.Id);
            result?.AnalysisId.Should().Be(requirementsAnalysis.AnalysisId);
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
        public async Task GetByAnalysisIdAsync_WithValidId_ReturnsEntity()
        {
            // Arrange
            var project = EntityBuilders.BuildProject();
            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();
            
            var requirementsAnalysis = EntityBuilders.BuildRequirementsAnalysis(projectId: project.Id, analysisId: "test-analysis-123");
            await _repository.AddAsync(requirementsAnalysis, CancellationToken.None);

            // Act
            var result = await _repository.GetByAnalysisIdAsync("test-analysis-123", CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result?.AnalysisId.Should().Be("test-analysis-123");
        }

        [Fact]
        public async Task GetByAnalysisIdAsync_WithInvalidId_ReturnsNull()
        {
            // Act
            var result = await _repository.GetByAnalysisIdAsync("invalid-id", CancellationToken.None);

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
            await _repository.AddAsync(requirementsAnalysis, CancellationToken.None);

            // Act
            var result = await _repository.GetByProjectIdAsync(project.Id, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result?.ProjectId.Should().Be(project.Id);
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
        public async Task GetEntityIdByAnalysisIdAsync_WithValidId_ReturnsId()
        {
            // Arrange
            var project = EntityBuilders.BuildProject();
            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();
            
            var requirementsAnalysis = EntityBuilders.BuildRequirementsAnalysis(projectId: project.Id, analysisId: "test-analysis-456");
            var addedEntity = await _repository.AddAsync(requirementsAnalysis, CancellationToken.None);

            // Act
            var result = await _repository.GetEntityIdByAnalysisIdAsync("test-analysis-456", CancellationToken.None);

            // Assert
            result.Should().Be(addedEntity?.Id);
        }

        [Fact]
        public async Task GetEntityIdByAnalysisIdAsync_WithInvalidId_ReturnsNull()
        {
            // Act
            var result = await _repository.GetEntityIdByAnalysisIdAsync("invalid-id", CancellationToken.None);

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
            
            var analysis1 = EntityBuilders.BuildRequirementsAnalysis(projectId: project.Id, analysisId: "analysis-1");
            var analysis2 = EntityBuilders.BuildRequirementsAnalysis(projectId: project.Id, analysisId: "analysis-2");
            await _repository.AddAsync(analysis1, CancellationToken.None);
            await _repository.AddAsync(analysis2, CancellationToken.None);

            // Act
            var result = await _repository.GetAllAsync(CancellationToken.None);

            // Assert
            result.Should().HaveCount(2);
            var analyses = result.ToList();
            analyses.Should().ContainSingle(ra => ra.AnalysisId == "analysis-1");
            analyses.Should().ContainSingle(ra => ra.AnalysisId == "analysis-2");
        }

        [Fact]
        public async Task UpdateAsync_UpdatesEntity()
        {
            // Arrange
            var project = EntityBuilders.BuildProject();
            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();
            
            var requirementsAnalysis = EntityBuilders.BuildRequirementsAnalysis(projectId: project.Id);
            var addedEntity = await _repository.AddAsync(requirementsAnalysis, CancellationToken.None);
            addedEntity.Content = "Updated Content";

            // Act
            await _repository.UpdateAsync(addedEntity, CancellationToken.None);

            // Assert
            var updatedEntity = await _context.RequirementsAnalyses.FindAsync(addedEntity.Id);
            updatedEntity.Should().NotBeNull();
            updatedEntity?.Content.Should().Be("Updated Content");
        }

        [Fact]
        public async Task DeleteAsync_RemovesEntity()
        {
            // Arrange
            var project = EntityBuilders.BuildProject();
            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();
            
            var requirementsAnalysis = EntityBuilders.BuildRequirementsAnalysis(projectId: project.Id);
            var addedEntity = await _repository.AddAsync(requirementsAnalysis, CancellationToken.None);

            // Act
            await _repository.DeleteAsync(addedEntity.Id, CancellationToken.None);

            // Assert
            var deletedEntity = await _context.RequirementsAnalyses.FindAsync(addedEntity.Id);
            deletedEntity.Should().BeNull();
        }

        public async Task InitializeAsync()
        {
            // Initialization logic if needed
            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            _context?.Dispose();
            await Task.CompletedTask;
        }
    }
}