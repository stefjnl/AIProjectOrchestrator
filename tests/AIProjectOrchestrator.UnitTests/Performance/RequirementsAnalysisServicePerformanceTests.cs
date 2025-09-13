using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using AIProjectOrchestrator.Application.Services;
using AIProjectOrchestrator.Domain.Models;
using AIProjectOrchestrator.Domain.Models.AI;
using AIProjectOrchestrator.Domain.Models.Review;
using AIProjectOrchestrator.Domain.Services;
using AIProjectOrchestrator.Domain.Interfaces;
using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Infrastructure.AI;

namespace AIProjectOrchestrator.UnitTests.Performance
{
    /// <summary>
    /// Performance tests for RequirementsAnalysisService to establish baseline metrics
    /// </summary>
    public class RequirementsAnalysisServicePerformanceTests
    {
        private readonly Mock<IInstructionService> _mockInstructionService;
        private readonly Mock<IAIClientFactory> _mockAIClientFactory;
        private readonly Mock<IReviewService> _mockReviewService;
        private readonly Mock<ILogger<RequirementsAnalysisService>> _mockLogger;
        private readonly Mock<IRequirementsAnalysisRepository> _mockRequirementsAnalysisRepository;
        private readonly RequirementsAnalysisService _service;

        public RequirementsAnalysisServicePerformanceTests()
        {
            _mockInstructionService = new Mock<IInstructionService>();
            _mockAIClientFactory = new Mock<IAIClientFactory>();
            _mockReviewService = new Mock<IReviewService>();
            _mockLogger = new Mock<ILogger<RequirementsAnalysisService>>();
            _mockRequirementsAnalysisRepository = new Mock<IRequirementsAnalysisRepository>();

            _service = new RequirementsAnalysisService(
                _mockInstructionService.Object,
                _mockAIClientFactory.Object,
                new Lazy<IReviewService>(() => _mockReviewService.Object),
                _mockLogger.Object,
                _mockRequirementsAnalysisRepository.Object);
        }

        [Fact]
        public async Task AnalyzeRequirementsAsync_ValidRequest_ShouldCompleteWithinTargetTime()
        {
            // Arrange
            var request = new RequirementsAnalysisRequest
            {
                ProjectDescription = "Build a comprehensive task management system with user authentication, real-time collaboration, and advanced reporting features for enterprise teams"
            };

            var instructionContent = new InstructionContent
            {
                ServiceName = "RequirementsAnalyst",
                Content = "# Role\nYou are a requirements analyst\n# Task\nAnalyze project requirements",
                LastModified = DateTime.UtcNow,
                IsValid = true,
                ValidationMessage = string.Empty
            };

            var aiResponse = new AIResponse
            {
                Content = "Detailed analysis of requirements including functional and non-functional requirements, user stories, and technical specifications",
                TokensUsed = 250,
                ProviderName = "OpenRouter",
                IsSuccess = true,
                ResponseTime = TimeSpan.FromMilliseconds(3500) // 3.5 seconds - within 5s target
            };

            var reviewResponse = new ReviewResponse
            {
                ReviewId = Guid.NewGuid(),
                Status = ReviewStatus.Pending,
                SubmittedAt = DateTime.UtcNow
            };

            var mockAIClient = new Mock<IAIClient>();
            mockAIClient.Setup(x => x.ProviderName).Returns("OpenRouter");
            mockAIClient.Setup(x => x.CallAsync(It.IsAny<AIRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(aiResponse);

            _mockInstructionService.Setup(x => x.GetInstructionAsync("RequirementsAnalyst", It.IsAny<CancellationToken>()))
                .ReturnsAsync(instructionContent);

            _mockAIClientFactory.Setup(x => x.GetClient("OpenRouter"))
                .Returns(mockAIClient.Object);

            _mockReviewService.Setup(x => x.SubmitForReviewAsync(It.IsAny<SubmitReviewRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(reviewResponse);

            // Act
            var stopwatch = Stopwatch.StartNew();
            var result = await _service.AnalyzeRequirementsAsync(request, CancellationToken.None);
            stopwatch.Stop();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(aiResponse.Content, result.AnalysisResult);
            Assert.Equal(reviewResponse.ReviewId, result.ReviewId);
            Assert.Equal(RequirementsAnalysisStatus.PendingReview, result.Status);

            // Performance assertion - should complete within 6 seconds (allowing 1 second overhead)
            Assert.True(stopwatch.ElapsedMilliseconds <= 6000,
                $"Requirements analysis should complete within 6 seconds, but took {stopwatch.ElapsedMilliseconds}ms");

            // Verify all dependencies were called
            _mockInstructionService.Verify(x => x.GetInstructionAsync("RequirementsAnalyst", It.IsAny<CancellationToken>()), Times.Once);
            _mockAIClientFactory.Verify(x => x.GetClient("OpenRouter"), Times.Once);
            mockAIClient.Verify(x => x.CallAsync(It.IsAny<AIRequest>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockReviewService.Verify(x => x.SubmitForReviewAsync(It.IsAny<SubmitReviewRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AnalyzeRequirementsAsync_WithNullProjectDescription_ShouldThrowArgumentException()
        {
            // Arrange
            var request = new RequirementsAnalysisRequest
            {
                ProjectDescription = null
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.AnalyzeRequirementsAsync(request, CancellationToken.None));

            // Verify no services were called due to validation failure
            _mockInstructionService.Verify(x => x.GetInstructionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockAIClientFactory.Verify(x => x.GetClient(It.IsAny<string>()), Times.Never);
            _mockReviewService.Verify(x => x.SubmitForReviewAsync(It.IsAny<SubmitReviewRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task AnalyzeRequirementsAsync_WithShortDescription_ShouldThrowArgumentException()
        {
            // Arrange
            var request = new RequirementsAnalysisRequest
            {
                ProjectDescription = "Short" // Too short - less than 10 characters
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.AnalyzeRequirementsAsync(request, CancellationToken.None));

            // Verify no services were called due to validation failure
            _mockInstructionService.Verify(x => x.GetInstructionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockAIClientFactory.Verify(x => x.GetClient(It.IsAny<string>()), Times.Never);
            _mockReviewService.Verify(x => x.SubmitForReviewAsync(It.IsAny<SubmitReviewRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetAnalysisStatusAsync_WithExistingAnalysis_ReturnsCorrectStatus()
        {
            // Arrange
            var analysisId = Guid.NewGuid();
            var expectedStatus = RequirementsAnalysisStatus.PendingReview;

            var analysisEntity = new AIProjectOrchestrator.Domain.Entities.RequirementsAnalysis
            {
                AnalysisId = analysisId.ToString(),
                ProjectId = 1,
                Status = expectedStatus,
                Content = "Test analysis content",
                ReviewId = Guid.NewGuid().ToString(),
                CreatedDate = DateTime.UtcNow
            };

            _mockRequirementsAnalysisRepository.Setup(x => x.GetByAnalysisIdAsync(analysisId.ToString(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(analysisEntity);

            // Act
            var result = await _service.GetAnalysisStatusAsync(analysisId, CancellationToken.None);

            // Assert
            Assert.Equal(expectedStatus, result);
            _mockRequirementsAnalysisRepository.Verify(x => x.GetByAnalysisIdAsync(analysisId.ToString(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetAnalysisStatusAsync_WithNonExistingAnalysis_ReturnsFailed()
        {
            // Arrange
            var analysisId = Guid.NewGuid();

            _mockRequirementsAnalysisRepository.Setup(x => x.GetByAnalysisIdAsync(analysisId.ToString(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AIProjectOrchestrator.Domain.Entities.RequirementsAnalysis)null);

            // Act
            var result = await _service.GetAnalysisStatusAsync(analysisId, CancellationToken.None);

            // Assert
            Assert.Equal(RequirementsAnalysisStatus.Failed, result);
            _mockRequirementsAnalysisRepository.Verify(x => x.GetByAnalysisIdAsync(analysisId.ToString(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CanAnalyzeRequirementsAsync_WithNoExistingAnalysis_ReturnsTrue()
        {
            // Arrange
            var projectId = Guid.NewGuid();

            _mockRequirementsAnalysisRepository.Setup(x => x.GetByProjectIdAsync(projectId.GetHashCode(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AIProjectOrchestrator.Domain.Entities.RequirementsAnalysis)null);

            // Act
            var result = await _service.CanAnalyzeRequirementsAsync(projectId, CancellationToken.None);

            // Assert
            Assert.True(result);
            _mockRequirementsAnalysisRepository.Verify(x => x.GetByProjectIdAsync(projectId.GetHashCode(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CanAnalyzeRequirementsAsync_WithExistingAnalysis_ReturnsFalse()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var existingAnalysis = new AIProjectOrchestrator.Domain.Entities.RequirementsAnalysis
            {
                AnalysisId = Guid.NewGuid().ToString(),
                ProjectId = projectId.GetHashCode(),
                Status = RequirementsAnalysisStatus.PendingReview
            };

            _mockRequirementsAnalysisRepository.Setup(x => x.GetByProjectIdAsync(projectId.GetHashCode(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingAnalysis);

            // Act
            var result = await _service.CanAnalyzeRequirementsAsync(projectId, CancellationToken.None);

            // Assert
            Assert.False(result);
            _mockRequirementsAnalysisRepository.Verify(x => x.GetByProjectIdAsync(projectId.GetHashCode(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetAnalysisResultContentAsync_WithExistingAnalysis_ReturnsContent()
        {
            // Arrange
            var analysisId = Guid.NewGuid();
            var expectedContent = "Test analysis content";

            var analysisEntity = new AIProjectOrchestrator.Domain.Entities.RequirementsAnalysis
            {
                AnalysisId = analysisId.ToString(),
                ProjectId = 1,
                Status = RequirementsAnalysisStatus.PendingReview,
                Content = expectedContent,
                ReviewId = Guid.NewGuid().ToString(),
                CreatedDate = DateTime.UtcNow
            };

            _mockRequirementsAnalysisRepository.Setup(x => x.GetByAnalysisIdAsync(analysisId.ToString(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(analysisEntity);

            // Act
            var result = await _service.GetAnalysisResultContentAsync(analysisId, CancellationToken.None);

            // Assert
            Assert.Equal(expectedContent, result);
            _mockRequirementsAnalysisRepository.Verify(x => x.GetByAnalysisIdAsync(analysisId.ToString(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetAnalysisResultContentAsync_WithNonExistingAnalysis_ReturnsNull()
        {
            // Arrange
            var analysisId = Guid.NewGuid();

            _mockRequirementsAnalysisRepository.Setup(x => x.GetByAnalysisIdAsync(analysisId.ToString(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AIProjectOrchestrator.Domain.Entities.RequirementsAnalysis)null);

            // Act
            var result = await _service.GetAnalysisResultContentAsync(analysisId, CancellationToken.None);

            // Assert
            Assert.Null(result);
            _mockRequirementsAnalysisRepository.Verify(x => x.GetByAnalysisIdAsync(analysisId.ToString(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetBusinessContextAsync_WithExistingAnalysis_ReturnsContent()
        {
            // Arrange
            var analysisId = Guid.NewGuid();
            var expectedContent = "Business context content";

            var analysisEntity = new AIProjectOrchestrator.Domain.Entities.RequirementsAnalysis
            {
                AnalysisId = analysisId.ToString(),
                ProjectId = 1,
                Status = RequirementsAnalysisStatus.PendingReview,
                Content = expectedContent,
                ReviewId = Guid.NewGuid().ToString(),
                CreatedDate = DateTime.UtcNow
            };

            _mockRequirementsAnalysisRepository.Setup(x => x.GetByAnalysisIdAsync(analysisId.ToString(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(analysisEntity);

            // Act
            var result = await _service.GetBusinessContextAsync(analysisId, CancellationToken.None);

            // Assert
            Assert.Equal(expectedContent, result);
            _mockRequirementsAnalysisRepository.Verify(x => x.GetByAnalysisIdAsync(analysisId.ToString(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAnalysisStatusAsync_WithExistingAnalysis_UpdatesStatus()
        {
            // Arrange
            var analysisId = Guid.NewGuid();
            var newStatus = RequirementsAnalysisStatus.Approved;

            var analysisEntity = new AIProjectOrchestrator.Domain.Entities.RequirementsAnalysis
            {
                AnalysisId = analysisId.ToString(),
                ProjectId = 1,
                Status = RequirementsAnalysisStatus.PendingReview,
                Content = "Test content",
                ReviewId = Guid.NewGuid().ToString(),
                CreatedDate = DateTime.UtcNow
            };

            _mockRequirementsAnalysisRepository.Setup(x => x.GetByAnalysisIdAsync(analysisId.ToString(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(analysisEntity);

            // Act
            await _service.UpdateAnalysisStatusAsync(analysisId, newStatus, CancellationToken.None);

            // Assert
            Assert.Equal(newStatus, analysisEntity.Status);
            _mockRequirementsAnalysisRepository.Verify(x => x.GetByAnalysisIdAsync(analysisId.ToString(), It.IsAny<CancellationToken>()), Times.Once);
            _mockRequirementsAnalysisRepository.Verify(x => x.UpdateAsync(analysisEntity, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetAnalysisByProjectAsync_WithExistingAnalysis_ReturnsAnalysis()
        {
            // Arrange
            var projectId = 123;
            var expectedAnalysis = new AIProjectOrchestrator.Domain.Entities.RequirementsAnalysis
            {
                AnalysisId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                Status = RequirementsAnalysisStatus.PendingReview,
                Content = "Test content",
                ReviewId = Guid.NewGuid().ToString(),
                CreatedDate = DateTime.UtcNow
            };

            _mockRequirementsAnalysisRepository.Setup(x => x.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedAnalysis);

            // Act
            var result = await _service.GetAnalysisByProjectAsync(projectId, CancellationToken.None);

            // Assert
            Assert.Equal(expectedAnalysis, result);
            _mockRequirementsAnalysisRepository.Verify(x => x.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetAnalysisByProjectAsync_WithNonExistingAnalysis_ReturnsNull()
        {
            // Arrange
            var projectId = 123;

            _mockRequirementsAnalysisRepository.Setup(x => x.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((AIProjectOrchestrator.Domain.Entities.RequirementsAnalysis)null);

            // Act
            var result = await _service.GetAnalysisByProjectAsync(projectId, CancellationToken.None);

            // Assert
            Assert.Null(result);
            _mockRequirementsAnalysisRepository.Verify(x => x.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}