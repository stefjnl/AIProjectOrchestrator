using System;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.API.Controllers;
using AIProjectOrchestrator.Domain.Models;
using AIProjectOrchestrator.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using FluentAssertions;
using Xunit;

namespace AIProjectOrchestrator.UnitTests.API.Controllers
{
    public class ProjectPlanningControllerTests
    {
        private readonly Mock<IProjectPlanningService> _mockService;
        private readonly ProjectPlanningController _controller;

        public ProjectPlanningControllerTests()
        {
            _mockService = new Mock<IProjectPlanningService>();
            _controller = new ProjectPlanningController(_mockService.Object);
        }

        [Fact]
        public async Task CreateProjectPlan_WithValidRequest_CallsServiceAndReturnsResult()
        {
            // Arrange
            var request = new ProjectPlanningRequest
            {
                RequirementsAnalysisId = Guid.NewGuid()
            };
            var expectedResponse = new ProjectPlanningResponse
            {
                PlanningId = Guid.NewGuid(),
                Status = ProjectPlanningStatus.Approved
            };
            _mockService.Setup(s => s.CreateProjectPlanAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.CreateProjectPlan(request, CancellationToken.None);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(expectedResponse);
            _mockService.Verify(s => s.CreateProjectPlanAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetPlanning_WithValidId_CallsServiceAndReturnsResult()
        {
            // Arrange
            var planningId = Guid.NewGuid();
            var expectedResponse = new ProjectPlanningResponse
            {
                PlanningId = planningId,
                Status = ProjectPlanningStatus.Approved
            };
            _mockService.Setup(s => s.GetPlanningResultsAsync(planningId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetPlanning(planningId, CancellationToken.None);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(expectedResponse);
            _mockService.Verify(s => s.GetPlanningResultsAsync(planningId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetPlanning_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            var planningId = Guid.NewGuid();
            _mockService.Setup(s => s.GetPlanningResultsAsync(planningId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ProjectPlanningResponse)null!);

            // Act
            var result = await _controller.GetPlanning(planningId, CancellationToken.None);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task ApprovePlan_WithValidId_CallsService()
        {
            // Arrange
            var planningId = Guid.NewGuid();

            // Act
            var result = await _controller.ApprovePlan(planningId, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkResult>();
            _mockService.Verify(s => s.UpdatePlanningStatusAsync(planningId, ProjectPlanningStatus.Approved, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}