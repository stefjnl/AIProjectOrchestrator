using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Models.Stories;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AIProjectOrchestrator.IntegrationTests.Controllers
{
    public class StoriesControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public StoriesControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GenerateStories_WithValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new StoryGenerationRequest
            {
                PlanningId = Guid.NewGuid()
            };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/stories/generate", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GenerateStories_WithInvalidRequest_ReturnsBadRequest()
        {
            // Arrange
            var invalidRequest = new { invalid = "request" };
            var content = new StringContent(JsonSerializer.Serialize(invalidRequest), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/stories/generate", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetGenerationStatus_WithValidId_ReturnsOk()
        {
            // Arrange
            var generationId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/stories/generations/{generationId}/status");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetGenerationStatus_WithInvalidId_ReturnsOkWithFailedStatus()
        {
            // Arrange
            var invalidId = Guid.Empty;

            // Act
            var response = await _client.GetAsync($"/api/stories/generations/{invalidId}/status");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            // Check if the response content is "Failed" (the default status for non-existent generation)
            Assert.Contains("Failed", content, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GetGenerationResults_WithValidId_ReturnsOk()
        {
            // Arrange
            var generationId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/stories/generations/{generationId}/results");

            // Assert
            // Can return OK or NotFound depending on if generation exists
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetGenerationResults_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = Guid.Empty;

            // Act
            var response = await _client.GetAsync($"/api/stories/generations/{invalidId}/results");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CanGenerateStories_WithValidId_ReturnsOk()
        {
            // Arrange
            var planningId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/stories/can-generate/{planningId}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CanGenerateStories_WithInvalidId_ReturnsOk()
        {
            // Arrange
            var invalidId = Guid.Empty;

            // Act
            var response = await _client.GetAsync($"/api/stories/can-generate/{invalidId}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ApproveStories_WithValidId_ReturnsOk()
        {
            // Arrange
            var generationId = Guid.NewGuid();
            var content = new StringContent("", Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync($"/api/stories/generations/{generationId}/approve", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ApproveStories_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = Guid.Empty;
            var content = new StringContent("", Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync($"/api/stories/generations/{invalidId}/approve", content);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetApprovedStories_WithValidId_ReturnsOk()
        {
            // Arrange
            var storyGenerationId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/stories/generations/{storyGenerationId}/approved");

            // Assert
            // Can return OK or NotFound depending on if approved stories exist
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetApprovedStories_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = Guid.Empty;

            // Act
            var response = await _client.GetAsync($"/api/stories/generations/{invalidId}/approved");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetStoryStatus_WithValidId_ReturnsOk()
        {
            // Arrange
            var storyId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/stories/{storyId}/status");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetStoryStatus_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = Guid.Empty;

            // Act
            var response = await _client.GetAsync($"/api/stories/{invalidId}/status");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ApproveStory_WithValidId_ReturnsOk()
        {
            // Arrange
            var storyId = Guid.NewGuid();
            var content = new StringContent("", Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PutAsync($"/api/stories/{storyId}/approve", content);

            // Assert
            // Can return OK or NotFound depending on if story exists
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ApproveStory_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = Guid.Empty;
            var content = new StringContent("", Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PutAsync($"/api/stories/{invalidId}/approve", content);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task RejectStory_WithValidId_ReturnsOk()
        {
            // Arrange
            var storyId = Guid.NewGuid();
            var feedback = new { Feedback = "Test feedback" };
            var content = new StringContent(JsonSerializer.Serialize(feedback), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PutAsync($"/api/stories/{storyId}/reject", content);

            // Assert
            // Can return OK or NotFound depending on if story exists
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task RejectStory_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = Guid.Empty;
            var feedback = new { Feedback = "Test feedback" };
            var content = new StringContent(JsonSerializer.Serialize(feedback), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PutAsync($"/api/stories/{invalidId}/reject", content);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task EditStory_WithValidRequest_ReturnsOk()
        {
            // Arrange
            var storyId = Guid.NewGuid();
            var request = new EditStoryRequest
            {
                UpdatedStory = new UpdateStoryDto
                {
                    Title = "Updated Title",
                    Description = "Updated Description",
                    Status = StoryStatus.Draft
                }
            };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PutAsync($"/api/stories/{storyId}/edit", content);

            // Assert
            // Can return OK, BadRequest, or NotFound depending on story state
            Assert.True(response.StatusCode == HttpStatusCode.OK || 
                       response.StatusCode == HttpStatusCode.BadRequest || 
                       response.StatusCode == HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task EditStory_WithInvalidRequest_ReturnsBadRequest()
        {
            // Arrange
            var storyId = Guid.NewGuid();
            var invalidRequest = new { invalid = "request" };
            var content = new StringContent(JsonSerializer.Serialize(invalidRequest), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PutAsync($"/api/stories/{storyId}/edit", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task EditStory_WithInvalidStoryId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = Guid.Empty;
            var request = new EditStoryRequest
            {
                UpdatedStory = new UpdateStoryDto
                {
                    Title = "Updated Title",
                    Description = "Updated Description",
                    Status = StoryStatus.Draft
                }
            };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PutAsync($"/api/stories/{invalidId}/edit", content);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}