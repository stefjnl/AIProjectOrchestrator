using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using AIProjectOrchestrator.Domain.Models.PromptGeneration;
using AIProjectOrchestrator.Domain.Services;
using Moq;
using AIProjectOrchestrator.Application.Services;

namespace AIProjectOrchestrator.IntegrationTests
{
    public class PromptGenerationIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly Mock<IPromptGenerationService> _mockService;

        public PromptGenerationIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _mockService = new Mock<IPromptGenerationService>();
        }

        private WebApplicationFactory<Program> CreateTestFactory()
        {
            var testFactory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IPromptGenerationService>();
                    services.AddSingleton(_mockService.Object);
                });
            });
            return testFactory;
        }

        [Fact]
        public async Task GeneratePrompt_ValidRequest_ReturnsSuccess()
        {
            // Arrange
            var storyGenerationId = Guid.NewGuid();
            var request = new PromptGenerationRequest
            {
                StoryGenerationId = storyGenerationId,
                StoryIndex = 0
            };

            var mockResponse = new PromptGenerationResponse
            {
                PromptId = Guid.NewGuid(),
                GeneratedPrompt = "Test prompt",
                ReviewId = Guid.NewGuid(),
                Status = PromptGenerationStatus.PendingReview,
                CreatedAt = DateTime.UtcNow
            };

            _mockService.Setup(x => x.GeneratePromptAsync(It.IsAny<PromptGenerationRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            var client = CreateTestFactory().CreateClient();
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync("/api/prompts/generate", content);

            // Assert
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<PromptGenerationResponse>(responseString);
            Assert.NotNull(result);
            Assert.Equal(mockResponse.PromptId, result.PromptId);
            Assert.Equal(mockResponse.Status, result.Status);
        }

        [Fact]
        public async Task GeneratePrompt_InvalidStoryGenerationId_ReturnsBadRequest()
        {
            // Arrange
            var request = new PromptGenerationRequest
            {
                StoryGenerationId = Guid.Empty,
                StoryIndex = 0
            };

            var client = CreateTestFactory().CreateClient();
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync("/api/prompts/generate", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GeneratePrompt_UnapprovedStories_ReturnsConflict()
        {
            // Arrange
            var storyGenerationId = Guid.NewGuid();
            var request = new PromptGenerationRequest
            {
                StoryGenerationId = storyGenerationId,
                StoryIndex = 0
            };

            _mockService.Setup(x => x.GeneratePromptAsync(It.IsAny<PromptGenerationRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Prerequisites not met"));

            var client = CreateTestFactory().CreateClient();
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync("/api/prompts/generate", content);

            // Assert
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task GetPromptStatus_ValidId_ReturnsStatus()
        {
            // Arrange
            var promptId = Guid.NewGuid();
            _mockService.Setup(x => x.GetPromptStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(PromptGenerationStatus.PendingReview);

            var client = CreateTestFactory().CreateClient();

            // Act
            var response = await client.GetAsync($"/api/prompts/{promptId}/status");

            // Assert
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<PromptGenerationStatus>(responseString);
            Assert.Equal(PromptGenerationStatus.PendingReview, result);
        }

        [Fact]
        public async Task GetPromptStatus_InvalidId_ReturnsNotFound()
        {
            // Arrange
            var promptId = Guid.NewGuid();
            _mockService.Setup(x => x.GetPromptStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException($"Prompt generation with ID {promptId} not found"));

            var client = CreateTestFactory().CreateClient();

            // Act
            var response = await client.GetAsync($"/api/prompts/{promptId}/status");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CanGeneratePrompt_ValidPrerequisites_ReturnsTrue()
        {
            // Arrange
            var storyGenerationId = Guid.NewGuid();
            _mockService.Setup(x => x.CanGeneratePromptAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var client = CreateTestFactory().CreateClient();

            // Act
            var response = await client.GetAsync($"/api/prompts/can-generate/{storyGenerationId}/0");

            // Assert
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<bool>(responseString);
            Assert.True(result);
        }

        [Fact]
        public async Task CanGeneratePrompt_InvalidPrerequisites_ReturnsFalse()
        {
            // Arrange
            var storyGenerationId = Guid.NewGuid();
            _mockService.Setup(x => x.CanGeneratePromptAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var client = CreateTestFactory().CreateClient();

            // Act
            var response = await client.GetAsync($"/api/prompts/can-generate/{storyGenerationId}/0");

            // Assert
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<bool>(responseString);
            Assert.False(result);
        }

        [Fact]
        public async Task GetPrompt_ValidId_ReturnsPromptDetails()
        {
            // Arrange
            var promptId = Guid.NewGuid();
            var mockResponse = new PromptGenerationResponse
            {
                PromptId = promptId,
                GeneratedPrompt = "Test prompt",
                ReviewId = Guid.NewGuid(),
                Status = PromptGenerationStatus.PendingReview,
                CreatedAt = DateTime.UtcNow
            };

            _mockService.Setup(x => x.GetPromptAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            var client = CreateTestFactory().CreateClient();

            // Act
            var response = await client.GetAsync($"/api/prompts/{promptId}");

            // Assert
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<PromptGenerationResponse>(responseString);
            Assert.NotNull(result);
            Assert.Equal(mockResponse.GeneratedPrompt, result.GeneratedPrompt);
        }
    }
}
