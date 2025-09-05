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

namespace AIProjectOrchestrator.UnitTests.RequirementsAnalysis
{
    public class RequirementsAnalysisServiceTests
    {
        private readonly Mock<IInstructionService> _mockInstructionService;
        private readonly Mock<IAIClientFactory> _mockAIClientFactory;
        private readonly Mock<IReviewService> _mockReviewService;
        private readonly Mock<ILogger<RequirementsAnalysisService>> _mockLogger;
        private readonly RequirementsAnalysisService _service;

        public RequirementsAnalysisServiceTests()
        {
            _mockInstructionService = new Mock<IInstructionService>();
            _mockAIClientFactory = new Mock<IAIClientFactory>();
            _mockReviewService = new Mock<IReviewService>();
            _mockLogger = new Mock<ILogger<RequirementsAnalysisService>>();
            
            _service = new RequirementsAnalysisService(
                _mockInstructionService.Object,
                _mockAIClientFactory.Object,
                new Lazy<IReviewService>(() => _mockReviewService.Object),
                _mockLogger.Object);
        }

        [Fact]
        public async Task AnalyzeRequirementsAsync_WithValidRequest_ReturnsSuccessResponse()
        {
            // Arrange
            var request = new RequirementsAnalysisRequest
            {
                ProjectDescription = "Build a task management system for small teams",
                AdditionalContext = "React frontend, .NET API backend",
                Constraints = "Must integrate with existing authentication"
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

            // Act
            var result = await _service.AnalyzeRequirementsAsync(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(Guid.Empty, result.AnalysisId);
            Assert.Equal(request.ProjectDescription, result.ProjectDescription);
            Assert.Equal(aiResponse.Content, result.AnalysisResult);
            Assert.Equal(reviewResponse.ReviewId, result.ReviewId);
            Assert.Equal(RequirementsAnalysisStatus.PendingReview, result.Status);
            Assert.NotEqual(DateTime.MinValue, result.CreatedAt);
        }

        [Fact]
        public async Task AnalyzeRequirementsAsync_WithInvalidInstruction_ThrowsInvalidOperationException()
        {
            // Arrange
            var request = new RequirementsAnalysisRequest
            {
                ProjectDescription = "Build a task management system for small teams"
            };

            var instructionContent = new InstructionContent
            {
                ServiceName = "RequirementsAnalyst",
                Content = string.Empty,
                LastModified = DateTime.UtcNow,
                IsValid = false,
                ValidationMessage = "Instruction file not found"
            };

            _mockInstructionService.Setup(x => x.GetInstructionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(instructionContent);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _service.AnalyzeRequirementsAsync(request, CancellationToken.None));
        }

        [Fact]
        public async Task AnalyzeRequirementsAsync_WithMissingClaudeClient_ThrowsInvalidOperationException()
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

            _mockInstructionService.Setup(x => x.GetInstructionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(instructionContent);

            _mockAIClientFactory.Setup(x => x.GetClient(It.IsAny<string>()))
                .Returns((IAIClient)null);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _service.AnalyzeRequirementsAsync(request, CancellationToken.None));
        }

        [Fact]
        public async Task AnalyzeRequirementsAsync_WithFailedAIResponse_ThrowsInvalidOperationException()
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
                Content = string.Empty,
                TokensUsed = 0,
                ProviderName = "Claude",
                IsSuccess = false,
                ErrorMessage = "API call failed",
                ResponseTime = TimeSpan.FromMilliseconds(100)
            };

            _mockInstructionService.Setup(x => x.GetInstructionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(instructionContent);

            var mockAIClient = new Mock<IAIClient>();
            mockAIClient.Setup(x => x.ProviderName).Returns("Claude");
            mockAIClient.Setup(x => x.CallAsync(It.IsAny<AIRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(aiResponse);

            _mockAIClientFactory.Setup(x => x.GetClient(It.IsAny<string>()))
                .Returns(mockAIClient.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _service.AnalyzeRequirementsAsync(request, CancellationToken.None));
        }

        [Fact]
        public async Task AnalyzeRequirementsAsync_WithEmptyProjectDescription_ThrowsArgumentException()
        {
            // Arrange
            var request = new RequirementsAnalysisRequest
            {
                ProjectDescription = string.Empty
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.AnalyzeRequirementsAsync(request, CancellationToken.None));
        }

        [Fact]
        public async Task AnalyzeRequirementsAsync_WithShortProjectDescription_ThrowsArgumentException()
        {
            // Arrange
            var request = new RequirementsAnalysisRequest
            {
                ProjectDescription = "Short"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.AnalyzeRequirementsAsync(request, CancellationToken.None));
        }

        [Fact]
        public async Task GetAnalysisStatusAsync_WithExistingAnalysisId_ReturnsStatus()
        {
            // Arrange
            var analysisId = Guid.NewGuid();
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

            // First, perform an analysis to set the status
            var analysisResult = await _service.AnalyzeRequirementsAsync(request, CancellationToken.None);

            // Act
            var status = await _service.GetAnalysisStatusAsync(analysisResult.AnalysisId, CancellationToken.None);

            // Assert
            Assert.Equal(RequirementsAnalysisStatus.PendingReview, status);
        }

        [Fact]
        public async Task GetAnalysisStatusAsync_WithUnknownAnalysisId_ReturnsFailedStatus()
        {
            // Arrange
            var unknownAnalysisId = Guid.NewGuid();

            // Act
            var status = await _service.GetAnalysisStatusAsync(unknownAnalysisId, CancellationToken.None);

            // Assert
            Assert.Equal(RequirementsAnalysisStatus.Failed, status);
        }
    }
}