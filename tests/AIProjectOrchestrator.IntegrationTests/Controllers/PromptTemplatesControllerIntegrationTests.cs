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
    public class PromptTemplatesControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public PromptTemplatesControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetAll_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/prompttemplates");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetById_WithValidId_ReturnsOk()
        {
            // Arrange
            // First create a template to get
            var templateData = new PromptTemplate
            {
                Title = "Test Template",
                Content = "Test content"
            };
            var content = new StringContent(JsonSerializer.Serialize(templateData), Encoding.UTF8, "application/json");
            var createResponse = await _client.PostAsync("/api/prompttemplates", content);
            createResponse.EnsureSuccessStatusCode();
            
            var responseContent = await createResponse.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseContent);
            var templateId = doc.RootElement.GetProperty("id").GetGuid();

            // Act
            var response = await _client.GetAsync($"/api/prompttemplates/{templateId}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetById_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = Guid.Empty;

            // Act
            var response = await _client.GetAsync($"/api/prompttemplates/{invalidId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CreateOrUpdate_WithValidData_ReturnsCreated()
        {
            // Arrange
            var templateData = new PromptTemplate
            {
                Title = "Test Template",
                Content = "Test content"
            };
            var content = new StringContent(JsonSerializer.Serialize(templateData), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/prompttemplates", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateOrUpdate_WithValidDataForUpdate_ReturnsOk()
        {
            // Arrange
            // First create a template
            var templateData = new PromptTemplate
            {
                Title = "Test Template",
                Content = "Test content"
            };
            var content = new StringContent(JsonSerializer.Serialize(templateData), Encoding.UTF8, "application/json");
            var createResponse = await _client.PostAsync("/api/prompttemplates", content);
            createResponse.EnsureSuccessStatusCode();
            
            var responseContent = await createResponse.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseContent);
            var templateId = doc.RootElement.GetProperty("id").GetGuid();
            
            // Now update the template
            var updatedTemplateData = new PromptTemplate
            {
                Id = templateId,
                Title = "Updated Template",
                Content = "Updated content"
            };
            var updateContent = new StringContent(JsonSerializer.Serialize(updatedTemplateData), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/prompttemplates", updateContent);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CreateOrUpdate_WithNullData_ReturnsBadRequest()
        {
            // Arrange
            var content = new StringContent("null", Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/prompttemplates", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Delete_WithValidId_ReturnsNoContent()
        {
            // Arrange
            // First create a template to delete
            var templateData = new PromptTemplate
            {
                Title = "Test Template to Delete",
                Content = "Test content"
            };
            var content = new StringContent(JsonSerializer.Serialize(templateData), Encoding.UTF8, "application/json");
            var createResponse = await _client.PostAsync("/api/prompttemplates", content);
            createResponse.EnsureSuccessStatusCode();
            
            var responseContent = await createResponse.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseContent);
            var templateId = doc.RootElement.GetProperty("id").GetGuid();

            // Act
            var response = await _client.DeleteAsync($"/api/prompttemplates/{templateId}");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task Delete_WithInvalidId_ReturnsNoContent()
        {
            // Arrange
            var invalidId = Guid.Empty;

            // Act
            var response = await _client.DeleteAsync($"/api/prompttemplates/{invalidId}");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }
}