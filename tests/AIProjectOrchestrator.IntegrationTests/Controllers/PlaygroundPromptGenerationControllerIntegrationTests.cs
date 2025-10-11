using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AIProjectOrchestrator.IntegrationTests.Controllers
{
    public class PlaygroundPromptGenerationControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public PlaygroundPromptGenerationControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        // Adding the PlaygroundPromptRequest class that's defined in the controller
        public class PlaygroundPromptRequest
        {
            public string? PromptContent { get; set; }
        }

        [Fact]
        public async Task GeneratePrompt_WithValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new PlaygroundPromptRequest
            {
                PromptContent = "Test prompt content"
            };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/playground-prompt-generation", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GeneratePrompt_WithEmptyContent_ReturnsBadRequest()
        {
            // Arrange
            var request = new PlaygroundPromptRequest
            {
                PromptContent = ""
            };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/playground-prompt-generation", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GeneratePrompt_WithNullContent_ReturnsBadRequest()
        {
            // Arrange
            var request = new PlaygroundPromptRequest
            {
                PromptContent = null
            };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/playground-prompt-generation", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GeneratePrompt_WithInvalidRequest_ReturnsBadRequest()
        {
            // Arrange
            var invalidRequest = new { invalid = "request" };
            var content = new StringContent(JsonSerializer.Serialize(invalidRequest), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/playground-prompt-generation", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}