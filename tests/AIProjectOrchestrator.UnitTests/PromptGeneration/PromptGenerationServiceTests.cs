using AIProjectOrchestrator.Application.Services;
using AIProjectOrchestrator.Domain.Models.PromptGeneration;
using AIProjectOrchestrator.Domain.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AIProjectOrchestrator.UnitTests.PromptGeneration
{
    public class PromptGenerationServiceTests
    {
        private readonly Mock<ILogger<PromptGenerationService>> _mockLogger;
        private readonly PromptGenerationService _service;

        public PromptGenerationServiceTests()
        {
            _mockLogger = new Mock<ILogger<PromptGenerationService>>();
            _service = new PromptGenerationService(_mockLogger.Object);
        }

        [Fact]
        public async Task GeneratePromptAsync_WithValidRequest_ReturnsResponse()
        {
            // Arrange
            var request = new PromptGenerationRequest
            {
                StoryId = Guid.NewGuid(),
                TechnicalPreferences = new System.Collections.Generic.Dictionary<string, string>
                {
                    { "language", "C#" },
                    { "framework", ".NET 9" }
                },
                PromptStyle = "Detailed"
            };

            // Act
            var result = await _service.GeneratePromptAsync(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(Guid.Empty, result.PromptId);
            Assert.NotEmpty(result.GeneratedPrompt);
            Assert.NotEqual(Guid.Empty, result.ReviewId);
            Assert.Equal(PromptGenerationStatus.PendingReview, result.Status);
            Assert.NotEqual(DateTime.MinValue, result.CreatedAt);
        }

        [Fact]
        public async Task GetPromptStatusAsync_WithValidId_ReturnsStatus()
        {
            // Arrange
            var request = new PromptGenerationRequest
            {
                StoryId = Guid.NewGuid()
            };

            // First, generate a prompt to set the status
            var generationResult = await _service.GeneratePromptAsync(request, CancellationToken.None);

            // Act
            var status = await _service.GetPromptStatusAsync(generationResult.PromptId, CancellationToken.None);

            // Assert
            Assert.Equal(PromptGenerationStatus.PendingReview, status);
        }

        [Fact]
        public async Task CanGeneratePromptAsync_WithValidStoryId_ReturnsTrue()
        {
            // Arrange
            var storyId = Guid.NewGuid();

            // Act
            var result = await _service.CanGeneratePromptAsync(storyId, CancellationToken.None);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task GeneratePromptAsync_WithCancellation_ThrowsOperationCanceledException()
        {
            // Arrange
            var request = new PromptGenerationRequest
            {
                StoryId = Guid.NewGuid()
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                _service.GeneratePromptAsync(request, cancellationTokenSource.Token));
        }
    }
}