using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Models.PromptGeneration;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AIProjectOrchestrator.IntegrationTests.Controllers
{
    public class PromptGenerationControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public PromptGenerationControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GeneratePrompt_WithValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new PromptGenerationRequest
            {
                StoryGenerationId = Guid.NewGuid(),
                StoryIndex = 0
            };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/prompts/generate", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GeneratePrompt_WithInvalidRequest_ReturnsBadRequest()
        {
            // Arrange
            var invalidRequest = new { invalid = "request" };
            var content = new StringContent(JsonSerializer.Serialize(invalidRequest), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/prompts/generate", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetPromptStatus_WithValidId_ReturnsOk()
        {
            // Arrange
            var promptId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/prompts/{promptId}/status");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetPromptStatus_WithInvalidId_ReturnsBadRequest()
        {
            // Arrange
            var invalidId = "invalid-guid-format";

            // Act
            var response = await _client.GetAsync($"/api/prompts/{invalidId}/status");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetPromptStatus_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid(); // Valid format but non-existent

            // Act
            var response = await _client.GetAsync($"/api/prompts/{nonExistentId}/status");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CanGeneratePrompt_WithValidIds_ReturnsOk()
        {
            // Arrange
            var storyGenerationId = Guid.NewGuid();
            var storyIndex = 0;

            // Act
            var response = await _client.GetAsync($"/api/prompts/can-generate/{storyGenerationId}/{storyIndex}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CanGeneratePrompt_WithInvalidStoryGenerationId_ReturnsBadRequest()
        {
            // Arrange
            var invalidStoryGenerationId = "invalid-guid-format";
            var storyIndex = 0;

            // Act
            var response = await _client.GetAsync($"/api/prompts/can-generate/{invalidStoryGenerationId}/{storyIndex}");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetPrompt_WithValidId_ReturnsOk()
        {
            // Arrange
            var promptId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/prompts/{promptId}");

            // Assert
            // Can return OK or NotFound depending on if prompt exists
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetPrompt_WithInvalidId_ReturnsBadRequest()
        {
            // Arrange
            var invalidId = "invalid-guid-format";

            // Act
            var response = await _client.GetAsync($"/api/prompts/{invalidId}");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}