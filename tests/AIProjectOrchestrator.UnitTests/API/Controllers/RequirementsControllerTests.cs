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
    public class RequirementsControllerTests
    {
        private readonly Mock<IRequirementsAnalysisService> _mockService;
        private readonly RequirementsController _controller;

        public RequirementsControllerTests()
        {
            _mockService = new Mock<IRequirementsAnalysisService>();
            _controller = new RequirementsController(_mockService.Object);
        }

        [Fact]
        public async Task AnalyzeRequirements_WithValidRequest_CallsServiceAndReturnsResult()
        {
            // Arrange
            var request = new RequirementsAnalysisRequest
            {
                ProjectDescription = "Test description"
            };
            var expectedResponse = new RequirementsAnalysisResponse
            {
                AnalysisId = Guid.NewGuid(),
                Status = RequirementsAnalysisStatus.Approved
            };
            _mockService.Setup(s => s.AnalyzeRequirementsAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.AnalyzeRequirements(request, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            _mockService.Verify(s => s.AnalyzeRequirementsAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetAnalysis_WithValidId_CallsServiceAndReturnsResult()
        {
            // Arrange
            var analysisId = Guid.NewGuid();
            var expectedResponse = new RequirementsAnalysisResponse
            {
                AnalysisId = analysisId,
                Status = RequirementsAnalysisStatus.Approved
            };
            _mockService.Setup(s => s.GetAnalysisResultsAsync(analysisId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetAnalysis(analysisId, CancellationToken.None);

            // Assert
            result.Value.Should().Be(expectedResponse);
            _mockService.Verify(s => s.GetAnalysisResultsAsync(analysisId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetAnalysis_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            var analysisId = Guid.NewGuid();
            _mockService.Setup(s => s.GetAnalysisResultsAsync(analysisId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((RequirementsAnalysisResponse)null);

            // Act
            var result = await _controller.GetAnalysis(analysisId, CancellationToken.None);

            // Assert
            result.Result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task ApproveAnalysis_WithValidId_CallsService()
        {
            // Arrange
            var analysisId = Guid.NewGuid();

            // Act
            var result = await _controller.ApproveAnalysis(analysisId, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkResult>();
            _mockService.Verify(s => s.UpdateAnalysisStatusAsync(analysisId, RequirementsAnalysisStatus.Approved, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}