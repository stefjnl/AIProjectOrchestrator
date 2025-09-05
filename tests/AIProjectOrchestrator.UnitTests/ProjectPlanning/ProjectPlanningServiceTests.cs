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

namespace AIProjectOrchestrator.UnitTests.ProjectPlanning
{
    public class ProjectPlanningServiceTests
    {
        private readonly Mock<IRequirementsAnalysisService> _mockRequirementsAnalysisService;
        private readonly Mock<IInstructionService> _mockInstructionService;
        private readonly Mock<IAIClientFactory> _mockAIClientFactory;
        private readonly Mock<IReviewService> _mockReviewService;
        private readonly Mock<ILogger<ProjectPlanningService>> _mockLogger;
        private readonly ProjectPlanningService _service;

        public ProjectPlanningServiceTests()
        {
            _mockRequirementsAnalysisService = new Mock<IRequirementsAnalysisService>();
            _mockInstructionService = new Mock<IInstructionService>();
            _mockAIClientFactory = new Mock<IAIClientFactory>();
            _mockReviewService = new Mock<IReviewService>();
            _mockLogger = new Mock<ILogger<ProjectPlanningService>>();
            
            _service = new ProjectPlanningService(
                _mockRequirementsAnalysisService.Object,
                _mockInstructionService.Object,
                _mockAIClientFactory.Object,
                new Lazy<IReviewService>(() => _mockReviewService.Object),
                _mockLogger.Object);
        }

        [Fact]
        public async Task CreateProjectPlanAsync_WithValidRequest_ReturnsSuccessResponse()
        {
            // Arrange
            var requirementsAnalysisId = Guid.NewGuid();
            var request = new ProjectPlanningRequest
            {
                RequirementsAnalysisId = requirementsAnalysisId,
                PlanningPreferences = "Agile methodology, microservices architecture",
                TechnicalConstraints = "Must use .NET and React",
                TimelineConstraints = "6-month delivery timeline"
            };

            var requirementsAnalysis = new RequirementsAnalysisResponse
            {
                AnalysisId = requirementsAnalysisId,
                ProjectDescription = "Build task management system for small teams",
                AnalysisResult = "### Project Overview\\nA task management system for small teams...",
                ReviewId = Guid.NewGuid(),
                Status = RequirementsAnalysisStatus.PendingReview,
                CreatedAt = DateTime.UtcNow
            };

            var instructionContent = new InstructionContent
            {
                ServiceName = "ProjectPlanner",
                Content = "# Role\\nYou are a project planner\\n# Task\\nCreate project plan\\n# Constraints\\nFocus on feasibility",
                LastModified = DateTime.UtcNow,
                IsValid = true,
                ValidationMessage = string.Empty
            };

            var aiResponse = new AIResponse
            {
                Content = "### Project Overview\\nA comprehensive project plan...\\n### Project Roadmap\\n1. Phase 1...\\n### Architectural Decisions\\nTechnology stack...\\n### Milestones\\nKey deliverables...",
                TokensUsed = 300,
                ProviderName = "Claude",
                IsSuccess = true,
                ErrorMessage = null,
                ResponseTime = TimeSpan.FromMilliseconds(800)
            };

            var reviewResponse = new ReviewResponse
            {
                ReviewId = Guid.NewGuid(),
                Status = ReviewStatus.Pending,
                SubmittedAt = DateTime.UtcNow,
                Message = "Review submitted successfully"
            };

            _mockRequirementsAnalysisService.Setup(x => x.GetAnalysisResultsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(requirementsAnalysis);

            _mockRequirementsAnalysisService.Setup(x => x.GetAnalysisStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(RequirementsAnalysisStatus.PendingReview);

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
            var result = await _service.CreateProjectPlanAsync(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(Guid.Empty, result.PlanningId);
            Assert.Equal(request.RequirementsAnalysisId, result.RequirementsAnalysisId);
            Assert.NotEmpty(result.ProjectRoadmap);
            Assert.NotEmpty(result.ArchitecturalDecisions);
            Assert.NotEmpty(result.Milestones);
            Assert.Equal(reviewResponse.ReviewId, result.ReviewId);
            Assert.Equal(ProjectPlanningStatus.PendingReview, result.Status);
            Assert.NotEqual(DateTime.MinValue, result.CreatedAt);
        }

        [Fact]
        public async Task CreateProjectPlanAsync_WithInvalidInstruction_ThrowsInvalidOperationException()
        {
            // Arrange
            var requirementsAnalysisId = Guid.NewGuid();
            var request = new ProjectPlanningRequest
            {
                RequirementsAnalysisId = requirementsAnalysisId
            };

            var requirementsAnalysis = new RequirementsAnalysisResponse
            {
                AnalysisId = requirementsAnalysisId,
                ProjectDescription = "Build task management system for small teams",
                AnalysisResult = "### Project Overview\\nA task management system for small teams...",
                ReviewId = Guid.NewGuid(),
                Status = RequirementsAnalysisStatus.PendingReview,
                CreatedAt = DateTime.UtcNow
            };

            var instructionContent = new InstructionContent
            {
                ServiceName = "ProjectPlanner",
                Content = string.Empty,
                LastModified = DateTime.UtcNow,
                IsValid = false,
                ValidationMessage = "Instruction file not found"
            };

            _mockRequirementsAnalysisService.Setup(x => x.GetAnalysisResultsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(requirementsAnalysis);

            _mockRequirementsAnalysisService.Setup(x => x.GetAnalysisStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(RequirementsAnalysisStatus.PendingReview);

            _mockInstructionService.Setup(x => x.GetInstructionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(instructionContent);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _service.CreateProjectPlanAsync(request, CancellationToken.None));
        }

        [Fact]
        public async Task CreateProjectPlanAsync_WithMissingClaudeClient_ThrowsInvalidOperationException()
        {
            // Arrange
            var requirementsAnalysisId = Guid.NewGuid();
            var request = new ProjectPlanningRequest
            {
                RequirementsAnalysisId = requirementsAnalysisId
            };

            var requirementsAnalysis = new RequirementsAnalysisResponse
            {
                AnalysisId = requirementsAnalysisId,
                ProjectDescription = "Build task management system for small teams",
                AnalysisResult = "### Project Overview\\nA task management system for small teams...",
                ReviewId = Guid.NewGuid(),
                Status = RequirementsAnalysisStatus.PendingReview,
                CreatedAt = DateTime.UtcNow
            };

            var instructionContent = new InstructionContent
            {
                ServiceName = "ProjectPlanner",
                Content = "# Role\\nYou are a project planner\\n# Task\\nCreate project plan\\n# Constraints\\nFocus on feasibility",
                LastModified = DateTime.UtcNow,
                IsValid = true,
                ValidationMessage = string.Empty
            };

            _mockRequirementsAnalysisService.Setup(x => x.GetAnalysisResultsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(requirementsAnalysis);

            _mockRequirementsAnalysisService.Setup(x => x.GetAnalysisStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(RequirementsAnalysisStatus.PendingReview);

            _mockInstructionService.Setup(x => x.GetInstructionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(instructionContent);

            _mockAIClientFactory.Setup(x => x.GetClient(It.IsAny<string>()))
                .Returns((IAIClient)null);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _service.CreateProjectPlanAsync(request, CancellationToken.None));
        }

        [Fact]
        public async Task CreateProjectPlanAsync_WithFailedAIResponse_ThrowsInvalidOperationException()
        {
            // Arrange
            var requirementsAnalysisId = Guid.NewGuid();
            var request = new ProjectPlanningRequest
            {
                RequirementsAnalysisId = requirementsAnalysisId
            };

            var requirementsAnalysis = new RequirementsAnalysisResponse
            {
                AnalysisId = requirementsAnalysisId,
                ProjectDescription = "Build task management system for small teams",
                AnalysisResult = "### Project Overview\\nA task management system for small teams...",
                ReviewId = Guid.NewGuid(),
                Status = RequirementsAnalysisStatus.PendingReview,
                CreatedAt = DateTime.UtcNow
            };

            var instructionContent = new InstructionContent
            {
                ServiceName = "ProjectPlanner",
                Content = "# Role\\nYou are a project planner\\n# Task\\nCreate project plan\\n# Constraints\\nFocus on feasibility",
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

            _mockRequirementsAnalysisService.Setup(x => x.GetAnalysisResultsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(requirementsAnalysis);

            _mockRequirementsAnalysisService.Setup(x => x.GetAnalysisStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(RequirementsAnalysisStatus.PendingReview);

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
                _service.CreateProjectPlanAsync(request, CancellationToken.None));
        }

        [Fact]
        public async Task CreateProjectPlanAsync_WithEmptyRequirementsAnalysisId_ThrowsArgumentException()
        {
            // Arrange
            var request = new ProjectPlanningRequest
            {
                RequirementsAnalysisId = Guid.Empty
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.CreateProjectPlanAsync(request, CancellationToken.None));
        }

        [Fact]
        public async Task CreateProjectPlanAsync_WithUnapprovedRequirements_ThrowsInvalidOperationException()
        {
            // Arrange
            var requirementsAnalysisId = Guid.NewGuid();
            var request = new ProjectPlanningRequest
            {
                RequirementsAnalysisId = requirementsAnalysisId
            };

            _mockRequirementsAnalysisService.Setup(x => x.GetAnalysisResultsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((RequirementsAnalysisResponse)null);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _service.CreateProjectPlanAsync(request, CancellationToken.None));
        }

        [Fact]
        public async Task GetPlanningStatusAsync_WithExistingPlanningId_ReturnsStatus()
        {
            // Arrange
            var planningId = Guid.NewGuid();
            var requirementsAnalysisId = Guid.NewGuid();
            var request = new ProjectPlanningRequest
            {
                RequirementsAnalysisId = requirementsAnalysisId
            };

            var requirementsAnalysis = new RequirementsAnalysisResponse
            {
                AnalysisId = requirementsAnalysisId,
                ProjectDescription = "Build task management system for small teams",
                AnalysisResult = "### Project Overview\\nA task management system for small teams...",
                ReviewId = Guid.NewGuid(),
                Status = RequirementsAnalysisStatus.PendingReview,
                CreatedAt = DateTime.UtcNow
            };

            var instructionContent = new InstructionContent
            {
                ServiceName = "ProjectPlanner",
                Content = "# Role\\nYou are a project planner\\n# Task\\nCreate project plan\\n# Constraints\\nFocus on feasibility",
                LastModified = DateTime.UtcNow,
                IsValid = true,
                ValidationMessage = string.Empty
            };

            var aiResponse = new AIResponse
            {
                Content = "### Project Overview\\nA comprehensive project plan...\\n### Project Roadmap\\n1. Phase 1...\\n### Architectural Decisions\\nTechnology stack...\\n### Milestones\\nKey deliverables...",
                TokensUsed = 300,
                ProviderName = "Claude",
                IsSuccess = true,
                ErrorMessage = null,
                ResponseTime = TimeSpan.FromMilliseconds(800)
            };

            var reviewResponse = new ReviewResponse
            {
                ReviewId = Guid.NewGuid(),
                Status = ReviewStatus.Pending,
                SubmittedAt = DateTime.UtcNow,
                Message = "Review submitted successfully"
            };

            _mockRequirementsAnalysisService.Setup(x => x.GetAnalysisResultsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(requirementsAnalysis);

            _mockRequirementsAnalysisService.Setup(x => x.GetAnalysisStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(RequirementsAnalysisStatus.PendingReview);

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

            // First, perform a planning to set the status
            var planningResult = await _service.CreateProjectPlanAsync(request, CancellationToken.None);

            // Act
            var status = await _service.GetPlanningStatusAsync(planningResult.PlanningId, CancellationToken.None);

            // Assert
            Assert.Equal(ProjectPlanningStatus.PendingReview, status);
        }

        [Fact]
        public async Task GetPlanningStatusAsync_WithUnknownPlanningId_ReturnsFailedStatus()
        {
            // Arrange
            var unknownPlanningId = Guid.NewGuid();

            // Act
            var status = await _service.GetPlanningStatusAsync(unknownPlanningId, CancellationToken.None);

            // Assert
            Assert.Equal(ProjectPlanningStatus.Failed, status);
        }

        [Fact]
        public async Task CanCreatePlanAsync_WithApprovedRequirements_ReturnsTrue()
        {
            // Arrange
            var requirementsAnalysisId = Guid.NewGuid();
            var requirementsAnalysis = new RequirementsAnalysisResponse
            {
                AnalysisId = requirementsAnalysisId,
                ProjectDescription = "Build task management system for small teams",
                AnalysisResult = "### Project Overview\\nA task management system for small teams...",
                ReviewId = Guid.NewGuid(),
                Status = RequirementsAnalysisStatus.PendingReview,
                CreatedAt = DateTime.UtcNow
            };

            _mockRequirementsAnalysisService.Setup(x => x.GetAnalysisResultsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(requirementsAnalysis);

            // Act
            var canCreate = await _service.CanCreatePlanAsync(requirementsAnalysisId, CancellationToken.None);

            // Assert
            Assert.True(canCreate);
        }

        [Fact]
        public async Task CanCreatePlanAsync_WithUnapprovedRequirements_ReturnsFalse()
        {
            // Arrange
            var requirementsAnalysisId = Guid.NewGuid();

            _mockRequirementsAnalysisService.Setup(x => x.GetAnalysisResultsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((RequirementsAnalysisResponse)null);

            // Act
            var canCreate = await _service.CanCreatePlanAsync(requirementsAnalysisId, CancellationToken.None);

            // Assert
            Assert.False(canCreate);
        }
    }
}