using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AIProjectOrchestrator.IntegrationTests.Controllers
{
    public class ProviderManagementControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ProviderManagementControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetAvailableProviders_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/providermanagement/providers");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetProviderHealth_WithValidProvider_ReturnsOk()
        {
            // Arrange
            var providerName = "claude"; // Using a common provider name

            // Act
            var response = await _client.GetAsync($"/api/providermanagement/health/{providerName}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetProviderHealth_WithInvalidProvider_ReturnsNotFound()
        {
            // Arrange
            var invalidProvider = "invalidprovider";

            // Act
            var response = await _client.GetAsync($"/api/providermanagement/health/{invalidProvider}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetCurrentProvider_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/providermanagement/current");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task SwitchProvider_WithValidProvider_ReturnsOk()
        {
            // Arrange
            var switchRequest = new { Provider = "claude" };
            var content = new StringContent(JsonSerializer.Serialize(switchRequest), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/providermanagement/switch", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task SwitchProvider_WithInvalidProvider_ReturnsBadRequest()
        {
            // Arrange
            var switchRequest = new { Provider = "nonexistent" };
            var content = new StringContent(JsonSerializer.Serialize(switchRequest), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/providermanagement/switch", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task SwitchProvider_WithEmptyProvider_ReturnsBadRequest()
        {
            // Arrange
            var switchRequest = new { Provider = "" };
            var content = new StringContent(JsonSerializer.Serialize(switchRequest), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/providermanagement/switch", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}