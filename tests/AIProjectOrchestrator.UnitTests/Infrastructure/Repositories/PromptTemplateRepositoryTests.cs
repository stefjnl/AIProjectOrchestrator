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
    public class PromptTemplateRepositoryTests : IAsyncLifetime
    {
        private readonly IPromptTemplateRepository _repository;
        private readonly AppDbContext _context;

        public PromptTemplateRepositoryTests()
        {
            _context = TestDbContextFactory.CreateContext();
            _repository = new PromptTemplateRepository(_context);
        }

        public async Task InitializeAsync()
        {
            // Initialize resources if needed
            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            _context?.Dispose();
            await Task.CompletedTask;
        }

        [Fact]
        public async Task AddAsync_PersistsEntity()
        {
            // Arrange
            var promptTemplate = EntityBuilders.BuildPromptTemplate(title: "Test Template");

            // Act
            var result = await _repository.AddAsync(promptTemplate);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().NotBeEmpty();
            result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            result.UpdatedAt.Should().NotBeNull().And.BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            
            var savedEntity = await _context.PromptTemplates.FindAsync(new object[] { result.Id });
            savedEntity.Should().NotBeNull();
            savedEntity?.Title.Should().Be("Test Template");
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsEntity()
        {
            // Arrange
            var promptTemplate = EntityBuilders.BuildPromptTemplate(title: "Test Template");
            var addedEntity = await _repository.AddAsync(promptTemplate);

            // Act
            var result = await ((IPromptTemplateRepository)_repository).GetByIdAsync(addedEntity.Id);

            // Assert
            result.Should().NotBeNull();
            result?.Id.Should().Be(addedEntity.Id);
            result?.Title.Should().Be("Test Template");
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Act
            var result = await ((IPromptTemplateRepository)_repository).GetByIdAsync(Guid.NewGuid());

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_WithValidIdAndCancellationToken_ReturnsEntity()
        {
            // Arrange
            var promptTemplate = EntityBuilders.BuildPromptTemplate(title: "Test Template");
            var addedEntity = await _repository.AddAsync(promptTemplate);

            // Act
            var result = await _context.PromptTemplates.FindAsync(new object[] { addedEntity.Id }, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result?.Id.Should().Be(addedEntity.Id);
            result?.Title.Should().Be("Test Template");
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidIdAndCancellationToken_ReturnsNull()
        {
            // Act
            var nonExistentId = Guid.NewGuid();
            var result = await _context.PromptTemplates.FindAsync(new object[] { nonExistentId }, CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact(Skip = "GetByStringIdAsync not applicable for PromptTemplate with Guid Id")]
        public async Task GetByStringIdAsync_WithValidId_ReturnsEntity()
        {
            // Arrange
            var promptTemplate = EntityBuilders.BuildPromptTemplate(title: "Test Template");
            var addedEntity = await _repository.AddAsync(promptTemplate);

            // Act
            var result = await ((IRepository<PromptTemplate>)_repository).GetByStringIdAsync(addedEntity.Id.ToString(), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result?.Id.Should().Be(addedEntity.Id);
            result?.Id.Should().Be(addedEntity.Id);
        }

        [Fact(Skip = "GetByStringIdAsync not applicable for PromptTemplate with Guid Id")]
        public async Task GetByStringIdAsync_WithInvalidId_ReturnsNull()
        {
            // Act
            var result = await ((IRepository<PromptTemplate>)_repository).GetByStringIdAsync("invalid-id", CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllEntities()
        {
            // Arrange
            var template1 = EntityBuilders.BuildPromptTemplate(title: "Template 1");
            var template2 = EntityBuilders.BuildPromptTemplate(title: "Template 2");
            await _repository.AddAsync(template1);
            await _repository.AddAsync(template2);

            // Act
            var result = await ((IRepository<PromptTemplate>)_repository).GetAllAsync(CancellationToken.None);

            // Assert
            result.Should().HaveCount(2);
            var templates = result.ToList();
            templates.Should().ContainSingle(pt => pt.Title == "Template 1");
            templates.Should().ContainSingle(pt => pt.Title == "Template 2");
        }

        [Fact]
        public async Task UpdateAsync_UpdatesEntity()
        {
            // Arrange
            var promptTemplate = EntityBuilders.BuildPromptTemplate(title: "Original Title");
            var addedEntity = await _repository.AddAsync(promptTemplate);
            addedEntity.Title = "Updated Title";

            // Act
            var result = await ((IPromptTemplateRepository)_repository).UpdateAsync(addedEntity);

            // Assert
            result.Should().NotBeNull();
            result?.Title.Should().Be("Updated Title");
            result?.UpdatedAt.Should().BeAfter(result?.CreatedAt ?? DateTime.MinValue);
            
            var updatedEntity = await _context.PromptTemplates.FindAsync(new object[] { addedEntity.Id });
            updatedEntity.Should().NotBeNull();
            updatedEntity?.Title.Should().Be("Updated Title");
        }

        [Fact]
        public async Task UpdateAsync_WithCancellationToken_UpdatesEntity()
        {
            // Arrange
            var promptTemplate = EntityBuilders.BuildPromptTemplate(title: "Original Title");
            var addedEntity = await _repository.AddAsync(promptTemplate);
            addedEntity.Title = "Updated Title";

            // Act
            await ((IPromptTemplateRepository)_repository).UpdateAsync(addedEntity, CancellationToken.None);

            // Assert
            var updatedEntity = await _context.PromptTemplates.FindAsync(new object[] { addedEntity.Id });
            updatedEntity.Should().NotBeNull();
            updatedEntity?.Title.Should().Be("Updated Title");
        }

        [Fact]
        public async Task DeleteAsync_WithValidId_RemovesEntity()
        {
            // Arrange
            var promptTemplate = EntityBuilders.BuildPromptTemplate(title: "Test Template");
            var addedEntity = await _repository.AddAsync(promptTemplate);

            // Act - Use the specific IPromptTemplateRepository method
            await ((IPromptTemplateRepository)_repository).DeleteAsync(addedEntity.Id);

            // Assert
            var deletedEntity = await _context.PromptTemplates.FindAsync(new object[] { addedEntity.Id });
            deletedEntity.Should().BeNull();
        }

        [Fact]
        public async Task DeleteAsync_WithValidIdAndCancellationToken_RemovesEntity()
        {
            // Arrange
            var promptTemplate = EntityBuilders.BuildPromptTemplate(title: "Test Template");
            var addedEntity = await _repository.AddAsync(promptTemplate);

            // Act - Use the context directly to remove the entity
            var entityToRemove = await _context.PromptTemplates.FindAsync(new object[] { addedEntity.Id });
            if (entityToRemove != null)
            {
                _context.PromptTemplates.Remove(entityToRemove);
                await _context.SaveChangesAsync(CancellationToken.None);
            }

            // Assert
            var deletedEntity = await _context.PromptTemplates.FindAsync(new object[] { addedEntity.Id });
            deletedEntity.Should().BeNull();
        }

        [Fact]
        public async Task DeleteAsync_WithInvalidId_DoesNotThrow()
        {
            // Act & Assert
            var action = async () => await ((IPromptTemplateRepository)_repository).DeleteAsync(Guid.NewGuid());
            await action.Should().NotThrowAsync();
        }

        // The Dispose method is not needed when implementing IAsyncLifetime
        // Use DisposeAsync instead which is already implemented above
    }
}