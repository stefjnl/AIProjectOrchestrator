using AIProjectOrchestrator.Application.Services;
using AIProjectOrchestrator.Domain.Models.PromptGeneration;
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

namespace AIProjectOrchestrator.UnitTests.PromptGeneration
{
    public class PromptGenerationServiceTests
    {
        private readonly Mock<IStoryGenerationService> _mockStoryGenerationService;
        private readonly Mock<IInstructionService> _mockInstructionService;
        private readonly Mock<ILogger<PromptGenerationService>> _mockLogger;
        private readonly PromptGenerationService _service;

        public PromptGenerationServiceTests()
        {
            _mockStoryGenerationService = new Mock<IStoryGenerationService>();
            var mockProjectPlanningService = new Mock<IProjectPlanningService>();
            _mockInstructionService = new Mock<IInstructionService>();
            var mockAIClientFactory = new Mock<IAIClientFactory>();
            var mockLazyReview = new Mock<Lazy<IReviewService>>();
            _mockLogger = new Mock<ILogger<PromptGenerationService>>();
            var mockLoggerAssembler = new Mock<ILogger<PromptContextAssembler>>();
            _service = new PromptGenerationService(
                _mockStoryGenerationService.Object,
                mockProjectPlanningService.Object,
                _mockInstructionService.Object,
                mockAIClientFactory.Object,
                mockLazyReview.Object,
                _mockLogger.Object,
                mockLoggerAssembler.Object);
        }

        [Fact]
        public async Task GeneratePromptAsync_WithValidRequest_ReturnsResponse()
        {
            // Arrange
            var storyGenerationId = Guid.NewGuid();
            var story = new UserStory
            {
                Id = Guid.NewGuid(),
                Title = "Test Story",
                Description = "Test Description",
                AcceptanceCriteria = new List<string> { "Criteria 1", "Criteria 2" },
                Priority = "High",
                StoryPoints = 5,
                Tags = new List<string> { "tag1", "tag2" }
            };

            var request = new PromptGenerationRequest
            {
                StoryGenerationId = storyGenerationId,
                StoryIndex = 0,
                TechnicalPreferences = new Dictionary<string, string>
                {
                    { "language", "C#" },
                    { "framework", ".NET 9" }
                },
                PromptStyle = "Detailed"
            };

            // Setup mock
            _mockStoryGenerationService.Setup(x => x.GetIndividualStoryAsync(
                    It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(story);

            _mockStoryGenerationService.Setup(x => x.GetGenerationStatusAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Domain.Models.Stories.StoryGenerationStatus.Approved);

            _mockStoryGenerationService.Setup(x => x.GetStoryCountAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.GeneratePromptAsync(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(Guid.Empty, result.PromptId);
            Assert.NotEmpty(result.GeneratedPrompt);
            Assert.Contains(story.Title, result.GeneratedPrompt);
            Assert.NotEqual(Guid.Empty, result.ReviewId);
            Assert.Equal(PromptGenerationStatus.PendingReview, result.Status);
            Assert.NotEqual(DateTime.MinValue, result.CreatedAt);
        }

        [Fact]
        public async Task GetPromptStatusAsync_WithValidId_ReturnsStatus()
        {
            // Arrange
            var storyGenerationId = Guid.NewGuid();
            var story = new UserStory
            {
                Id = Guid.NewGuid(),
                Title = "Test Story",
                Description = "Test Description",
                AcceptanceCriteria = new List<string> { "Criteria 1", "Criteria 2" },
                Priority = "High",
                StoryPoints = 5,
                Tags = new List<string> { "tag1", "tag2" }
            };

            var request = new PromptGenerationRequest
            {
                StoryGenerationId = storyGenerationId,
                StoryIndex = 0
            };

            // Setup mock
            _mockStoryGenerationService.Setup(x => x.GetIndividualStoryAsync(
                    It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(story);

            _mockStoryGenerationService.Setup(x => x.GetGenerationStatusAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Domain.Models.Stories.StoryGenerationStatus.Approved);

            _mockStoryGenerationService.Setup(x => x.GetStoryCountAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // First, generate a prompt to set the status
            var generationResult = await _service.GeneratePromptAsync(request, CancellationToken.None);

            // Act
            var status = await _service.GetPromptStatusAsync(generationResult.PromptId, CancellationToken.None);

            // Assert
            Assert.Equal(PromptGenerationStatus.PendingReview, status);
        }

        [Fact]
        public async Task CanGeneratePromptAsync_WithApprovedStories_ReturnsTrue()
        {
            // Arrange
            var storyGenerationId = Guid.NewGuid();
            var storyIndex = 0;

            // Setup mock
            _mockStoryGenerationService.Setup(x => x.GetGenerationStatusAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Domain.Models.Stories.StoryGenerationStatus.Approved);

            _mockStoryGenerationService.Setup(x => x.GetStoryCountAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(5); // 5 stories available

            // Act
            var result = await _service.CanGeneratePromptAsync(storyGenerationId, storyIndex, CancellationToken.None);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CanGeneratePromptAsync_WithUnapprovedStories_ReturnsFalse()
        {
            // Arrange
            var storyGenerationId = Guid.NewGuid();
            var storyIndex = 0;

            // Setup mock
            _mockStoryGenerationService.Setup(x => x.GetGenerationStatusAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Domain.Models.Stories.StoryGenerationStatus.PendingReview);

            _mockStoryGenerationService.Setup(x => x.GetStoryCountAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(5); // 5 stories available

            // Act
            var result = await _service.CanGeneratePromptAsync(storyGenerationId, storyIndex, CancellationToken.None);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CanGeneratePromptAsync_WithInvalidStoryIndex_ReturnsFalse()
        {
            // Arrange
            var storyGenerationId = Guid.NewGuid();
            var storyIndex = 10; // Index out of range

            // Setup mock
            _mockStoryGenerationService.Setup(x => x.GetGenerationStatusAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Domain.Models.Stories.StoryGenerationStatus.Approved);

            _mockStoryGenerationService.Setup(x => x.GetStoryCountAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(5); // Only 5 stories available, index 10 is invalid

            // Act
            var result = await _service.CanGeneratePromptAsync(storyGenerationId, storyIndex, CancellationToken.None);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GeneratePromptAsync_WithInvalidPrerequisites_ThrowsException()
        {
            // Arrange
            var storyGenerationId = Guid.NewGuid();
            var request = new PromptGenerationRequest
            {
                StoryGenerationId = storyGenerationId,
                StoryIndex = 0,
                TechnicalPreferences = new Dictionary<string, string>
                {
                    { "language", "C#" },
                    { "framework", ".NET 9" }
                },
                PromptStyle = "Detailed"
            };

            // Setup mock to return unapproved status
            _mockStoryGenerationService.Setup(x => x.GetGenerationStatusAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Domain.Models.Stories.StoryGenerationStatus.PendingReview);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.GeneratePromptAsync(request, CancellationToken.None));
        }

        [Fact]
        public async Task GeneratePromptAsync_WithCancellation_ThrowsOperationCanceledException()
        {
            // Arrange
            var storyGenerationId = Guid.NewGuid();
            var request = new PromptGenerationRequest
            {
                StoryGenerationId = storyGenerationId,
                StoryIndex = 0
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                _service.GeneratePromptAsync(request, cancellationTokenSource.Token));
        }
    }
}
