using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using AIProjectOrchestrator.Domain.Models.PromptGeneration;
using Microsoft.Extensions.DependencyInjection;
using AIProjectOrchestrator.Domain.Interfaces;
using Moq;
using AIProjectOrchestrator.Infrastructure.AI;
using AIProjectOrchestrator.Domain.Services;
using AIProjectOrchestrator.Domain.Models.AI;
using System.Net;

namespace AIProjectOrchestrator.IntegrationTests
{
    public class CompleteWorkflowIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public CompleteWorkflowIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task CompleteWorkflow_ProjectToPrompts_Success()
        {
            // Arrange
            var testFactory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Mock AI client factory to return fixed responses for fast testing
                    var mockAIClient = new Mock<IAIClient>();
                    mockAIClient.Setup(c => c.CallAsync(It.IsAny<AIRequest>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new AIResponse { Content = "### Context\nTest context\n### Instructions\nTest instructions" });
                    var mockAIClientFactory = new Mock<IAIClientFactory>();
                    mockAIClientFactory.Setup(f => f.GetClient(It.IsAny<string>())).Returns(mockAIClient.Object);
                    services.AddSingleton(mockAIClient.Object);
                    services.AddSingleton(mockAIClientFactory.Object);
                });
            });

            var client = testFactory.CreateClient();

            // 1. Create project
            var projectData = new { Name = "Test Project", Description = "A test project for workflow validation" };
            var projectContent = new StringContent(JsonSerializer.Serialize(projectData), Encoding.UTF8, "application/json");
            var projectResponse = await client.PostAsync("/api/projects", projectContent);
            projectResponse.EnsureSuccessStatusCode();
            var projectJson = await projectResponse.Content.ReadAsStringAsync();
            var project = JsonSerializer.Deserialize<dynamic>(projectJson);
            var projectId = (Guid)project.Id;

            // 2. Submit requirements analysis  
            var reqData = new { ProjectId = projectId, Description = "Analyze requirements for this project" };
            var reqContent = new StringContent(JsonSerializer.Serialize(reqData), Encoding.UTF8, "application/json");
            var reqResponse = await client.PostAsync("/api/requirements/analyze", reqContent);
            reqResponse.EnsureSuccessStatusCode();
            var reqJson = await reqResponse.Content.ReadAsStringAsync();
            var req = JsonSerializer.Deserialize<dynamic>(reqJson);
            var analysisId = (Guid)req.Id; // Assume response has Id for analysis

            // 3. Approve requirements (assume review is created, get pending and approve first)
            await Task.Delay(100); // Small delay for background processing if needed
            var pendingReviewsResponse = await client.GetAsync("/api/review/pending");
            pendingReviewsResponse.EnsureSuccessStatusCode();
            var pendingReviewsJson = await pendingReviewsResponse.Content.ReadAsStringAsync();
            dynamic pendingReviews = JsonSerializer.Deserialize<dynamic>(pendingReviewsJson);
            var reviewId = (Guid)pendingReviews[0].Id; // Assume first is the requirements review
            var approveContent = new StringContent("{}", Encoding.UTF8, "application/json");
            var approveReqResponse = await client.PostAsync($"/api/review/{reviewId}/approve", approveContent);
            approveReqResponse.EnsureSuccessStatusCode();

            // 4. Submit project planning
            var planData = new { RequirementsAnalysisId = analysisId };
            var planContent = new StringContent(JsonSerializer.Serialize(planData), Encoding.UTF8, "application/json");
            var planResponse = await client.PostAsync("/api/projectplanning/create", planContent);
            planResponse.EnsureSuccessStatusCode();
            var planJson = await planResponse.Content.ReadAsStringAsync();
            var plan = JsonSerializer.Deserialize<dynamic>(planJson);
            var planningId = (Guid)plan.Id; // Assume Id

            // 5. Approve planning
            await Task.Delay(100);
            var pendingPlanResponse = await client.GetAsync("/api/review/pending");
            pendingPlanResponse.EnsureSuccessStatusCode();
            var pendingPlanJson = await pendingPlanResponse.Content.ReadAsStringAsync();
            dynamic pendingPlans = JsonSerializer.Deserialize<dynamic>(pendingPlanJson);
            var planReviewId = (Guid)pendingPlans[0].Id;
            var approvePlanResponse = await client.PostAsync($"/api/review/{planReviewId}/approve", approveContent);
            approvePlanResponse.EnsureSuccessStatusCode();

            // 6. Submit story generation
            var storyData = new { ProjectPlanningId = planningId };
            var storyContent = new StringContent(JsonSerializer.Serialize(storyData), Encoding.UTF8, "application/json");
            var storyResponse = await client.PostAsync("/api/stories/generate", storyContent);
            storyResponse.EnsureSuccessStatusCode();
            var storyJson = await storyResponse.Content.ReadAsStringAsync();
            var story = JsonSerializer.Deserialize<dynamic>(storyJson);
            var storyGenerationId = (Guid)story.Id; // Assume GenerationId

            // 7. Approve stories
            await Task.Delay(100);
            var pendingStoryResponse = await client.GetAsync("/api/review/pending");
            pendingStoryResponse.EnsureSuccessStatusCode();
            var pendingStoryJson = await pendingStoryResponse.Content.ReadAsStringAsync();
            dynamic pendingStories = JsonSerializer.Deserialize<dynamic>(pendingStoryJson);
            var storyReviewId = (Guid)pendingStories[0].Id;
            var approveStoryResponse = await client.PostAsync($"/api/review/{storyReviewId}/approve", approveContent);
            approveStoryResponse.EnsureSuccessStatusCode();

            // 8. Generate prompt for first story
            var promptData = new PromptGenerationRequest { StoryGenerationId = storyGenerationId, StoryIndex = 0 };
            var promptContent = new StringContent(JsonSerializer.Serialize(promptData), Encoding.UTF8, "application/json");
            var promptResponse = await client.PostAsync("/api/prompts/generate", promptContent);
            Assert.Equal(HttpStatusCode.OK, promptResponse.StatusCode); // Success

            var promptJson = await promptResponse.Content.ReadAsStringAsync();
            var prompt = JsonSerializer.Deserialize<PromptGenerationResponse>(promptJson);

            // 9. Verify prompt contains expected sections
            Assert.NotNull(prompt);
            Assert.NotNull(prompt.GeneratedPrompt);
            Assert.Contains("Context", prompt.GeneratedPrompt);
            Assert.Contains("Instructions", prompt.GeneratedPrompt);
            // Additional assertions based on expected structure
        }
    }
}
