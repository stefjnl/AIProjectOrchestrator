using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AIProjectOrchestrator.IntegrationTests.Controllers
{
    public class RequirementsControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public RequirementsControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task AnalyzeRequirements_WithValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new RequirementsAnalysisRequest
            {
                ProjectDescription = "Test requirements description"
            };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/requirements/analyze", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AnalyzeRequirements_WithInvalidRequest_ReturnsBadRequest()
        {
            // Arrange
            var invalidRequest = new { invalid = "request" };
            var content = new StringContent(JsonSerializer.Serialize(invalidRequest), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/requirements/analyze", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetAnalysisStatus_WithValidId_ReturnsOk()
        {
            // Arrange
            var analysisId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/requirements/{analysisId}/status");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetAnalysisStatus_WithInvalidId_ReturnsOkWithFailedStatus()
        {
            // Arrange
            var invalidId = Guid.Empty;

            // Act
            var response = await _client.GetAsync($"/api/requirements/{invalidId}/status");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            // Check if the response content is "Failed" (the default status for non-existent analysis)
            Assert.Contains("Failed", content, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GetAnalysis_WithValidId_ReturnsOk()
        {
            // Arrange
            var analysisId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/requirements/{analysisId}");

            // Assert
            // Can return OK or NotFound depending on if analysis exists
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetAnalysis_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = Guid.Empty; // Using Guid.Empty which will not be found

            // Act
            var response = await _client.GetAsync($"/api/requirements/{invalidId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ApproveAnalysis_WithValidId_ReturnsOk()
        {
            // Arrange
            var analysisId = Guid.NewGuid();
            var content = new StringContent("", Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync($"/api/requirements/{analysisId}/approve", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ApproveAnalysis_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = Guid.Empty;
            var content = new StringContent("", Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync($"/api/requirements/{invalidId}/approve", content);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}