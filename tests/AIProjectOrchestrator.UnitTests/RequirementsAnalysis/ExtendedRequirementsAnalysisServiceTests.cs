using AIProjectOrchestrator.Application.Services;
using AIProjectOrchestrator.Domain.Models;
using AIProjectOrchestrator.Domain.Models.AI;
using AIProjectOrchestrator.Domain.Models.Review;
using AIProjectOrchestrator.Domain.Services;
using AIProjectOrchestrator.Infrastructure.AI;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using AIProjectOrchestrator.Domain.Interfaces;

namespace AIProjectOrchestrator.UnitTests.RequirementsAnalysis
{
    public class ExtendedRequirementsAnalysisServiceTests
    {
        private readonly Mock<IInstructionService> _mockInstructionService;
        private readonly Mock<IAIClientFactory> _mockAIClientFactory;
        private readonly Mock<IReviewService> _mockReviewService;
        private readonly Mock<ILogger<RequirementsAnalysisService>> _mockLogger;
        private readonly Mock<IRequirementsAnalysisRepository> _mockRequirementsAnalysisRepository;
        private readonly RequirementsAnalysisService _service;

        public ExtendedRequirementsAnalysisServiceTests()
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
        public async Task GetAnalysisResultsAsync_WithExistingAnalysisId_ReturnsAnalysisResult()
        {
            // Arrange
            var request = new RequirementsAnalysisRequest
            {
                ProjectDescription = "Build a task management system for small teams"
            };

            var instructionContent = new InstructionContent
            {
                ServiceName = "RequirementsAnalyst",
                Content = "# Role\nYou are a requirements analyst\n# Task\nAnalyze requirements\n# Constraints\nFocus on clarity",
                LastModified = DateTime.UtcNow,
                IsValid = true,
                ValidationMessage = string.Empty
            };

            var aiResponse = new AIResponse
            {
                Content = "### Project Overview\nA task management system for small teams...",
                TokensUsed = 150,
                ProviderName = "Claude",
                IsSuccess = true,
                ErrorMessage = null,
                ResponseTime = TimeSpan.FromMilliseconds(500)
            };

            var reviewResponse = new ReviewResponse
            {
                ReviewId = Guid.NewGuid(),
                Status = ReviewStatus.Pending,
                SubmittedAt = DateTime.UtcNow,
                Message = "Review submitted successfully"
            };

            _mockInstructionService.Setup(x => x.GetInstructionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(instructionContent);

            var mockAIClient = new Mock<IAIClient>();
            mockAIClient.Setup(x => x.ProviderName).Returns("Claude");
            mockAIClient.Setup(x => x.CallAsync(It.IsAny<AIRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(aiResponse);

            _mockAIClientFactory.Setup(x => x.GetClient(It.IsAny<string>()))
                .Returns(mockAIClient.Object);

            _mockReviewService.Setup(x => x.SubmitForReviewAsync(It.IsAny<SubmitReviewRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(reviewResponse);

            // First, perform an analysis to store the result
            var analysisResult = await _service.AnalyzeRequirementsAsync(request, CancellationToken.None);

            // Act
            var result = await _service.GetAnalysisResultsAsync(analysisResult.AnalysisId, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(analysisResult.AnalysisId, result.AnalysisId);
            Assert.Equal(analysisResult.ProjectDescription, result.ProjectDescription);
            Assert.Equal(analysisResult.AnalysisResult, result.AnalysisResult);
            Assert.Equal(analysisResult.ReviewId, result.ReviewId);
            Assert.Equal(analysisResult.Status, result.Status);
        }

        [Fact]
        public async Task GetAnalysisResultsAsync_WithUnknownAnalysisId_ReturnsNull()
        {
            // Arrange
            var unknownAnalysisId = Guid.NewGuid();

            // Act
            var result = await _service.GetAnalysisResultsAsync(unknownAnalysisId, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }
    }
}
