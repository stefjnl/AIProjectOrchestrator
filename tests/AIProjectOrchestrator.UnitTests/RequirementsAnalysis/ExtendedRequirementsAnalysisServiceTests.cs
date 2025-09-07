using AIProjectOrchestrator.Application.Services;
using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Models;
using AIProjectOrchestrator.Domain.Models.AI;
using AIProjectOrchestrator.Domain.Models.Review;
using AIProjectOrchestrator.Domain.Services;
using AIProjectOrchestrator.Infrastructure.AI;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
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
            var analysisId = Guid.NewGuid();

            var mockAnalysisEntity = new Domain.Entities.RequirementsAnalysis
            {
                AnalysisId = analysisId.ToString(),
                ProjectId = 1,
                Status = RequirementsAnalysisStatus.PendingReview,
                Content = "### Project Overview\nA task management system for small teams...",
                ReviewId = Guid.NewGuid().ToString(),
                CreatedDate = DateTime.UtcNow
            };

            _mockRequirementsAnalysisRepository.Setup(x => x.GetByAnalysisIdAsync(analysisId.ToString(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockAnalysisEntity);

            // Act
            var result = await _service.GetAnalysisResultsAsync(analysisId, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(analysisId, result.AnalysisId);
            Assert.Equal(mockAnalysisEntity.Content, result.AnalysisResult);
            Assert.Equal(Guid.Parse(mockAnalysisEntity.ReviewId), result.ReviewId);
            Assert.Equal(RequirementsAnalysisStatus.PendingReview, result.Status);
            Assert.Equal(DateTime.UtcNow.Date, result.CreatedAt.Date); // Check date part only
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

        [Fact]
        public async Task GetAnalysisStatusAsync_WithExistingAnalysis_ReturnsCorrectStatus()
        {
            // Arrange
            var analysisId = Guid.NewGuid();
            var mockAnalysisEntity = new AIProjectOrchestrator.Domain.Entities.RequirementsAnalysis
            {
                AnalysisId = analysisId.ToString(),
                ProjectId = 1,
                Status = RequirementsAnalysisStatus.Approved,
                Content = "Test content",
                ReviewId = Guid.NewGuid().ToString(),
                CreatedDate = DateTime.UtcNow
            };

            _mockRequirementsAnalysisRepository.Setup(x => x.GetByAnalysisIdAsync(analysisId.ToString(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockAnalysisEntity);

            // Act
            var result = await _service.GetAnalysisStatusAsync(analysisId, CancellationToken.None);

            // Assert
            Assert.Equal(RequirementsAnalysisStatus.Approved, result);
        }

        [Fact]
        public async Task GetAnalysisStatusAsync_WithNonExistingAnalysis_ReturnsFailedStatus()
        {
            // Arrange
            var unknownAnalysisId = Guid.NewGuid();

            _mockRequirementsAnalysisRepository.Setup(x => x.GetByAnalysisIdAsync(unknownAnalysisId.ToString(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AIProjectOrchestrator.Domain.Entities.RequirementsAnalysis)null!);

            // Act
            var result = await _service.GetAnalysisStatusAsync(unknownAnalysisId, CancellationToken.None);

            // Assert
            Assert.Equal(RequirementsAnalysisStatus.Failed, result);
        }

        [Fact]
        public async Task GetAnalysisResultContentAsync_WithExistingAnalysis_ReturnsContent()
        {
            // Arrange
            var analysisId = Guid.NewGuid();
            var expectedContent = "Test analysis content";

            var mockAnalysisEntity = new AIProjectOrchestrator.Domain.Entities.RequirementsAnalysis
            {
                AnalysisId = analysisId.ToString(),
                ProjectId = 1,
                Status = RequirementsAnalysisStatus.PendingReview,
                Content = expectedContent,
                ReviewId = Guid.NewGuid().ToString(),
                CreatedDate = DateTime.UtcNow
            };

            _mockRequirementsAnalysisRepository.Setup(x => x.GetByAnalysisIdAsync(analysisId.ToString(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockAnalysisEntity);

            // Act
            var result = await _service.GetAnalysisResultContentAsync(analysisId, CancellationToken.None);

            // Assert
            Assert.Equal(expectedContent, result);
        }

        [Fact]
        public async Task GetAnalysisResultContentAsync_WithNonExistingAnalysis_ReturnsNull()
        {
            // Arrange
            var unknownAnalysisId = Guid.NewGuid();

            _mockRequirementsAnalysisRepository.Setup(x => x.GetByAnalysisIdAsync(unknownAnalysisId.ToString(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AIProjectOrchestrator.Domain.Entities.RequirementsAnalysis)null!);

            // Act
            var result = await _service.GetAnalysisResultContentAsync(unknownAnalysisId, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CanAnalyzeRequirementsAsync_WhenNoExistingAnalysis_ReturnsTrue()
        {
            // Arrange
            var projectId = Guid.NewGuid();

            _mockRequirementsAnalysisRepository.Setup(x => x.GetByProjectIdAsync(projectId.GetHashCode(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AIProjectOrchestrator.Domain.Entities.RequirementsAnalysis)null!);

            // Act
            var result = await _service.CanAnalyzeRequirementsAsync(projectId, CancellationToken.None);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CanAnalyzeRequirementsAsync_WhenExistingAnalysisExists_ReturnsFalse()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var existingAnalysis = new AIProjectOrchestrator.Domain.Entities.RequirementsAnalysis
            {
                AnalysisId = Guid.NewGuid().ToString(),
                ProjectId = projectId.GetHashCode(),
                Status = RequirementsAnalysisStatus.PendingReview,
                Content = "Existing analysis",
                ReviewId = Guid.NewGuid().ToString(),
                CreatedDate = DateTime.UtcNow
            };

            _mockRequirementsAnalysisRepository.Setup(x => x.GetByProjectIdAsync(projectId.GetHashCode(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingAnalysis);

            // Act
            var result = await _service.CanAnalyzeRequirementsAsync(projectId, CancellationToken.None);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CanAnalyzeRequirementsAsync_WhenRepositoryThrowsException_ReturnsFalse()
        {
            // Arrange
            var projectId = Guid.NewGuid();

            _mockRequirementsAnalysisRepository.Setup(x => x.GetByProjectIdAsync(projectId.GetHashCode(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _service.CanAnalyzeRequirementsAsync(projectId, CancellationToken.None);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetBusinessContextAsync_WithExistingAnalysis_ReturnsContent()
        {
            // Arrange
            var analysisId = Guid.NewGuid();
            var expectedContent = "Business context content";

            var mockAnalysisEntity = new AIProjectOrchestrator.Domain.Entities.RequirementsAnalysis
            {
                AnalysisId = analysisId.ToString(),
                ProjectId = 1,
                Status = RequirementsAnalysisStatus.PendingReview,
                Content = expectedContent,
                ReviewId = Guid.NewGuid().ToString(),
                CreatedDate = DateTime.UtcNow
            };

            _mockRequirementsAnalysisRepository.Setup(x => x.GetByAnalysisIdAsync(analysisId.ToString(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockAnalysisEntity);

            // Act
            var result = await _service.GetBusinessContextAsync(analysisId, CancellationToken.None);

            // Assert
            Assert.Equal(expectedContent, result);
        }

        [Fact]
        public async Task GetBusinessContextAsync_WithNonExistingAnalysis_ReturnsNull()
        {
            // Arrange
            var unknownAnalysisId = Guid.NewGuid();

            _mockRequirementsAnalysisRepository.Setup(x => x.GetByAnalysisIdAsync(unknownAnalysisId.ToString(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AIProjectOrchestrator.Domain.Entities.RequirementsAnalysis)null!);

            // Act
            var result = await _service.GetBusinessContextAsync(unknownAnalysisId, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateAnalysisStatusAsync_WithExistingAnalysis_UpdatesStatus()
        {
            // Arrange
            var analysisId = Guid.NewGuid();
            var newStatus = RequirementsAnalysisStatus.Approved;

            var mockAnalysisEntity = new AIProjectOrchestrator.Domain.Entities.RequirementsAnalysis
            {
                AnalysisId = analysisId.ToString(),
                ProjectId = 1,
                Status = RequirementsAnalysisStatus.PendingReview,
                Content = "Test content",
                ReviewId = Guid.NewGuid().ToString(),
                CreatedDate = DateTime.UtcNow
            };

            _mockRequirementsAnalysisRepository.Setup(x => x.GetByAnalysisIdAsync(analysisId.ToString(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockAnalysisEntity);

            // Act
            await _service.UpdateAnalysisStatusAsync(analysisId, newStatus, CancellationToken.None);

            // Assert
            _mockRequirementsAnalysisRepository.Verify(x => x.UpdateAsync(It.Is<AIProjectOrchestrator.Domain.Entities.RequirementsAnalysis>(
                a => a.Status == newStatus), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAnalysisStatusAsync_WithNonExistingAnalysis_DoesNotUpdate()
        {
            // Arrange
            var unknownAnalysisId = Guid.NewGuid();
            var newStatus = RequirementsAnalysisStatus.Approved;

            _mockRequirementsAnalysisRepository.Setup(x => x.GetByAnalysisIdAsync(unknownAnalysisId.ToString(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AIProjectOrchestrator.Domain.Entities.RequirementsAnalysis)null!);

            // Act
            await _service.UpdateAnalysisStatusAsync(unknownAnalysisId, newStatus, CancellationToken.None);

            // Assert
            _mockRequirementsAnalysisRepository.Verify(x => x.UpdateAsync(It.IsAny<AIProjectOrchestrator.Domain.Entities.RequirementsAnalysis>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetAnalysisByProjectAsync_WithExistingProject_ReturnsAnalysis()
        {
            // Arrange
            var projectId = 123;
            var expectedAnalysis = new AIProjectOrchestrator.Domain.Entities.RequirementsAnalysis
            {
                AnalysisId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                Status = RequirementsAnalysisStatus.PendingReview,
                Content = "Project analysis content",
                ReviewId = Guid.NewGuid().ToString(),
                CreatedDate = DateTime.UtcNow
            };

            _mockRequirementsAnalysisRepository.Setup(x => x.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedAnalysis);

            // Act
            var result = await _service.GetAnalysisByProjectAsync(projectId, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedAnalysis.AnalysisId, result.AnalysisId);
            Assert.Equal(expectedAnalysis.ProjectId, result.ProjectId);
            Assert.Equal(expectedAnalysis.Content, result.Content);
        }

        [Fact]
        public async Task GetAnalysisByProjectAsync_WithNonExistingProject_ReturnsNull()
        {
            // Arrange
            var projectId = 999;

            _mockRequirementsAnalysisRepository.Setup(x => x.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((AIProjectOrchestrator.Domain.Entities.RequirementsAnalysis)null!);

            // Act
            var result = await _service.GetAnalysisByProjectAsync(projectId, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAnalysisByProjectAsync_WhenRepositoryThrowsException_ReturnsNull()
        {
            // Arrange
            var projectId = 123;

            _mockRequirementsAnalysisRepository.Setup(x => x.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _service.GetAnalysisByProjectAsync(projectId, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AnalyzeRequirementsAsync_WithValidRequest_ReturnsSuccessfulResponse()
        {
            // Arrange
            var request = new RequirementsAnalysisRequest
            {
                ProjectDescription = "Build a task management system for small teams",
                ProjectId = "123",
                AdditionalContext = "The system should support collaboration features",
                Constraints = "Must be completed within 3 months"
            };

            var instructionContent = new InstructionContent
            {
                Content = "You are a requirements analyst. Analyze the project description and provide detailed requirements.",
                IsValid = true,
                ValidationMessage = string.Empty
            };

            var aiResponse = new AIResponse
            {
                Content = "## Requirements Analysis\n\n### Functional Requirements\n1. User authentication\n2. Task creation and management\n3. Team collaboration features\n\n### Non-Functional Requirements\n1. Performance: Support 1000 concurrent users\n2. Security: Implement proper authentication\n3. Scalability: Cloud-native architecture",
                IsSuccess = true,
                ErrorMessage = string.Empty
            };

            var reviewResponse = new ReviewResponse
            {
                ReviewId = Guid.NewGuid(),
                Status = ReviewStatus.Pending,
                SubmittedAt = DateTime.UtcNow,
                Message = "Review submitted successfully"
            };

            _mockInstructionService.Setup(x => x.GetInstructionAsync("RequirementsAnalyst", It.IsAny<CancellationToken>()))
                .ReturnsAsync(instructionContent);

            var mockAIClient = new Mock<IAIClient>();
            mockAIClient.Setup(x => x.CallAsync(It.IsAny<AIRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(aiResponse);

            _mockAIClientFactory.Setup(x => x.GetClient("OpenRouter"))
                .Returns(mockAIClient.Object);

            _mockReviewService.Setup(x => x.SubmitForReviewAsync(It.IsAny<SubmitReviewRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(reviewResponse);

            _mockRequirementsAnalysisRepository.Setup(x => x.AddAsync(It.IsAny<AIProjectOrchestrator.Domain.Entities.RequirementsAnalysis>(), It.IsAny<CancellationToken>()))
                .Callback<AIProjectOrchestrator.Domain.Entities.RequirementsAnalysis, CancellationToken>((analysis, token) =>
                {
                    analysis.Id = 1; // Simulate database-generated ID
                })
                .ReturnsAsync((AIProjectOrchestrator.Domain.Entities.RequirementsAnalysis analysis, CancellationToken token) => analysis);

            _mockRequirementsAnalysisRepository.Setup(x => x.UpdateAsync(It.IsAny<AIProjectOrchestrator.Domain.Entities.RequirementsAnalysis>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.AnalyzeRequirementsAsync(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(Guid.Empty, result.AnalysisId);
            Assert.Equal(reviewResponse.ReviewId, result.ReviewId);
            Assert.Equal(RequirementsAnalysisStatus.PendingReview, result.Status);

            // Verify all dependencies were called
            _mockInstructionService.Verify(x => x.GetInstructionAsync("RequirementsAnalyst", It.IsAny<CancellationToken>()), Times.Once);
            _mockAIClientFactory.Verify(x => x.GetClient("OpenRouter"), Times.Once);
            mockAIClient.Verify(x => x.CallAsync(It.IsAny<AIRequest>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockReviewService.Verify(x => x.SubmitForReviewAsync(It.IsAny<SubmitReviewRequest>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockRequirementsAnalysisRepository.Verify(x => x.AddAsync(It.IsAny<AIProjectOrchestrator.Domain.Entities.RequirementsAnalysis>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockRequirementsAnalysisRepository.Verify(x => x.UpdateAsync(It.IsAny<AIProjectOrchestrator.Domain.Entities.RequirementsAnalysis>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AnalyzeRequirementsAsync_WhenInstructionServiceFails_ReturnsFailureResponse()
        {
            // Arrange
            var request = new RequirementsAnalysisRequest
            {
                ProjectDescription = "Build a task management system",
                ProjectId = "123"
            };

            _mockInstructionService.Setup(x => x.GetInstructionAsync("RequirementsAnalyst", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new InstructionContent { IsValid = false, ValidationMessage = "Instruction not found" });

            // Act
            var result = await _service.AnalyzeRequirementsAsync(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(Guid.Empty, result.AnalysisId);
            Assert.Equal(Guid.Empty, result.ReviewId);
            Assert.Equal(RequirementsAnalysisStatus.Failed, result.Status);
        }

        [Fact]
        public async Task AnalyzeRequirementsAsync_WhenAIClientFails_ReturnsFailureResponse()
        {
            // Arrange
            var request = new RequirementsAnalysisRequest
            {
                ProjectDescription = "Build a task management system",
                ProjectId = "123"
            };

            var instructionContent = new InstructionContent
            {
                Content = "You are a requirements analyst.",
                IsValid = true,
                ValidationMessage = string.Empty
            };

            _mockInstructionService.Setup(x => x.GetInstructionAsync("RequirementsAnalyst", It.IsAny<CancellationToken>()))
                .ReturnsAsync(instructionContent);

            var mockAIClient = new Mock<IAIClient>();
            mockAIClient.Setup(x => x.CallAsync(It.IsAny<AIRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIResponse { IsSuccess = false, ErrorMessage = "AI service error" });

            _mockAIClientFactory.Setup(x => x.GetClient("OpenRouter"))
                .Returns(mockAIClient.Object);

            // Act
            var result = await _service.AnalyzeRequirementsAsync(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(Guid.Empty, result.AnalysisId);
            Assert.Equal(Guid.Empty, result.ReviewId);
            Assert.Equal(RequirementsAnalysisStatus.Failed, result.Status);
        }

        [Fact]
        public async Task AnalyzeRequirementsAsync_WhenReviewServiceFails_ReturnsFailureResponse()
        {
            // Arrange
            var request = new RequirementsAnalysisRequest
            {
                ProjectDescription = "Build a task management system",
                ProjectId = "123"
            };

            var instructionContent = new InstructionContent
            {
                Content = "You are a requirements analyst.",
                IsValid = true,
                ValidationMessage = string.Empty
            };

            var aiResponse = new AIResponse
            {
                Content = "Analysis content",
                IsSuccess = true,
                ErrorMessage = string.Empty
            };

            _mockInstructionService.Setup(x => x.GetInstructionAsync("RequirementsAnalyst", It.IsAny<CancellationToken>()))
                .ReturnsAsync(instructionContent);

            var mockAIClient = new Mock<IAIClient>();
            mockAIClient.Setup(x => x.CallAsync(It.IsAny<AIRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(aiResponse);

            _mockAIClientFactory.Setup(x => x.GetClient("OpenRouter"))
                .Returns(mockAIClient.Object);

            _mockReviewService.Setup(x => x.SubmitForReviewAsync(It.IsAny<SubmitReviewRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReviewResponse { ReviewId = Guid.Empty, Status = ReviewStatus.Failed, SubmittedAt = DateTime.UtcNow, Message = "Review submission failed" });

            // Act
            var result = await _service.AnalyzeRequirementsAsync(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(Guid.Empty, result.AnalysisId);
            Assert.Equal(Guid.Empty, result.ReviewId);
            Assert.Equal(RequirementsAnalysisStatus.Failed, result.Status);
        }
    }
}
