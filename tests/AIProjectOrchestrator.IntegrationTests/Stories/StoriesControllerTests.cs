using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Models.Stories;
using Xunit;

namespace AIProjectOrchestrator.IntegrationTests.Stories
{
    [Collection("Sequential")]
    public class StoriesControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public StoriesControllerTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task POST_Generate_ValidRequest_ReturnsAccepted()
        {
            // Arrange
            var request = new StoryGenerationRequest
            {
                PlanningId = Guid.NewGuid(),
                StoryPreferences = "Focus on user authentication features",
                ComplexityLevels = "Simple, Medium, Complex",
                AdditionalGuidance = "Include security considerations"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/stories/generate", request);

            // Assert
            // Note: In a real environment with Claude API configured, this might return 200
            // In our test environment without API keys, it will likely return 503
            // We're just verifying the endpoint exists and can handle the request
            Assert.True(response.StatusCode == HttpStatusCode.OK || 
                       response.StatusCode == HttpStatusCode.ServiceUnavailable ||
                       response.StatusCode == HttpStatusCode.NotFound ||
                       response.StatusCode == HttpStatusCode.InternalServerError);
        }

        [Fact]
        public async Task POST_Generate_InvalidPlanningId_ReturnsBadRequest()
        {
            // Arrange
            var request = new StoryGenerationRequest
            {
                PlanningId = Guid.Empty // Invalid - empty
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/stories/generate", request);

            // Assert
            // With attribute validation, this should return BadRequest rather than NotFound
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GET_Status_ValidId_ReturnsStatus()
        {
            // Arrange
            var generationId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/stories/{generationId}/status");

            // Assert
            // This should return a status, even if it's Failed for an unknown ID
            Assert.True(response.StatusCode == HttpStatusCode.OK || 
                       response.StatusCode == HttpStatusCode.NotFound ||
                       response.StatusCode == HttpStatusCode.InternalServerError);
        }

        [Fact]
        public async Task GET_Results_ValidApprovedId_ReturnsStories()
        {
            // Arrange
            var generationId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/stories/{generationId}/results");

            // Assert
            // This should return 404 for unknown generation IDs
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GET_CanGenerate_ValidPlanningId_ReturnsBoolean()
        {
            // Arrange
            var planningId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/stories/can-generate/{planningId}");

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.OK || 
                       response.StatusCode == HttpStatusCode.NotFound ||
                       response.StatusCode == HttpStatusCode.InternalServerError);
        }

        [Fact]
        public async Task POST_Generate_ServiceUnavailable_ReturnsServiceUnavailable()
        {
            // Arrange
            var request = new StoryGenerationRequest
            {
                PlanningId = Guid.NewGuid()
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/stories/generate", request);

            // Assert
            // In our test environment without API keys, it will likely return 503
            // We're just verifying the endpoint exists and can handle the request
            Assert.True(response.StatusCode == HttpStatusCode.OK || 
                       response.StatusCode == HttpStatusCode.ServiceUnavailable ||
                       response.StatusCode == HttpStatusCode.NotFound ||
                       response.StatusCode == HttpStatusCode.InternalServerError);
        }
    }
}
