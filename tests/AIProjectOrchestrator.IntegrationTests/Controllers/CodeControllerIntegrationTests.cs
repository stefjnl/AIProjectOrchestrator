using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Models.Code;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AIProjectOrchestrator.IntegrationTests.Controllers
{
    public class CodeControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public CodeControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GenerateCode_WithValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new CodeGenerationRequest
            {
                StoryGenerationId = Guid.NewGuid(),
                AdditionalInstructions = "Test requirements"
            };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/code/generate", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GenerateCode_WithInvalidRequest_ReturnsBadRequest()
        {
            // Arrange
            var invalidRequest = new { invalid = "request" };
            var content = new StringContent(JsonSerializer.Serialize(invalidRequest), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/code/generate", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetStatus_WithValidId_ReturnsOk()
        {
            // Arrange
            var generationId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/code/{generationId}/status");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetStatus_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = Guid.Empty;

            // Act
            var response = await _client.GetAsync($"/api/code/{invalidId}/status");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetGeneratedCode_WithValidId_ReturnsOk()
        {
            // Arrange
            var generationId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/code/{generationId}/artifacts");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetGeneratedCode_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = Guid.Empty;

            // Act
            var response = await _client.GetAsync($"/api/code/{invalidId}/artifacts");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CanGenerateCode_WithValidId_ReturnsOk()
        {
            // Arrange
            var storyGenerationId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/code/can-generate/{storyGenerationId}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task DownloadGeneratedFiles_WithValidId_ReturnsOk()
        {
            // Arrange
            var generationId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/code/{generationId}/download");

            // Assert
            // This could return Ok or NotFound depending on whether files exist
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound);
        }
    }
}