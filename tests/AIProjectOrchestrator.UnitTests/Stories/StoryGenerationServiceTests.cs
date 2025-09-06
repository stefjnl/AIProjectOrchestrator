using AIProjectOrchestrator.Application.Services;
using AIProjectOrchestrator.Domain.Models;
using AIProjectOrchestrator.Domain.Models.AI;
using AIProjectOrchestrator.Domain.Models.Review;
using AIProjectOrchestrator.Domain.Models.Stories;
using AIProjectOrchestrator.Domain.Services;
using AIProjectOrchestrator.Infrastructure.AI;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AIProjectOrchestrator.UnitTests.Stories
{
    public class StoryGenerationServiceTests
    {
        private readonly Mock<IRequirementsAnalysisService> _mockRequirementsAnalysisService;
        private readonly Mock<IProjectPlanningService> _mockProjectPlanningService;
        private readonly Mock<IInstructionService> _mockInstructionService;
        private readonly Mock<IAIClientFactory> _mockAIClientFactory;
        private readonly Mock<IReviewService> _mockReviewService;
        private readonly Mock<ILogger<StoryGenerationService>> _mockLogger;
        private readonly StoryGenerationService _service;

        public StoryGenerationServiceTests()
        {
            _mockRequirementsAnalysisService = new Mock<IRequirementsAnalysisService>();
            _mockProjectPlanningService = new Mock<IProjectPlanningService>();
            _mockInstructionService = new Mock<IInstructionService>();
            _mockAIClientFactory = new Mock<IAIClientFactory>();
            _mockReviewService = new Mock<IReviewService>();
            _mockLogger = new Mock<ILogger<StoryGenerationService>>();
            
            // For unit tests, we'll mock the repositories as well
            var mockStoryGenerationRepository = new Mock<Domain.Interfaces.IStoryGenerationRepository>();
            var mockProjectPlanningRepository = new Mock<Domain.Interfaces.IProjectPlanningRepository>();
            
            _service = new StoryGenerationService(
                _mockRequirementsAnalysisService.Object,
                _mockProjectPlanningService.Object,
                _mockInstructionService.Object,
                _mockAIClientFactory.Object,
                new Lazy<IReviewService>(() => _mockReviewService.Object),
                _mockLogger.Object,
                mockStoryGenerationRepository.Object,
                mockProjectPlanningRepository.Object);
        }

        [Fact]
        public async Task GenerateStoriesAsync_ValidPlanningId_ReturnsStoryGenerationResponse()
        {
            // Arrange
            var planningId = Guid.NewGuid();
            var requirementsAnalysisId = Guid.NewGuid();
            var request = new StoryGenerationRequest
            {
                PlanningId = planningId,
                StoryPreferences = "Focus on user authentication features",
                ComplexityLevels = "Simple, Medium, Complex",
                AdditionalGuidance = "Include security considerations"
            };

            var instructionContent = new InstructionContent
            {
                ServiceName = "StoryGenerator",
                Content = "# Role\nYou are a story generation specialist\n# Task\nGenerate user stories\n# Constraints\nFocus on clarity",
                LastModified = DateTime.UtcNow,
                IsValid = true,
                ValidationMessage = string.Empty
            };

            var aiResponse = new AIResponse
            {
                Content = "## User Stories\n\n### Story 1\n**Title:** User Login\n**Description:** As a user, I want to log in to the system\n**Acceptance Criteria:**\n- User can enter username and password\n- System validates credentials\n**Priority:** High\n**Estimated Complexity:** Simple",
                TokensUsed = 200,
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

            _mockProjectPlanningService.Setup(x => x.GetRequirementsAnalysisIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(requirementsAnalysisId);

            _mockRequirementsAnalysisService.Setup(x => x.CanAnalyzeRequirementsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockProjectPlanningService.Setup(x => x.CanCreatePlanAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockProjectPlanningService.Setup(x => x.GetPlanningResultContentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("# Project Planning Content\nThis is the planning content.");

            _mockRequirementsAnalysisService.Setup(x => x.GetAnalysisResultContentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("# Requirements Analysis Content\nThis is the requirements content.");

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
            var result = await _service.GenerateStoriesAsync(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(Guid.Empty, result.GenerationId);
            Assert.NotNull(result.Stories);
            Assert.NotEqual(Guid.Empty, result.ReviewId);
            Assert.Equal(StoryGenerationStatus.PendingReview, result.Status);
            Assert.NotEqual(DateTime.MinValue, result.CreatedAt);
        }

        [Fact]
        public async Task GenerateStoriesAsync_InvalidPlanningId_ThrowsArgumentException()
        {
            // Arrange
            var request = new StoryGenerationRequest
            {
                PlanningId = Guid.Empty // Invalid - empty
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.GenerateStoriesAsync(request, CancellationToken.None));
        }

        [Fact]
        public async Task GenerateStoriesAsync_PlanningNotApproved_ThrowsInvalidOperationException_Duplicate()
        {
            // Arrange
            var planningId = Guid.NewGuid();
            var request = new StoryGenerationRequest
            {
                PlanningId = planningId
            };

            _mockProjectPlanningService.Setup(x => x.CanCreatePlanAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.GenerateStoriesAsync(request, CancellationToken.None));
        }

        [Fact]
        public async Task GenerateStoriesAsync_PlanningNotApproved_ThrowsInvalidOperationException()
        {
            // Arrange
            var planningId = Guid.NewGuid();
            var request = new StoryGenerationRequest
            {
                PlanningId = planningId
            };

            _mockProjectPlanningService.Setup(x => x.CanCreatePlanAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _service.GenerateStoriesAsync(request, CancellationToken.None));
        }

        [Fact]
        public async Task GenerateStoriesAsync_AIProviderFails_ThrowsInvalidOperationException()
        {
            // Arrange
            var planningId = Guid.NewGuid();
            var requirementsAnalysisId = Guid.NewGuid();
            var request = new StoryGenerationRequest
            {
                PlanningId = planningId
            };

            var instructionContent = new InstructionContent
            {
                ServiceName = "StoryGenerator",
                Content = "# Role\nYou are a story generation specialist\n# Task\nGenerate user stories\n# Constraints\nFocus on clarity",
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

            _mockProjectPlanningService.Setup(x => x.GetRequirementsAnalysisIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(requirementsAnalysisId);

            _mockRequirementsAnalysisService.Setup(x => x.CanAnalyzeRequirementsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockProjectPlanningService.Setup(x => x.CanCreatePlanAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

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
                _service.GenerateStoriesAsync(request, CancellationToken.None));
        }

        [Fact]
        public async Task GenerateStoriesAsync_ContextTooLarge_LogsWarningAndProceeds()
        {
            // Arrange
            var planningId = Guid.NewGuid();
            var requirementsAnalysisId = Guid.NewGuid();
            var request = new StoryGenerationRequest
            {
                PlanningId = planningId
            };

            var instructionContent = new InstructionContent
            {
                ServiceName = "StoryGenerator",
                Content = new string('A', 200000), // Large content to trigger warning
                LastModified = DateTime.UtcNow,
                IsValid = true,
                ValidationMessage = string.Empty
            };

            var aiResponse = new AIResponse
            {
                Content = "## User Stories\n\n### Story 1\n**Title:** User Login\n**Description:** As a user, I want to log in to the system\n**Acceptance Criteria:**\n- User can enter username and password\n- System validates credentials\n**Priority:** High\n**Estimated Complexity:** Simple",
                TokensUsed = 200,
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

            _mockProjectPlanningService.Setup(x => x.GetRequirementsAnalysisIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(requirementsAnalysisId);

            _mockRequirementsAnalysisService.Setup(x => x.CanAnalyzeRequirementsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockProjectPlanningService.Setup(x => x.CanCreatePlanAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockProjectPlanningService.Setup(x => x.GetPlanningResultContentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("# Project Planning Content\nThis is the planning content.");

            _mockRequirementsAnalysisService.Setup(x => x.GetAnalysisResultContentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("# Requirements Analysis Content\nThis is the requirements content.");

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
            var result = await _service.GenerateStoriesAsync(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(StoryGenerationStatus.PendingReview, result.Status);
        }

        [Fact]
        public async Task GetGenerationStatusAsync_ValidId_ReturnsCorrectStatus()
        {
            // Arrange
            var planningId = Guid.NewGuid();
            var requirementsAnalysisId = Guid.NewGuid();
            var request = new StoryGenerationRequest
            {
                PlanningId = planningId
            };

            var instructionContent = new InstructionContent
            {
                ServiceName = "StoryGenerator",
                Content = "# Role\nYou are a story generation specialist\n# Task\nGenerate user stories\n# Constraints\nFocus on clarity",
                LastModified = DateTime.UtcNow,
                IsValid = true,
                ValidationMessage = string.Empty
            };

            var aiResponse = new AIResponse
            {
                Content = "## User Stories\n\n### Story 1\n**Title:** User Login\n**Description:** As a user, I want to log in to the system\n**Acceptance Criteria:**\n- User can enter username and password\n- System validates credentials\n**Priority:** High\n**Estimated Complexity:** Simple",
                TokensUsed = 200,
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

            _mockProjectPlanningService.Setup(x => x.GetRequirementsAnalysisIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(requirementsAnalysisId);

            _mockRequirementsAnalysisService.Setup(x => x.CanAnalyzeRequirementsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockProjectPlanningService.Setup(x => x.CanCreatePlanAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockProjectPlanningService.Setup(x => x.GetPlanningResultContentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("# Project Planning Content\nThis is the planning content.");

            _mockRequirementsAnalysisService.Setup(x => x.GetAnalysisResultContentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("# Requirements Analysis Content\nThis is the requirements content.");

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

            // First, perform a generation to set the status
            var generationResult = await _service.GenerateStoriesAsync(request, CancellationToken.None);

            // Act
            var status = await _service.GetGenerationStatusAsync(generationResult.GenerationId, CancellationToken.None);

            // Assert
            Assert.Equal(StoryGenerationStatus.PendingReview, status);
        }

        [Fact]
        public async Task GetGenerationStatusAsync_InvalidId_ReturnsFailedStatus()
        {
            // Arrange
            var unknownGenerationId = Guid.NewGuid();

            // Act
            var status = await _service.GetGenerationStatusAsync(unknownGenerationId, CancellationToken.None);

            // Assert
            Assert.Equal(StoryGenerationStatus.Failed, status);
        }

        [Fact]
        public async Task GetGenerationResultsAsync_ValidApprovedId_ReturnsStoryCollection()
        {
            // Arrange
            var planningId = Guid.NewGuid();
            var requirementsAnalysisId = Guid.NewGuid();
            var request = new StoryGenerationRequest
            {
                PlanningId = planningId
            };

            var instructionContent = new InstructionContent
            {
                ServiceName = "StoryGenerator",
                Content = "# Role\nYou are a story generation specialist\n# Task\nGenerate user stories\n# Constraints\nFocus on clarity",
                LastModified = DateTime.UtcNow,
                IsValid = true,
                ValidationMessage = string.Empty
            };

            var aiResponse = new AIResponse
            {
                Content = "## User Stories\n\n### Story 1\n**Title:** User Login\n**Description:** As a user, I want to log in to the system\n**Acceptance Criteria:**\n- User can enter username and password\n- System validates credentials\n**Priority:** High\n**Estimated Complexity:** Simple",
                TokensUsed = 200,
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

            _mockProjectPlanningService.Setup(x => x.GetRequirementsAnalysisIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(requirementsAnalysisId);

            _mockRequirementsAnalysisService.Setup(x => x.CanAnalyzeRequirementsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockProjectPlanningService.Setup(x => x.CanCreatePlanAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockProjectPlanningService.Setup(x => x.GetPlanningResultContentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("# Project Planning Content\nThis is the planning content.");

            _mockRequirementsAnalysisService.Setup(x => x.GetAnalysisResultContentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("# Requirements Analysis Content\nThis is the requirements content.");

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

            // First, perform a generation to store the results
            var generationResult = await _service.GenerateStoriesAsync(request, CancellationToken.None);

            // Act
            var result = await _service.GetGenerationResultsAsync(generationResult.GenerationId, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task CanGenerateStoriesAsync_ValidApprovedPlan_ReturnsTrue()
        {
            // Arrange
            var planningId = Guid.NewGuid();

            _mockProjectPlanningService.Setup(x => x.CanCreatePlanAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var canGenerate = await _service.CanGenerateStoriesAsync(planningId, CancellationToken.None);

            // Assert
            Assert.True(canGenerate);
        }

        [Fact]
        public async Task CanGenerateStoriesAsync_UnapprovedDependencies_ReturnsFalse()
        {
            // Arrange
            var planningId = Guid.NewGuid();

            _mockProjectPlanningService.Setup(x => x.CanCreatePlanAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            var canGenerate = await _service.CanGenerateStoriesAsync(planningId, CancellationToken.None);

            // Assert
            Assert.False(canGenerate);
        }

        [Fact]
        public async Task ParseAIResponseToStories_ValidResponse_ReturnsStoryCollection()
        {
            // Arrange
            var aiResponse = "## User Stories\n\n### Story 1\n**Title:** User Login\n**Description:** As a user, I want to log in to the system\n**Acceptance Criteria:**\n- User can enter username and password\n- System validates credentials\n**Priority:** High\n**Estimated Complexity:** Simple";

            // Act
            var result = await _service.ParseAIResponseToStories(aiResponse, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Equal("User Login", result[0].Title);
            Assert.Equal("As a user, I want to log in to the system", result[0].Description);
            Assert.NotEmpty(result[0].AcceptanceCriteria);
            Assert.Equal("High", result[0].Priority);
            Assert.Equal("Simple", result[0].EstimatedComplexity);
        }

        [Fact]
        public async Task GenerateStoriesAsync_RequirementsNotApproved_ThrowsInvalidOperationException()
        {
            // Arrange
            var planningId = Guid.NewGuid();
            var requirementsAnalysisId = Guid.NewGuid();
            var request = new StoryGenerationRequest
            {
                PlanningId = planningId
            };

            _mockProjectPlanningService.Setup(x => x.GetRequirementsAnalysisIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(requirementsAnalysisId);

            _mockRequirementsAnalysisService.Setup(x => x.CanAnalyzeRequirementsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false); // Requirements not approved

            _mockProjectPlanningService.Setup(x => x.CanCreatePlanAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.GenerateStoriesAsync(request, CancellationToken.None));
        }

        [Fact]
        public async Task GenerateStoriesAsync_PlanningNotFound_ThrowsInvalidOperationException()
        {
            // Arrange
            var planningId = Guid.NewGuid();
            var request = new StoryGenerationRequest
            {
                PlanningId = planningId
            };

            _mockProjectPlanningService.Setup(x => x.GetRequirementsAnalysisIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid?)null); // Planning not found

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.GenerateStoriesAsync(request, CancellationToken.None));
        }

        [Fact]
        public async Task GenerateStoriesAsync_IncludesRequirementsContentInPrompt()
        {
            // Arrange
            var planningId = Guid.NewGuid();
            var requirementsAnalysisId = Guid.NewGuid();
            var request = new StoryGenerationRequest
            {
                PlanningId = planningId,
                StoryPreferences = "Focus on user authentication features"
            };

            var instructionContent = new InstructionContent
            {
                ServiceName = "StoryGenerator",
                Content = "# Role\nYou are a story generation specialist\n# Task\nGenerate user stories\n# Constraints\nFocus on clarity",
                LastModified = DateTime.UtcNow,
                IsValid = true,
                ValidationMessage = string.Empty
            };

            var aiResponse = new AIResponse
            {
                Content = "## User Stories\n\n### Story 1\n**Title:** User Login\n**Description:** As a user, I want to log in to the system\n**Acceptance Criteria:**\n- User can enter username and password\n- System validates credentials\n**Priority:** High\n**Estimated Complexity:** Simple",
                TokensUsed = 200,
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

            _mockProjectPlanningService.Setup(x => x.GetRequirementsAnalysisIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(requirementsAnalysisId);

            _mockRequirementsAnalysisService.Setup(x => x.CanAnalyzeRequirementsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockProjectPlanningService.Setup(x => x.CanCreatePlanAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockProjectPlanningService.Setup(x => x.GetPlanningResultContentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("# Project Planning Content\nThis is the planning content.");

            _mockRequirementsAnalysisService.Setup(x => x.GetAnalysisResultContentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("# Requirements Analysis Content\nThis is the requirements content.");

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
            var result = await _service.GenerateStoriesAsync(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(StoryGenerationStatus.PendingReview, result.Status);
            // Verify that the AI client was called with a prompt containing both requirements and planning content
            mockAIClient.Verify(x => x.CallAsync(It.Is<AIRequest>(r =>
                r.Prompt.Contains("# Requirements Analysis Content") &&
                r.Prompt.Contains("# Project Planning Content") &&
                r.Prompt.Contains("Focus on user authentication features")), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact]
        public async Task GetIndividualStoryAsync_WithValidIndex_ReturnsStory()
        {
            // Arrange
            var planningId = Guid.NewGuid();
            var request = new StoryGenerationRequest
            {
                PlanningId = planningId
            };

            var instructionContent = new InstructionContent
            {
                ServiceName = "StoryGenerator",
                Content = "# Role\nYou are a story generator\n# Task\nGenerate stories\n# Constraints\nFocus on clarity",
                LastModified = DateTime.UtcNow,
                IsValid = true,
                ValidationMessage = string.Empty
            };

            var aiResponse = new AIResponse
            {
                Content = "### Story 1\n**Title:** User Login\n**Description:** As a user, I want to log in\n**Acceptance Criteria:**\n- User can enter credentials\n- System validates credentials\n**Priority:** High\n**Estimated Complexity:** Medium",
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

            _mockProjectPlanningService.Setup(x => x.GetRequirementsAnalysisIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Guid.NewGuid());

            _mockRequirementsAnalysisService.Setup(x => x.CanAnalyzeRequirementsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockProjectPlanningService.Setup(x => x.CanCreatePlanAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockProjectPlanningService.Setup(x => x.GetPlanningResultContentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("# Project Planning Content\nThis is the planning content.");

            _mockRequirementsAnalysisService.Setup(x => x.GetAnalysisResultContentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("# Requirements Analysis Content\nThis is the requirements content.");

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

            // First, generate stories
            var generationResult = await _service.GenerateStoriesAsync(request, CancellationToken.None);

            // Act
            var story = await _service.GetIndividualStoryAsync(generationResult.GenerationId, 0, CancellationToken.None);

            // Assert
            Assert.NotNull(story);
            Assert.Equal("User Login", story.Title);
            Assert.Equal("As a user, I want to log in", story.Description);
            Assert.Contains("User can enter credentials", story.AcceptanceCriteria);
            Assert.Equal("High", story.Priority);
        }
        
        [Fact]
        public async Task GetAllStoriesAsync_WithValidId_ReturnsAllStories()
        {
            // Arrange
            var planningId = Guid.NewGuid();
            var request = new StoryGenerationRequest
            {
                PlanningId = planningId
            };

            var instructionContent = new InstructionContent
            {
                ServiceName = "StoryGenerator",
                Content = "# Role\nYou are a story generator\n# Task\nGenerate stories\n# Constraints\nFocus on clarity",
                LastModified = DateTime.UtcNow,
                IsValid = true,
                ValidationMessage = string.Empty
            };

            var aiResponse = new AIResponse
            {
                Content = "### Story 1\n**Title:** User Login\n**Description:** As a user, I want to log in\n**Acceptance Criteria:**\n- User can enter credentials\n- System validates credentials\n**Priority:** High\n**Estimated Complexity:** Medium\n\n" +
                          "### Story 2\n**Title:** User Logout\n**Description:** As a user, I want to log out\n**Acceptance Criteria:**\n- User can click logout button\n- System clears session\n**Priority:** Medium\n**Estimated Complexity:** Low",
                TokensUsed = 300,
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

            _mockProjectPlanningService.Setup(x => x.GetRequirementsAnalysisIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Guid.NewGuid());

            _mockRequirementsAnalysisService.Setup(x => x.CanAnalyzeRequirementsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockProjectPlanningService.Setup(x => x.CanCreatePlanAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockProjectPlanningService.Setup(x => x.GetPlanningResultContentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("# Project Planning Content\nThis is the planning content.");

            _mockRequirementsAnalysisService.Setup(x => x.GetAnalysisResultContentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("# Requirements Analysis Content\nThis is the requirements content.");

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

            // First, generate stories
            var generationResult = await _service.GenerateStoriesAsync(request, CancellationToken.None);

            // Act
            var stories = await _service.GetAllStoriesAsync(generationResult.GenerationId, CancellationToken.None);

            // Assert
            Assert.NotNull(stories);
            Assert.Equal(2, stories.Count);
            Assert.Equal("User Login", stories[0].Title);
            Assert.Equal("User Logout", stories[1].Title);
        }
        
        [Fact]
        public async Task GetStoryCountAsync_WithValidId_ReturnsCorrectCount()
        {
            // Arrange
            var planningId = Guid.NewGuid();
            var request = new StoryGenerationRequest
            {
                PlanningId = planningId
            };

            var instructionContent = new InstructionContent
            {
                ServiceName = "StoryGenerator",
                Content = "# Role\nYou are a story generator\n# Task\nGenerate stories\n# Constraints\nFocus on clarity",
                LastModified = DateTime.UtcNow,
                IsValid = true,
                ValidationMessage = string.Empty
            };

            var aiResponse = new AIResponse
            {
                Content = "### Story 1\n**Title:** User Login\n**Description:** As a user, I want to log in\n**Acceptance Criteria:**\n- User can enter credentials\n- System validates credentials\n**Priority:** High\n**Estimated Complexity:** Medium\n\n" +
                          "### Story 2\n**Title:** User Logout\n**Description:** As a user, I want to log out\n**Acceptance Criteria:**\n- User can click logout button\n- System clears session\n**Priority:** Medium\n**Estimated Complexity:** Low\n\n" +
                          "### Story 3\n**Title:** User Profile\n**Description:** As a user, I want to view my profile\n**Acceptance Criteria:**\n- User can see profile info\n- User can edit profile\n**Priority:** Low\n**Estimated Complexity:** Medium",
                TokensUsed = 450,
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

            _mockProjectPlanningService.Setup(x => x.GetRequirementsAnalysisIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Guid.NewGuid());

            _mockRequirementsAnalysisService.Setup(x => x.CanAnalyzeRequirementsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockProjectPlanningService.Setup(x => x.CanCreatePlanAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockProjectPlanningService.Setup(x => x.GetPlanningResultContentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("# Project Planning Content\nThis is the planning content.");

            _mockRequirementsAnalysisService.Setup(x => x.GetAnalysisResultContentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("# Requirements Analysis Content\nThis is the requirements content.");

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

            // First, generate stories
            var generationResult = await _service.GenerateStoriesAsync(request, CancellationToken.None);

            // Act
            var count = await _service.GetStoryCountAsync(generationResult.GenerationId, CancellationToken.None);

            // Assert
            Assert.Equal(3, count);
        }
        
        [Fact]
        public async Task GetIndividualStoryAsync_WithInvalidIndex_ThrowsException()
        {
            // Arrange
            var planningId = Guid.NewGuid();
            var request = new StoryGenerationRequest
            {
                PlanningId = planningId
            };

            var instructionContent = new InstructionContent
            {
                ServiceName = "StoryGenerator",
                Content = "# Role\nYou are a story generator\n# Task\nGenerate stories\n# Constraints\nFocus on clarity",
                LastModified = DateTime.UtcNow,
                IsValid = true,
                ValidationMessage = string.Empty
            };

            var aiResponse = new AIResponse
            {
                Content = "### Story 1\n**Title:** User Login\n**Description:** As a user, I want to log in\n**Acceptance Criteria:**\n- User can enter credentials\n- System validates credentials\n**Priority:** High\n**Estimated Complexity:** Medium",
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

            _mockProjectPlanningService.Setup(x => x.GetRequirementsAnalysisIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Guid.NewGuid());

            _mockRequirementsAnalysisService.Setup(x => x.CanAnalyzeRequirementsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockProjectPlanningService.Setup(x => x.CanCreatePlanAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockProjectPlanningService.Setup(x => x.GetPlanningResultContentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("# Project Planning Content\nThis is the planning content.");

            _mockRequirementsAnalysisService.Setup(x => x.GetAnalysisResultContentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("# Requirements Analysis Content\nThis is the requirements content.");

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

            // First, generate stories
            var generationResult = await _service.GenerateStoriesAsync(request, CancellationToken.None);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => 
                _service.GetIndividualStoryAsync(generationResult.GenerationId, 5, CancellationToken.None)); // Index 5 is out of range
        }
    }
}
