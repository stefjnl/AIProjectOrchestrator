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
    public class RepositoryTests : IAsyncLifetime
    {
        private readonly IFullRepository<Project, int> _repository;
        private readonly AppDbContext _context;

        public RepositoryTests()
        {
            _context = TestDbContextFactory.CreateContext();
            _repository = new Repository<Project>(_context);
        }

        [Fact]
        public async Task Add_PersistsEntity()
        {
            // Arrange
            var project = EntityBuilders.BuildProject();

            // Act
            var result = await _repository.AddAsync(project, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().BeGreaterThan(0);
            var savedEntity = await _context.Projects.FindAsync(result.Id);
            savedEntity.Should().NotBeNull();
            savedEntity?.Name.Should().Be(project.Name);
        }

        [Fact]
        public async Task GetById_WithValidId_ReturnsEntity()
        {
            // Arrange
            var project = EntityBuilders.BuildProject();
            var addedEntity = await _repository.AddAsync(project, CancellationToken.None);

            // Act
            var result = await _repository.GetByIdAsync(addedEntity.Id, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result?.Id.Should().Be(addedEntity.Id);
            result?.Name.Should().Be(project.Name);
        }

        [Fact]
        public async Task GetById_WithInvalidId_ReturnsNull()
        {
            // Act
            var result = await _repository.GetByIdAsync(999, CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        // GetByStringIdAsync method removed - it had performance issues:
        // 1. Used reflection which doesn't translate to SQL
        // 2. Loaded entire table into memory with ToListAsync()
        // 3. Never used in production code
        // Tests were already skipped due to implementation issues

        [Fact]
        public async Task GetAll_ReturnsAllEntities()
        {
            // Arrange
            var project1 = EntityBuilders.BuildProject(name: "Project 1");
            var project2 = EntityBuilders.BuildProject(name: "Project 2");
            await _repository.AddAsync(project1, CancellationToken.None);
            await _repository.AddAsync(project2, CancellationToken.None);

            // Act
            var result = await _repository.GetAllAsync(CancellationToken.None);

            // Assert
            result.Should().HaveCount(2);
            var projects = result.ToList();
            projects.Should().ContainSingle(p => p.Name == "Project 1");
            projects.Should().ContainSingle(p => p.Name == "Project 2");
        }

        [Fact]
        public async Task Update_ModifiesFieldsAndPersistsChanges()
        {
            // Arrange
            var project = EntityBuilders.BuildProject(name: "Original Name");
            var addedEntity = await _repository.AddAsync(project, CancellationToken.None);
            addedEntity.Name = "Updated Name";

            // Act
            await _repository.UpdateAsync(addedEntity, CancellationToken.None);

            // Assert
            var updatedEntity = await _context.Projects.FindAsync(addedEntity.Id);
            updatedEntity.Should().NotBeNull();
            updatedEntity?.Name.Should().Be("Updated Name");
        }

        [Fact]
        public async Task Delete_RemovesEntity()
        {
            // Arrange
            var project = EntityBuilders.BuildProject();
            var addedEntity = await _repository.AddAsync(project, CancellationToken.None);

            // Act
            await _repository.DeleteAsync(addedEntity.Id, CancellationToken.None);

            // Assert
            var deletedEntity = await _context.Projects.FindAsync(addedEntity.Id);
            deletedEntity.Should().BeNull();
        }

        [Fact]
        public async Task Delete_WithInvalidId_DoesNotThrow()
        {
            // Act & Assert
            var action = async () => await _repository.DeleteAsync(999, CancellationToken.None);
            await action.Should().NotThrowAsync();
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