using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AIProjectOrchestrator.IntegrationTests.Controllers
{
    public class ProjectsControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ProjectsControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetProjects_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/projects");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetProject_WithValidId_ReturnsOk()
        {
            // Arrange
            // First create a project to get
            var projectData = new { Name = "Test Project", Description = "Test Description" };
            var content = new StringContent(JsonSerializer.Serialize(projectData), Encoding.UTF8, "application/json");
            var createResponse = await _client.PostAsync("/api/projects", content);
            createResponse.EnsureSuccessStatusCode();
            
            var responseContent = await createResponse.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseContent);
            var projectId = doc.RootElement.GetProperty("id").GetInt32();

            // Act
            var response = await _client.GetAsync($"/api/projects/{projectId}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetProject_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = 999999; // Non-existent ID

            // Act
            var response = await _client.GetAsync($"/api/projects/{invalidId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CreateProject_WithValidData_ReturnsCreated()
        {
            // Arrange
            var projectData = new { Name = "Test Project", Description = "Test Description" };
            var content = new StringContent(JsonSerializer.Serialize(projectData), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/projects", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateProject_WithInvalidData_ReturnsBadRequest()
        {
            // Arrange
            var invalidData = new { invalid = "data" };
            var content = new StringContent(JsonSerializer.Serialize(invalidData), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/projects", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task DeleteProject_WithValidId_ReturnsNoContent()
        {
            // Arrange
            // First create a project to delete
            var projectData = new { Name = "Test Project to Delete", Description = "Test Description" };
            var content = new StringContent(JsonSerializer.Serialize(projectData), Encoding.UTF8, "application/json");
            var createResponse = await _client.PostAsync("/api/projects", content);
            createResponse.EnsureSuccessStatusCode();
            
            var responseContent = await createResponse.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseContent);
            var projectId = doc.RootElement.GetProperty("id").GetInt32();

            // Act
            var response = await _client.DeleteAsync($"/api/projects/{projectId}");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task DeleteProject_WithInvalidId_ReturnsOk()
        {
            // Arrange
            var invalidId = 999999; // Non-existent ID

            // Act
            var response = await _client.DeleteAsync($"/api/projects/{invalidId}");

            // Assert
            // Deletion of non-existent resource typically returns OK/NoContent
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent);
        }
    }
}