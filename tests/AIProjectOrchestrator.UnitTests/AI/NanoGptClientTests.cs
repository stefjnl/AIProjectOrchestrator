using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Configuration;
using AIProjectOrchestrator.Domain.Models.AI;
using AIProjectOrchestrator.Domain.Services;
using AIProjectOrchestrator.Infrastructure.AI;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace AIProjectOrchestrator.UnitTests.AI
{
    public class NanoGptClientTests
    {
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly HttpClient _httpClient;
        private readonly Mock<ILogger<NanoGptClient>> _loggerMock;
        private readonly Mock<AIProviderConfigurationService> _configurationServiceMock;
        private readonly NanoGptClient _client;

        public NanoGptClientTests()
        {
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _loggerMock = new Mock<ILogger<NanoGptClient>>();
            _configurationServiceMock = new Mock<AIProviderConfigurationService>(MockBehavior.Strict, new Mock<Microsoft.Extensions.Options.IOptions<AIProviderSettings>>().Object);

            // Setup the configuration service to return mock settings
            var mockSettings = new NanoGptSettings
            {
                BaseUrl = "https://nano-gpt.com/api/v1",
                ApiKey = "test-api-key",
                DefaultModel = "test-model",
                MaxRetries = 3,
                TimeoutSeconds = 30
            };
            
            _configurationServiceMock.Setup(x => x.GetProviderSettings("NanoGpt")).Returns(mockSettings);

            _client = new NanoGptClient(_httpClient, _loggerMock.Object, _configurationServiceMock.Object);
        }

        [Fact]
        public async Task CallAsync_ShouldReturnSuccessfulResponse()
        {
            // Arrange
            var request = new AIRequest
            {
                Prompt = "Test prompt",
                ModelName = "test-model"
            };

            var responseContent = "{\"choices\":[{\"text\":\"Test response\"}]}";
            var httpResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(responseContent)
            };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            // Act
            var response = await _client.CallAsync(request);

            // Assert
            Assert.True(response.IsSuccess);
            Assert.Equal("Test response", response.Content);
        }

        [Fact]
        public async Task CallAsync_ShouldReturnFailedResponse_WhenHttpRequestFails()
        {
            // Arrange
            var request = new AIRequest
            {
                Prompt = "Test prompt",
                ModelName = "test-model"
            };

            var httpResponse = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            // Act
            var response = await _client.CallAsync(request);

            // Assert
            Assert.False(response.IsSuccess);
            Assert.Contains("NanoGpt API returned status", response.ErrorMessage);
        }

        [Fact]
        public async Task CallAsync_ShouldUseCorrectEndpointPath()
        {
            // Arrange
            var request = new AIRequest
            {
                Prompt = "Test prompt",
                ModelName = "test-model"
            };

            var responseContent = "{\"choices\":[{\"message\":{\"content\":\"Test response\"}}]}";
            var httpResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(responseContent)
            };

            HttpRequestMessage capturedRequest = null;

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((req, ct) => capturedRequest = req)
                .ReturnsAsync(httpResponse);

            // Act
            var response = await _client.CallAsync(request);

            // Assert
            Assert.True(response.IsSuccess);
            Assert.Equal("Test response", response.Content);
            Assert.NotNull(capturedRequest);
            Assert.EndsWith("/v1/chat/completions", capturedRequest.RequestUri?.ToString());
            Assert.DoesNotContain("/api/api", capturedRequest.RequestUri?.ToString()); // Ensure no double /api in URL
        }

        [Fact]
        public async Task CallAsync_ShouldSetCorrectHeadersAndRequestFormat()
        {
            // Arrange
            var request = new AIRequest
            {
                Prompt = "Test prompt",
                ModelName = "test-model",
                SystemMessage = "You are a helpful assistant",
                Temperature = 0.7,
                MaxTokens = 100
            };

            var responseContent = "{\"choices\":[{\"message\":{\"content\":\"Test response\"}}],\"usage\":{\"completion_tokens\":50}}";
            var httpResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(responseContent)
            };

            HttpRequestMessage capturedRequest = null;

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((req, ct) => capturedRequest = req)
                .ReturnsAsync(httpResponse);

            // Act
            var response = await _client.CallAsync(request);

            // Assert
            Assert.True(response.IsSuccess);
            Assert.Equal("Test response", response.Content);
            Assert.Equal(50, response.TokensUsed);
            Assert.Equal("NanoGpt", response.ProviderName);

            // Verify endpoint path
            Assert.NotNull(capturedRequest);
            Assert.EndsWith("/v1/chat/completions", capturedRequest.RequestUri?.ToString());

            // Verify request body format matches OpenAI API
            Assert.NotNull(capturedRequest.Content);
            var capturedRequestBody = await capturedRequest.Content.ReadAsStringAsync();
            Assert.Contains("\"model\":\"test-model\"", capturedRequestBody);
            Assert.Contains("\"messages\"", capturedRequestBody);
            Assert.Contains("\"role\":\"system\"", capturedRequestBody);
            Assert.Contains("\"role\":\"user\"", capturedRequestBody);
            Assert.Contains("\"temperature\":0.7", capturedRequestBody);
            Assert.Contains("\"max_tokens\":100", capturedRequestBody);
            Assert.Contains("\"stream\":false", capturedRequestBody);

            // Verify headers
            Assert.True(capturedRequest.Headers.Contains("Authorization"));
            var authHeader = capturedRequest.Headers.GetValues("Authorization").FirstOrDefault();
            Assert.NotNull(authHeader);
            Assert.StartsWith("Bearer test-api-key", authHeader);
            Assert.True(capturedRequest.Headers.Contains("Accept"));
            var acceptHeader = capturedRequest.Headers.GetValues("Accept").FirstOrDefault();
            Assert.NotNull(acceptHeader);
            Assert.Contains("text/event-stream", acceptHeader);
        }

        [Fact]
        public async Task CallAsync_ShouldHandleJsonExceptionGracefully()
        {
            // Arrange
            var request = new AIRequest
            {
                Prompt = "Test prompt",
                ModelName = "test-model"
            };

            var invalidJsonResponse = "invalid json response";
            var httpResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(invalidJsonResponse)
            };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            // Act
            var response = await _client.CallAsync(request);

            // Assert
            Assert.False(response.IsSuccess);
            Assert.Contains("Failed to parse JSON response", response.ErrorMessage);
            Assert.Contains("invalid json response", response.ErrorMessage);
        }

        [Fact]
        public async Task IsHealthyAsync_ShouldReturnTrue_WhenHttpRequestSucceeds()
        {
            // Arrange
            var httpResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK);

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri.ToString() == "/"),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            // Act
            var result = await _client.IsHealthyAsync();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsHealthyAsync_ShouldReturnFalse_WhenHttpRequestFails()
        {
            // Arrange
            var httpResponse = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri.ToString() == "/"),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            // Act
            var result = await _client.IsHealthyAsync();

            // Assert
            Assert.False(result);
        }
    }
}