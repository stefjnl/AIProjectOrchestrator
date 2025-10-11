using FluentAssertions;
using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.UnitTests.Domain.Builders;

namespace AIProjectOrchestrator.UnitTests.Domain.Entities
{
    public class PromptTemplateEntityTests
    {
        [Fact]
        public void Constructor_InitializesDefaultProperties()
        {
            // Arrange & Act
            var promptTemplate = new PromptTemplate();

            // Assert
            promptTemplate.Id.Should().Be(Guid.Empty);
            promptTemplate.Title.Should().Be(string.Empty);
            promptTemplate.Content.Should().Be(string.Empty);
            promptTemplate.CreatedAt.Should().Be(default(DateTime));
            promptTemplate.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void Constructor_InitializesPropertiesWithValidValues()
        {
            // Arrange
            var id = Guid.NewGuid();
            var title = "Test Template";
            var content = "Test template content";
            var createdAt = DateTime.UtcNow;
            var updatedAt = DateTime.UtcNow;

            // Act
            var promptTemplate = new PromptTemplate
            {
                Id = id,
                Title = title,
                Content = content,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt
            };

            // Assert
            promptTemplate.Id.Should().Be(id);
            promptTemplate.Title.Should().Be(title);
            promptTemplate.Content.Should().Be(content);
            promptTemplate.CreatedAt.Should().Be(createdAt);
            promptTemplate.UpdatedAt.Should().Be(updatedAt);
        }

        [Fact]
        public void Properties_SetAndGetValuesCorrectly()
        {
            // Arrange
            var promptTemplate = new PromptTemplate();
            var expectedId = Guid.NewGuid();
            var expectedTitle = "Updated Title";
            var expectedContent = "Updated Content";
            var expectedCreatedAt = DateTime.UtcNow.AddDays(-1);
            var expectedUpdatedAt = DateTime.UtcNow;

            // Act
            promptTemplate.Id = expectedId;
            promptTemplate.Title = expectedTitle;
            promptTemplate.Content = expectedContent;
            promptTemplate.CreatedAt = expectedCreatedAt;
            promptTemplate.UpdatedAt = expectedUpdatedAt;

            // Assert
            promptTemplate.Id.Should().Be(expectedId);
            promptTemplate.Title.Should().Be(expectedTitle);
            promptTemplate.Content.Should().Be(expectedContent);
            promptTemplate.CreatedAt.Should().Be(expectedCreatedAt);
            promptTemplate.UpdatedAt.Should().Be(expectedUpdatedAt);
        }

        [Fact]
        public void Id_PropertyIsGuidAndInitializedToNewGuid()
        {
            // Arrange & Act
            var promptTemplate = new PromptTemplate();

            // Assert
            promptTemplate.Id.Should().Be(Guid.Empty);
            promptTemplate.Id.Should().Be(default(Guid));
        }

        [Fact]
        public void CreatedAt_PropertyIsDateTimeAndDefaultsToDefault()
        {
            // Arrange & Act
            var promptTemplate = new PromptTemplate();

            // Assert
            promptTemplate.CreatedAt.Should().Be(default(DateTime));
            promptTemplate.CreatedAt.Should().Be(default(DateTime));
        }

        [Fact]
        public void UpdatedAt_PropertyIsNullableDateTimeAndDefaultsToNull()
        {
            // Arrange & Act
            var promptTemplate = new PromptTemplate();

            // Assert
            promptTemplate.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void Title_PropertyIsStringAndDefaultsToEmpty()
        {
            // Arrange & Act
            var promptTemplate = new PromptTemplate();

            // Assert
            promptTemplate.Title.Should().Be(string.Empty);
            promptTemplate.Title.Should().BeAssignableTo<string>();
        }

        [Fact]
        public void Content_PropertyIsStringAndDefaultsToEmpty()
        {
            // Arrange & Act
            var promptTemplate = new PromptTemplate();

            // Assert
            promptTemplate.Content.Should().Be(string.Empty);
            promptTemplate.Content.Should().BeAssignableTo<string>();
        }
    }
}