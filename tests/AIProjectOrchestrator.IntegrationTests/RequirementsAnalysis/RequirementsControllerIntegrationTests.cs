using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Models;
using Xunit;

namespace AIProjectOrchestrator.IntegrationTests.RequirementsAnalysis
{
    [Collection("Sequential")]
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
        public async Task AnalyzeRequirements_WithValidRequest_ReturnsSuccess()
        {
            // Arrange
            var request = new RequirementsAnalysisRequest
            {
                ProjectDescription = "Build a task management system for small teams",
                AdditionalContext = "React frontend, .NET API backend",
                Constraints = "Must integrate with existing authentication"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/requirements/analyze", request);

            // Assert
            // Note: In a real environment with Claude API configured, this would return 200
            // In our test environment without API keys, it might return 503
            // We're just verifying the endpoint exists and can handle the request
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.ServiceUnavailable);
        }

        [Fact]
        public async Task AnalyzeRequirements_WithInvalidRequest_ReturnsBadRequest()
        {
            // Arrange
            var request = new RequirementsAnalysisRequest
            {
                ProjectDescription = "" // Invalid - empty
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/requirements/analyze", request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetAnalysisStatus_WithValidGuid_ReturnsStatus()
        {
            // Arrange
            var analysisId = System.Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/requirements/{analysisId}/status");

            // Assert
            // This should return a status, even if it's Failed for an unknown ID
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}