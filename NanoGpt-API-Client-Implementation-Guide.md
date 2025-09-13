# NanoGpt Client Implementation Guide for .NET

This guide provides a complete, working implementation of a NanoGpt client in .NET based on our successful production deployment.

## Overview

NanoGpt provides an OpenAI-compatible API that allows you to access various AI models, including the `moonshotai/Kimi-K2-Instruct-0905` model. This implementation follows best practices for HTTP client management, error handling, and logging.

## API Specification

### Base URL
```
https://api.nanogpt.com
```

### Endpoint
```
POST /v1/chat/completions
```

### Authentication
```
Authorization: Bearer your-api-key-here
```

### Request Headers
```
Content-Type: application/json
Accept: text/event-stream
```

## Complete Implementation

### 1. Configuration Model

```csharp
public class NanoGptSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.nanogpt.com";
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
    public string DefaultModel { get; set; } = "moonshotai/Kimi-K2-Instruct-0905";
}
```

### 2. Request/Response Models

```csharp
public class AIRequest
{
    public string Prompt { get; set; } = string.Empty;
    public string? SystemMessage { get; set; }
    public string? ModelName { get; set; }
    public float Temperature { get; set; } = 0.7f;
    public int MaxTokens { get; set; } = 1500;
}

public class AIResponse
{
    public string Content { get; set; } = string.Empty;
    public int TokensUsed { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan ResponseTime { get; set; }
}
```

### 3. NanoGpt Client Implementation

```csharp
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public class NanoGptClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NanoGptClient> _logger;
    private readonly NanoGptSettings _settings;

    public NanoGptClient(HttpClient httpClient, ILogger<NanoGptClient> logger, NanoGptSettings settings)
    {
        _httpClient = httpClient;
        _logger = logger;
        _settings = settings;
        
        // Configure HttpClient
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
    }

    public async Task<AIResponse> CallAsync(AIRequest request, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            // Create OpenAI-compatible request format
            var messages = new object[]
            {
                new { role = "system", content = request.SystemMessage ?? "You are a helpful AI assistant." },
                new { role = "user", content = request.Prompt }
            };

            var nanoGptRequest = new
            {
                model = string.IsNullOrEmpty(request.ModelName) ? _settings.DefaultModel : request.ModelName,
                messages = messages,
                temperature = request.Temperature,
                max_tokens = request.MaxTokens,
                stream = false // Use non-streaming mode
            };

            var json = JsonSerializer.Serialize(nanoGptRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Create request message
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions")
            {
                Content = content
            };

            // Add required headers
            requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.ApiKey);
            requestMessage.Headers.Add("Accept", "text/event-stream");

            // Log request details
            _logger.LogInformation("NanoGpt Request: Model={Model}, Temperature={Temperature}, MaxTokens={MaxTokens}", 
                nanoGptRequest.model, nanoGptRequest.temperature, nanoGptRequest.max_tokens);

            // Send request
            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                // Parse non-streaming response
                using var doc = JsonDocument.Parse(responseContent);
                var root = doc.RootElement;

                if (root.TryGetProperty("choices", out var choicesElement) &&
                    choicesElement.GetArrayLength() > 0)
                {
                    var firstChoice = choicesElement[0];
                    if (firstChoice.TryGetProperty("message", out var messageElement) &&
                        messageElement.TryGetProperty("content", out var contentElement))
                    {
                        var finalContent = contentElement.GetString() ?? "";

                        return new AIResponse
                        {
                            Content = finalContent,
                            TokensUsed = 0, // Token usage not available in basic response
                            ProviderName = "NanoGpt",
                            IsSuccess = true,
                            ResponseTime = DateTime.UtcNow - startTime
                        };
                    }
                }

                throw new Exception("Invalid response format: missing choices or message content");
            }
            else
            {
                var errorMessage = response.StatusCode == System.Net.HttpStatusCode.NotFound || 
                                 response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed
                    ? $"NanoGpt API endpoint not found ({response.StatusCode}). This usually means either: 1) The API key is invalid/expired, 2) The endpoint URL is incorrect, or 3) The API service is down."
                    : $"NanoGpt API returned status {response.StatusCode}: {responseContent}";

                return new AIResponse
                {
                    Content = string.Empty,
                    TokensUsed = 0,
                    ProviderName = "NanoGpt",
                    IsSuccess = false,
                    ErrorMessage = errorMessage,
                    ResponseTime = DateTime.UtcNow - startTime
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in NanoGptClient.CallAsync");
            return new AIResponse
            {
                Content = string.Empty,
                TokensUsed = 0,
                ProviderName = "NanoGpt",
                IsSuccess = false,
                ErrorMessage = ex.Message,
                ResponseTime = DateTime.UtcNow - startTime
            };
        }
    }
}
```

### 4. Dependency Injection Setup

```csharp
// In your Program.cs or Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    // Configure NanoGpt settings
    services.Configure<NanoGptSettings>(Configuration.GetSection("AIProviders:NanoGpt"));
    
    // Configure HttpClient for NanoGpt
    services.AddHttpClient<NanoGptClient>((serviceProvider, client) =>
    {
        var settings = serviceProvider.GetRequiredService<IOptions<NanoGptSettings>>().Value;
        client.BaseAddress = new Uri(settings.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
    });
    
    // Register the client
    services.AddScoped<NanoGptClient>();
}
```

### 5. Configuration (appsettings.json)

```json
{
  "AIProviders": {
    "NanoGpt": {
      "ApiKey": "your-api-key-here",
      "BaseUrl": "https://api.nanogpt.com",
      "TimeoutSeconds": 30,
      "MaxRetries": 3,
      "DefaultModel": "moonshotai/Kimi-K2-Instruct-0905"
    }
  }
}
```

## Usage Example

```csharp
public class RequirementsAnalysisService
{
    private readonly NanoGptClient _nanoGptClient;
    private readonly ILogger<RequirementsAnalysisService> _logger;

    public RequirementsAnalysisService(NanoGptClient nanoGptClient, ILogger<RequirementsAnalysisService> logger)
    {
        _nanoGptClient = nanoGptClient;
        _logger = logger;
    }

    public async Task<string> GenerateRequirementsAsync(string projectDescription)
    {
        var request = new AIRequest
        {
            Prompt = $"Analyze the following project and generate comprehensive requirements:\n\n{projectDescription}",
            SystemMessage = "You are an expert business analyst. Generate detailed functional and non-functional requirements.",
            Temperature = 0.7f,
            MaxTokens = 2000
        };

        var response = await _nanoGptClient.CallAsync(request);

        if (response.IsSuccess)
        {
            _logger.LogInformation("Requirements generated successfully. Length: {Length} characters", response.Content.Length);
            return response.Content;
        }
        else
        {
            _logger.LogError("Failed to generate requirements: {Error}", response.ErrorMessage);
            throw new Exception($"Requirements generation failed: {response.ErrorMessage}");
        }
    }
}
```

## Key Implementation Details

### 1. Non-Streaming Mode
- Uses `stream: false` for simpler response handling
- Parses standard JSON response instead of Server-Sent Events
- More reliable for production use

### 2. OpenAI-Compatible Format
- Follows the OpenAI chat completions API structure
- Uses `messages` array with `role` and `content` properties
- Supports system and user messages

### 3. Proper Error Handling
- Specific handling for 404/405 errors (invalid API key or endpoint)
- Comprehensive exception handling with logging
- Graceful failure with detailed error messages

### 4. Logging and Debugging
- Request/response logging for debugging
- Performance timing measurement
- Detailed error information

### 5. HttpClient Best Practices
- Proper HttpClient lifetime management
- Configurable timeouts
- Base address configuration

## Common Issues and Solutions

### Issue: 405 MethodNotAllowed
**Cause**: Wrong base URL or endpoint path
**Solution**: Use `https://api.nanogpt.com` with endpoint `v1/chat/completions`

### Issue: 401 Unauthorized  
**Cause**: Invalid or expired API key
**Solution**: Verify your API key in the NanoGpt dashboard

### Issue: Empty Response
**Cause**: Streaming mode parsing issues
**Solution**: Use `stream: false` and standard JSON parsing

## Testing

```csharp
[TestClass]
public class NanoGptClientTests
{
    [TestMethod]
    public async Task CallAsync_ValidRequest_ReturnsResponse()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("https://api.nanogpt.com/v1/chat/completions")
                .Respond("application/json", @"{
                    ""id"": ""test-id"",
                    ""object"": ""chat.completion"",
                    ""choices"": [{
                        ""message"": {
                            ""role"": ""assistant"",
                            ""content"": ""Test response""
                        }
                    }]
                }");

        var httpClient = new HttpClient(mockHttp);
        var logger = new Mock<ILogger<NanoGptClient>>();
        var settings = new NanoGptSettings { ApiKey = "test-key" };
        
        var client = new NanoGptClient(httpClient, logger.Object, settings);
        var request = new AIRequest { Prompt = "Test prompt" };

        // Act
        var response = await client.CallAsync(request);

        // Assert
        Assert.IsTrue(response.IsSuccess);
        Assert.AreEqual("Test response", response.Content);
    }
}
```

This implementation has been tested in production and successfully generates comprehensive requirements analyses, project plans, and user stories using the `moonshotai/Kimi-K2-Instruct-0905` model.