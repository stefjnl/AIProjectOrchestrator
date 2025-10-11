using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Models.AI;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AIProjectOrchestrator.IntegrationTests.Controllers
{
    public class AITestControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public AITestControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task TestClaude_WithValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new AIRequest
            {
                Prompt = "Test prompt",
                ModelName = "claude-3-haiku-20240307"
            };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/aitest/claude", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task TestClaude_WithInvalidRequest_ReturnsBadRequest()
        {
            // Arrange
            var invalidRequest = new { invalid = "request" };
            var content = new StringContent(JsonSerializer.Serialize(invalidRequest), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/aitest/claude", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task TestLMStudio_WithValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new AIRequest
            {
                Prompt = "Test prompt",
                ModelName = "llama"
            };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/aitest/lmstudio", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task TestLMStudio_WithInvalidRequest_ReturnsBadRequest()
        {
            // Arrange
            var invalidRequest = new { invalid = "request" };
            var content = new StringContent(JsonSerializer.Serialize(invalidRequest), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/aitest/lmstudio", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task TestOpenRouter_WithValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new AIRequest
            {
                Prompt = "Test prompt",
                ModelName = "openai/gpt-3.5-turbo"
            };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/aitest/openrouter", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task TestOpenRouter_WithInvalidRequest_ReturnsBadRequest()
        {
            // Arrange
            var invalidRequest = new { invalid = "request" };
            var content = new StringContent(JsonSerializer.Serialize(invalidRequest), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/aitest/openrouter", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}