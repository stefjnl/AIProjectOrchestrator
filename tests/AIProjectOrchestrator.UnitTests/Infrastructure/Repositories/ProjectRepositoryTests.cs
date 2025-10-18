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
    public class ProjectRepositoryTests : IAsyncLifetime
    {
        private readonly IProjectRepository _repository;
        private readonly AppDbContext _context;

        public ProjectRepositoryTests()
        {
            _context = TestDbContextFactory.CreateContext();
            _repository = new ProjectRepository(_context);
        }

        [Fact]
        public async Task AddAsync_PersistsEntityWithTimestamps()
        {
            // Arrange
            var project = EntityBuilders.BuildProject();

            // Act
            var result = await _repository.AddAsync(project);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().BeGreaterThan(0);
            result.CreatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            result.UpdatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            
            var savedEntity = await _context.Projects.FindAsync(result.Id);
            savedEntity.Should().NotBeNull();
            savedEntity?.Name.Should().Be(project.Name);
        }

        [Fact]
        public async Task UpdateAsync_UpdatesEntityWithNewTimestamp()
        {
            // Arrange
            var project = EntityBuilders.BuildProject();
            var addedEntity = await _repository.AddAsync(project);
            var originalUpdatedDate = addedEntity.UpdatedDate;
            addedEntity.Name = "Updated Name";

            // Act
            await _repository.UpdateAsync(addedEntity);

            // Assert
            addedEntity.Should().NotBeNull();
            addedEntity.Name.Should().Be("Updated Name");
            addedEntity.UpdatedDate.Should().BeAfter(originalUpdatedDate);
            
            var updatedEntity = await _context.Projects.FindAsync(addedEntity.Id);
            updatedEntity.Should().NotBeNull();
            updatedEntity?.Name.Should().Be("Updated Name");
            updatedEntity?.UpdatedDate.Should().BeAfter(originalUpdatedDate);
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsEntity()
        {
            // Arrange
            var project = EntityBuilders.BuildProject();
            var addedEntity = await _repository.AddAsync(project);

            // Act
            var result = await _repository.GetByIdAsync(addedEntity.Id);

            // Assert
            result.Should().NotBeNull();
            result?.Id.Should().Be(addedEntity.Id);
            result?.Name.Should().Be(project.Name);
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Act
            var result = await _repository.GetByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        // GetByStringIdAsync method removed - it had performance issues:
        // 1. Used reflection which doesn't translate to SQL
        // 2. Loaded entire table into memory with ToListAsync()
        // 3. Never used in production code
        // Tests were already skipped due to implementation issues

        [Fact]
        public async Task GetAllAsync_ReturnsAllEntities()
        {
            // Arrange
            var project1 = EntityBuilders.BuildProject(name: "Project 1");
            var project2 = EntityBuilders.BuildProject(name: "Project 2");
            await _repository.AddAsync(project1);
            await _repository.AddAsync(project2);

            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            result.Should().HaveCount(2);
            var projects = result.ToList();
            projects.Should().ContainSingle(p => p.Name == "Project 1");
            projects.Should().ContainSingle(p => p.Name == "Project 2");
        }

        [Fact]
        public async Task UpdateAsync_WithCancellationToken_UpdatesEntity()
        {
            // Arrange
            var project = EntityBuilders.BuildProject();
            var addedEntity = await _repository.AddAsync(project);
            addedEntity.Name = "Updated Name";

            // Act
            await _repository.UpdateAsync(addedEntity, CancellationToken.None);

            // Assert
            var updatedEntity = await _context.Projects.FindAsync(addedEntity.Id);
            updatedEntity.Should().NotBeNull();
            updatedEntity?.Name.Should().Be("Updated Name");
        }

        [Fact]
        public async Task DeleteAsync_RemovesEntity()
        {
            // Arrange
            var project = EntityBuilders.BuildProject();
            var addedEntity = await _repository.AddAsync(project);

            // Act
            await _repository.DeleteAsync(addedEntity.Id);

            // Assert
            var deletedEntity = await _context.Projects.FindAsync(addedEntity.Id);
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