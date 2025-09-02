using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Models;
using Xunit;

namespace AIProjectOrchestrator.IntegrationTests.ProjectPlanning
{
    [Collection("Sequential")]
    public class ProjectPlanningServiceIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ProjectPlanningServiceIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact(Skip = "Requires working AI services and review approval")]
        public async Task ProjectPlanning_FullWorkflow_IntegrationTest()
        {
            // This test demonstrates the complete workflow but is skipped because:
            // 1. It requires working AI services (Claude API keys, etc.)
            // 2. It requires manual review approval
            // 3. It's a long-running test

            // Step 1: Create requirements analysis
            var requirementsRequest = new RequirementsAnalysisRequest
            {
                ProjectDescription = "Build task management system for small teams",
                AdditionalContext = "React frontend, .NET API backend",
                Constraints = "Must integrate with existing authentication"
            };

            var requirementsResponse = await _client.PostAsJsonAsync("/api/requirements/analyze", requirementsRequest);
            Assert.True(requirementsResponse.IsSuccessStatusCode);

            var requirementsResult = await requirementsResponse.Content.ReadFromJsonAsync<RequirementsAnalysisResponse>();
            Assert.NotNull(requirementsResult);
            Assert.NotEqual(Guid.Empty, requirementsResult.AnalysisId);
            Assert.NotEqual(Guid.Empty, requirementsResult.ReviewId);

            // Step 2: Approve requirements (in a real test, this would require manual intervention or mocking)
            // This step is skipped in automated tests because it requires human review

            // Step 3: Create project plan
            var planningRequest = new ProjectPlanningRequest
            {
                RequirementsAnalysisId = requirementsResult.AnalysisId,
                PlanningPreferences = "Agile methodology, microservices architecture",
                TechnicalConstraints = "Must use .NET and React",
                TimelineConstraints = "6-month delivery timeline"
            };

            var planningResponse = await _client.PostAsJsonAsync("/api/planning/create", planningRequest);
            // In a real environment, this would depend on whether requirements were approved
            // In our test environment without API keys, it will likely return ServiceUnavailable
        }
    }
}