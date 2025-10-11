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
            using var projectDoc = JsonDocument.Parse(projectJson);
            Guid projectId;
            if (projectDoc.RootElement.TryGetProperty("Id", out var projIdProp))
            {
                projectId = projIdProp.GetGuid();
            }
            else if (projectDoc.RootElement.TryGetProperty("id", out projIdProp))
            {
                projectId = projIdProp.GetGuid();
            }
            else
            {
                throw new InvalidOperationException("Project response missing Id");
            }

            // 2. Submit requirements analysis  
            var reqData = new { ProjectId = projectId, Description = "Analyze requirements for this project" };
            var reqContent = new StringContent(JsonSerializer.Serialize(reqData), Encoding.UTF8, "application/json");
            var reqResponse = await client.PostAsync("/api/requirements/analyze", reqContent);
            reqResponse.EnsureSuccessStatusCode();
            var reqJson = await reqResponse.Content.ReadAsStringAsync();
            using var reqDoc = JsonDocument.Parse(reqJson);
            Guid analysisId;
            if (reqDoc.RootElement.TryGetProperty("Id", out var reqIdProp))
            {
                analysisId = reqIdProp.GetGuid();
            }
            else if (reqDoc.RootElement.TryGetProperty("id", out reqIdProp))
            {
                analysisId = reqIdProp.GetGuid();
            }
            else
            {
                throw new InvalidOperationException("Requirements response missing Id");
            }

            // 3. Approve requirements (assume review is created, get pending and approve first)
            await Task.Delay(100); // Small delay for background processing if needed
            var pendingReviewsResponse = await client.GetAsync("/api/review/pending");
            pendingReviewsResponse.EnsureSuccessStatusCode();
            var pendingReviewsJson = await pendingReviewsResponse.Content.ReadAsStringAsync();
            using var pendingReviewsDoc = JsonDocument.Parse(pendingReviewsJson);
            var reviewsEnumerator = pendingReviewsDoc.RootElement.EnumerateArray();
            if (!reviewsEnumerator.MoveNext())
            {
                throw new InvalidOperationException("No pending reviews found");
            }
            var firstReview = reviewsEnumerator.Current;
            Guid reviewId;
            if (firstReview.TryGetProperty("Id", out var reviewIdProp))
            {
                reviewId = reviewIdProp.GetGuid();
            }
            else if (firstReview.TryGetProperty("id", out reviewIdProp))
            {
                reviewId = reviewIdProp.GetGuid();
            }
            else
            {
                throw new InvalidOperationException("Pending review missing Id");
            }
            var approveContent = new StringContent("{}", Encoding.UTF8, "application/json");
            var approveReqResponse = await client.PostAsync($"/api/review/{reviewId}/approve", approveContent);
            approveReqResponse.EnsureSuccessStatusCode();

            // Wait for review to be processed
            await Task.Delay(500);

            // 4. Submit project planning
            var planData = new { RequirementsAnalysisId = analysisId };
            var planContent = new StringContent(JsonSerializer.Serialize(planData), Encoding.UTF8, "application/json");
            var planResponse = await client.PostAsync("/api/projectplanning/create", planContent);
            planResponse.EnsureSuccessStatusCode();
            var planJson = await planResponse.Content.ReadAsStringAsync();
            using var planDoc = JsonDocument.Parse(planJson);
            Guid planningId;
            if (planDoc.RootElement.TryGetProperty("Id", out var planIdProp))
            {
                planningId = planIdProp.GetGuid();
            }
            else if (planDoc.RootElement.TryGetProperty("id", out planIdProp))
            {
                planningId = planIdProp.GetGuid();
            }
            else
            {
                throw new InvalidOperationException("Planning response missing Id");
            }

            // 5. Approve planning
            await Task.Delay(100);
            var pendingPlanResponse = await client.GetAsync("/api/review/pending");
            pendingPlanResponse.EnsureSuccessStatusCode();
            var pendingPlanJson = await pendingPlanResponse.Content.ReadAsStringAsync();
            using var pendingPlansDoc = JsonDocument.Parse(pendingPlanJson);
            var plansEnumerator = pendingPlansDoc.RootElement.EnumerateArray();
            if (!plansEnumerator.MoveNext())
            {
                throw new InvalidOperationException("No pending plans found");
            }
            var firstPlanReview = plansEnumerator.Current;
            Guid planReviewId;
            if (firstPlanReview.TryGetProperty("Id", out var planReviewIdProp))
            {
                planReviewId = planReviewIdProp.GetGuid();
            }
            else if (firstPlanReview.TryGetProperty("id", out planReviewIdProp))
            {
                planReviewId = planReviewIdProp.GetGuid();
            }
            else
            {
                throw new InvalidOperationException("Pending plan review missing Id");
            }
            var approvePlanResponse = await client.PostAsync($"/api/review/{planReviewId}/approve", approveContent);
            approvePlanResponse.EnsureSuccessStatusCode();

            // Wait for review to be processed
            await Task.Delay(500);

            // 6. Submit story generation
            var storyData = new { ProjectPlanningId = planningId };
            var storyContent = new StringContent(JsonSerializer.Serialize(storyData), Encoding.UTF8, "application/json");
            var storyResponse = await client.PostAsync("/api/stories/generate", storyContent);
            storyResponse.EnsureSuccessStatusCode();
            var storyJson = await storyResponse.Content.ReadAsStringAsync();
            using var storyDoc = JsonDocument.Parse(storyJson);
            Guid storyGenerationId;
            if (storyDoc.RootElement.TryGetProperty("Id", out var storyIdProp))
            {
                storyGenerationId = storyIdProp.GetGuid();
            }
            else if (storyDoc.RootElement.TryGetProperty("id", out storyIdProp))
            {
                storyGenerationId = storyIdProp.GetGuid();
            }
            else
            {
                throw new InvalidOperationException("Story generation response missing Id");
            }

            // 7. Approve stories
            await Task.Delay(100);
            var pendingStoryResponse = await client.GetAsync("/api/review/pending");
            pendingStoryResponse.EnsureSuccessStatusCode();
            var pendingStoryJson = await pendingStoryResponse.Content.ReadAsStringAsync();
            using var pendingStoriesDoc = JsonDocument.Parse(pendingStoryJson);
            var storiesEnumerator = pendingStoriesDoc.RootElement.EnumerateArray();
            if (!storiesEnumerator.MoveNext())
            {
                throw new InvalidOperationException("No pending stories found");
            }
            var firstStoryReview = storiesEnumerator.Current;
            Guid storyReviewId;
            if (firstStoryReview.TryGetProperty("Id", out var storyReviewIdProp))
            {
                storyReviewId = storyReviewIdProp.GetGuid();
            }
            else if (firstStoryReview.TryGetProperty("id", out storyReviewIdProp))
            {
                storyReviewId = storyReviewIdProp.GetGuid();
            }
            else
            {
                throw new InvalidOperationException("Pending story review missing Id");
            }
            var approveStoryResponse = await client.PostAsync($"/api/review/{storyReviewId}/approve", approveContent);
            approveStoryResponse.EnsureSuccessStatusCode();

            // Wait for review to be processed
            await Task.Delay(500);

            // 8. Generate prompt for first story
            var promptData = new PromptGenerationRequest { StoryGenerationId = storyGenerationId, StoryIndex = 0 };
            var promptContent = new StringContent(JsonSerializer.Serialize(promptData), Encoding.UTF8, "application/json");
            var promptResponse = await client.PostAsync("/api/prompts/generate", promptContent);
            Assert.Equal(HttpStatusCode.OK, promptResponse.StatusCode); // Success

            // Wait for prompt generation to complete
            await Task.Delay(500);

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
