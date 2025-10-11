using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AIProjectOrchestrator.IntegrationTests.Controllers
{
    public class TestExceptionControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public TestExceptionControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task ThrowException_WithValidationType_ReturnsValidationError()
        {
            // Act
            var response = await _client.GetAsync("/api/test-exception/validation");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ThrowException_WithAiProviderType_ReturnsError()
        {
            // Act
            var response = await _client.GetAsync("/api/test-exception/ai-provider");

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.InternalServerError || 
                       response.StatusCode == HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task ThrowException_WithArgumentType_ReturnsError()
        {
            // Act
            var response = await _client.GetAsync("/api/test-exception/argument");

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest || 
                       response.StatusCode == HttpStatusCode.InternalServerError);
        }

        [Fact]
        public async Task ThrowException_WithKeyNotFoundType_ReturnsError()
        {
            // Act
            var response = await _client.GetAsync("/api/test-exception/key-not-found");

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.NotFound || 
                       response.StatusCode == HttpStatusCode.InternalServerError);
        }

        [Fact]
        public async Task ThrowException_WithUnauthorizedType_ReturnsError()
        {
            // Act
            var response = await _client.GetAsync("/api/test-exception/unauthorized");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ThrowException_WithInvalidType_ReturnsError()
        {
            // Act
            var response = await _client.GetAsync("/api/test-exception/invalid-type");

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }
    }
}