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
    public class AIControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public AIControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Generate_WithValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new AIRequest
            {
                Prompt = "Test prompt",
                ModelName = "claude-3-haiku-20240307"
            };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/ai/generate", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Generate_WithInvalidRequest_ReturnsBadRequest()
        {
            // Arrange
            var invalidRequest = new { invalid = "request" };
            var content = new StringContent(JsonSerializer.Serialize(invalidRequest), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/ai/generate", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}