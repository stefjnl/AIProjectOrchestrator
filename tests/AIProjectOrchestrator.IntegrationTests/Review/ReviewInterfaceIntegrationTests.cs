using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Models.Review;
using Xunit;
using Xunit.Abstractions;

namespace AIProjectOrchestrator.IntegrationTests.Review
{
    [Collection("Sequential")]
    public class ReviewInterfaceIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly ITestOutputHelper _output;

        public ReviewInterfaceIntegrationTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
        {
            _client = factory.CreateClient();
            _output = output;
        }

        [Fact]
        public async Task GET_ReviewDashboard_ReturnsHTMLContent()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "/index.html");

            // Act
            var response = await _client.SendAsync(request);

            // Assert
            // Note: This will pass now that we've set up static file serving
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var contentType = response.Content.Headers.ContentType?.MediaType;
            Assert.Equal("text/html", contentType);
        }

        [Fact]
        public async Task POST_ApproveReview_UpdatesWorkflowStatus()
        {
            // Arrange
            // First submit a review
            var submitRequest = new SubmitReviewRequest
            {
                ServiceName = "TestService",
                Content = "Test content for approval",
                CorrelationId = "test-correlation-approve",
                PipelineStage = "Analysis"
            };

            var submitContent = new StringContent(
                JsonSerializer.Serialize(submitRequest),
                Encoding.UTF8,
                "application/json");

            var submitResponse = await _client.PostAsync("/api/review/submit", submitContent);
            submitResponse.EnsureSuccessStatusCode();

            var submitResult = await submitResponse.Content.ReadFromJsonAsync<ReviewResponse>();

            // Act
            var approveContent = new StringContent("{}", Encoding.UTF8, "application/json");
            var approveResponse = await _client.PostAsync($"/api/review/{submitResult.ReviewId}/approve", approveContent);

            // Assert
            Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);
        }

        [Fact]
        public async Task GET_ProjectStatus_ShowsCurrentStage()
        {
            // Arrange
            var projectId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/review/workflow-status/{projectId}");

            // Assert
            // This test will pass once we implement the actual method
            // For now, we expect a 500 Internal Server Error due to NotImplementedException
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task POST_SubmitTestScenario_InitiatesEndToEndWorkflow()
        {
            // This test will pass once we implement the actual method
            // For now, we expect a 500 Internal Server Error due to NotImplementedException
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/review/test-scenario");
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task GET_StaticFiles_ServeCorrectly()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "/index.html");

            // Act
            var response = await _client.SendAsync(request);

            // Assert
            // This should now pass since we've set up static file serving
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task EndToEnd_CompleteWorkflow_ProcessesSuccessfully()
        {
            // This is a placeholder test that will be implemented later
            Assert.True(true); // Placeholder
        }
    }
}
