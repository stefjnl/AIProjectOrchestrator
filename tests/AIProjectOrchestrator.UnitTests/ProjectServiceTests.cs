using Xunit;
using Moq;
using AIProjectOrchestrator.Application.Services;
using AIProjectOrchestrator.Domain.Interfaces;
using AIProjectOrchestrator.Domain.Entities;

namespace AIProjectOrchestrator.UnitTests
{
    public class ProjectServiceTests
    {
        [Fact]
        public async Task GetAllProjectsAsync_Returns_All_Projects()
        {
            // Arrange
            var mockRepository = new Mock<IProjectRepository>();
            var expectedProjects = new List<Project>
            {
                new Project { Id = 1, Name = "Project 1" },
                new Project { Id = 2, Name = "Project 2" }
            };
            mockRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(expectedProjects);
            
            var projectService = new ProjectService(mockRepository.Object);

            // Act
            var result = await projectService.GetAllProjectsAsync();

            // Assert
            Assert.Equal(expectedProjects, result);
        }
    }
}