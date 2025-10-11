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
    public class ProjectPlanningControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ProjectPlanningControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task CreateProjectPlan_WithValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new ProjectPlanningRequest
            {
                RequirementsAnalysisId = Guid.NewGuid()
            };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/projectplanning/create", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CreateProjectPlan_WithInvalidRequest_ReturnsBadRequest()
        {
            // Arrange
            var invalidRequest = new { invalid = "request" };
            var content = new StringContent(JsonSerializer.Serialize(invalidRequest), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/projectplanning/create", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetPlanningStatus_WithValidId_ReturnsOk()
        {
            // Arrange
            var planningId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/projectplanning/{planningId}/status");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetPlanningStatus_WithInvalidId_ReturnsOkWithFailedStatus()
        {
            // Arrange
            var invalidId = Guid.Empty;

            // Act
            var response = await _client.GetAsync($"/api/projectplanning/{invalidId}/status");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            // Check if the response content is "Failed" (the default status for non-existent planning)
            Assert.Contains("Failed", content, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GetPlanning_WithValidId_ReturnsOk()
        {
            // Arrange
            var planningId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/projectplanning/{planningId}");

            // Assert
            // Can return OK or NotFound depending on if planning exists
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetPlanning_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = Guid.Empty;

            // Act
            var response = await _client.GetAsync($"/api/projectplanning/{invalidId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CanCreatePlan_WithValidRequirementsAnalysisId_ReturnsOk()
        {
            // Arrange
            var requirementsAnalysisId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/projectplanning/can-create/{requirementsAnalysisId}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CanCreatePlan_WithInvalidRequirementsAnalysisId_ReturnsOk()
        {
            // Arrange
            var invalidId = Guid.Empty;

            // Act
            var response = await _client.GetAsync($"/api/projectplanning/can-create/{invalidId}");

            // Assert
            // Can return OK (false) or NotFound
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ApprovePlan_WithValidId_ReturnsOk()
        {
            // Arrange
            var planningId = Guid.NewGuid();
            var content = new StringContent("", Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync($"/api/projectplanning/{planningId}/approve", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ApprovePlan_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = Guid.Empty;
            var content = new StringContent("", Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync($"/api/projectplanning/{invalidId}/approve", content);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}