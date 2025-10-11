using FluentAssertions;
using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.UnitTests.Domain.Builders;

namespace AIProjectOrchestrator.UnitTests.Domain.Entities
{
    public class ProjectTests
    {
        [Fact]
        public void Constructor_InitializesDefaultProperties()
        {
            // Arrange & Act
            var project = new Project();

            // Assert
            project.Id.Should().Be(0);
            project.Name.Should().Be(string.Empty);
            project.Description.Should().Be(string.Empty);
            project.Status.Should().Be("active");
            project.Type.Should().Be("web");
            project.CreatedDate.Should().Be(default(DateTime));
            project.UpdatedDate.Should().Be(default(DateTime));
            project.RequirementsAnalyses.Should().NotBeNull();
            project.RequirementsAnalyses.Should().BeEmpty();
        }

        [Fact]
        public void Constructor_InitializesPropertiesWithValidValues()
        {
            // Arrange
            var name = "Test Project";
            var description = "Test Description";
            var status = "completed";
            var type = "mobile";
            var createdDate = DateTime.UtcNow;
            var updatedDate = DateTime.UtcNow;

            // Act
            var project = new Project
            {
                Name = name,
                Description = description,
                Status = status,
                Type = type,
                CreatedDate = createdDate,
                UpdatedDate = updatedDate
            };

            // Assert
            project.Name.Should().Be(name);
            project.Description.Should().Be(description);
            project.Status.Should().Be(status);
            project.Type.Should().Be(type);
            project.CreatedDate.Should().Be(createdDate);
            project.UpdatedDate.Should().Be(updatedDate);
        }

        [Fact]
        public void Properties_SetAndGetValuesCorrectly()
        {
            // Arrange
            var project = new Project();
            var expectedName = "Updated Project";
            var expectedDescription = "Updated Description";
            var expectedStatus = "inactive";
            var expectedType = "desktop";
            var expectedCreatedDate = DateTime.UtcNow.AddDays(-1);
            var expectedUpdatedDate = DateTime.UtcNow;

            // Act
            project.Name = expectedName;
            project.Description = expectedDescription;
            project.Status = expectedStatus;
            project.Type = expectedType;
            project.CreatedDate = expectedCreatedDate;
            project.UpdatedDate = expectedUpdatedDate;

            // Assert
            project.Name.Should().Be(expectedName);
            project.Description.Should().Be(expectedDescription);
            project.Status.Should().Be(expectedStatus);
            project.Type.Should().Be(expectedType);
            project.CreatedDate.Should().Be(expectedCreatedDate);
            project.UpdatedDate.Should().Be(expectedUpdatedDate);
        }

        [Fact]
        public void CreatedAt_PropertyReturnsCreatedDate()
        {
            // Arrange
            var createdDate = DateTime.UtcNow;
            var project = new Project
            {
                CreatedDate = createdDate
            };

            // Act
            var createdAt = project.CreatedAt;

            // Assert
            createdAt.Should().Be(createdDate);
        }

        [Fact]
        public void RequirementsAnalyses_CollectionIsInitialized()
        {
            // Arrange & Act
            var project = new Project();

            // Assert
            project.RequirementsAnalyses.Should().NotBeNull();
            project.RequirementsAnalyses.Should().BeAssignableTo<ICollection<RequirementsAnalysis>>();
            project.RequirementsAnalyses.Should().BeEmpty();
        }

        [Fact]
        public void RequirementsAnalyses_CollectionSupportsAddRemoveOperations()
        {
            // Arrange
            var project = new Project();
            var requirementsAnalysis = EntityBuilders.BuildRequirementsAnalysis();

            // Act
            project.RequirementsAnalyses.Add(requirementsAnalysis);

            // Assert
            project.RequirementsAnalyses.Should().Contain(requirementsAnalysis);
            project.RequirementsAnalyses.Count.Should().Be(1);

            // Act - Remove
            project.RequirementsAnalyses.Remove(requirementsAnalysis);

            // Assert
            project.RequirementsAnalyses.Should().NotContain(requirementsAnalysis);
            project.RequirementsAnalyses.Count.Should().Be(0);
        }

        [Fact]
        public void Id_PropertyIsIntAndDefaultsToZero()
        {
            // Arrange & Act
            var project = new Project();

            // Assert
            project.Id.Should().Be(0);
            project.Id.Should().BeOfType(typeof(int));
        }
    }
}