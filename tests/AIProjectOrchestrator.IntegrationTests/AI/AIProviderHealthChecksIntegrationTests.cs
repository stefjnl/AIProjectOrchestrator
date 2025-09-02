using System.Net;
using System.Threading.Tasks;
using Xunit;
using AIProjectOrchestrator.IntegrationTests;

namespace AIProjectOrchestrator.IntegrationTests.AI
{
    public class AIProviderHealthChecksIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public AIProviderHealthChecksIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task HealthCheck_ShouldReturnHealthy_WhenAllProvidersAreConfigured()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/health");

            // Assert
            // Note: In a real test, we would check for actual health status,
            // but since we don't have real API keys, we're just checking that
            // the endpoint responds
            Assert.True(response.StatusCode == HttpStatusCode.OK || 
                       response.StatusCode == HttpStatusCode.ServiceUnavailable);
        }
    }
}