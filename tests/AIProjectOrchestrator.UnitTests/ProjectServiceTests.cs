using Xunit;
using Moq;
using AIProjectOrchestrator.Application.Services;
using AIProjectOrchestrator.Domain.Interfaces;
using AIProjectOrchestrator.Domain.Entities;
using System.Threading;
using AIProjectOrchestrator.Domain.Services;

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
            mockRepository.Setup(repo => repo.GetAllAsync(CancellationToken.None)).ReturnsAsync(expectedProjects);
            
            var mockReviewService = new Mock<IReviewService>();
            var projectService = new ProjectService(mockRepository.Object, mockReviewService.Object);

            // Act
            var result = await projectService.GetAllProjectsAsync();

            // Assert
            Assert.Equal(expectedProjects, result);
        }
        
        [Fact]
        public async Task DeleteProjectAsync_CallsDeleteReviewsByProjectIdAsync()
        {
            // Arrange
            var mockRepository = new Mock<IProjectRepository>();
            var mockReviewService = new Mock<IReviewService>();
            
            // Setup the review service to verify it's called
            mockReviewService.Setup(rs => rs.DeleteReviewsByProjectIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            
            // Setup the project repository to verify it's called
            mockRepository.Setup(repo => repo.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            
            var projectService = new ProjectService(mockRepository.Object, mockReviewService.Object);

            // Act
            await projectService.DeleteProjectAsync(1);

            // Assert
            mockReviewService.Verify(rs => rs.DeleteReviewsByProjectIdAsync(1, CancellationToken.None), Times.Once);
            mockRepository.Verify(repo => repo.DeleteAsync(1, CancellationToken.None), Times.Once);
        }
    }
}
