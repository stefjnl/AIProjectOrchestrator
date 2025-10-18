using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Configuration;
using AIProjectOrchestrator.Domain.Models.AI;
using AIProjectOrchestrator.Domain.Services;
using Microsoft.Extensions.Logging;

namespace AIProjectOrchestrator.Infrastructure.AI
{
    public class NanoGptClient : BaseAIClientHandler, IAIClient
    {
        private readonly AIProviderConfigurationService _configurationService;
        private readonly NanoGptCredentials _settings;

        public override string ProviderName => "NanoGpt";

        public NanoGptClient(HttpClient httpClient, ILogger<NanoGptClient> logger, AIProviderConfigurationService configurationService)
            : base(httpClient, logger)
        {
            _configurationService = configurationService;
            _settings = _configurationService.GetProviderSettings<NanoGptCredentials>(ProviderName);

            // Log settings for debugging
            _logger.LogInformation("{ProviderName} Settings - BaseUrl: {BaseUrl}, ApiKey Length: {ApiKeyLength}, DefaultModel: {DefaultModel}",
                ProviderName, _settings.BaseUrl, _settings.ApiKey?.Length ?? 0, _settings.DefaultModel);

            // Log the actual API key prefix for debugging (first 10 characters)
            if (!string.IsNullOrEmpty(_settings.ApiKey))
            {
                _logger.LogInformation("{ProviderName} API Key prefix: {ApiKeyPrefix}", ProviderName, _settings.ApiKey.Substring(0, Math.Min(10, _settings.ApiKey.Length)));
            }

            // Also log the HttpClient BaseAddress in constructor
            _logger.LogInformation("{ProviderName} HttpClient BaseAddress: {BaseAddress}", ProviderName, httpClient.BaseAddress?.ToString() ?? "NULL");
        }

        public override async Task<AIResponse> CallAsync(AIRequest request, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // Log the request details for debugging
                _logger.LogInformation("NanoGptClient.CallAsync starting with prompt length: {PromptLength}, model: {Model}",
                    request.Prompt?.Length ?? 0, request.ModelName ?? _settings.DefaultModel);

                // Create OpenAI-compatible request format for NanoGpt API
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
                    stream = false // Use non-streaming mode as in the working example
                };

                var json = JsonSerializer.Serialize(nanoGptRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Log the complete request details for debugging
                _logger.LogInformation("=== NANO_GPT REQUEST DEBUG ===");
                _logger.LogInformation("Request URL: {BaseUrl}/chat/completions", _settings.BaseUrl);
                _logger.LogInformation("Request Method: POST");
                _logger.LogInformation("Request Headers:");
                _logger.LogInformation("  Authorization: Bearer {ApiKeyPrefix}...", _settings.ApiKey?.Substring(0, Math.Min(10, _settings.ApiKey.Length)) ?? "NULL");
                _logger.LogInformation("  Content-Type: application/json");
                _logger.LogInformation("  Accept: text/event-stream");
                _logger.LogInformation("Request Body:");
                _logger.LogInformation("  model: {Model}", nanoGptRequest.model);
                _logger.LogInformation("  temperature: {Temperature}", nanoGptRequest.temperature);
                _logger.LogInformation("  max_tokens: {MaxTokens}", nanoGptRequest.max_tokens);
                _logger.LogInformation("  stream: {Stream} (NON-STREAMING)", nanoGptRequest.stream);
                _logger.LogInformation("  messages count: {MessageCount}", messages.Length);
                for (int i = 0; i < messages.Length; i++)
                {
                    var msg = messages[i];
                    _logger.LogInformation("  message[{Index}] role: {Role}", i, msg.GetType().GetProperty("role")?.GetValue(msg)?.ToString() ?? "unknown");
                    var msgContent = msg.GetType().GetProperty("content")?.GetValue(msg)?.ToString() ?? "";
                    _logger.LogInformation("  message[{Index}] content length: {ContentLength} chars", i, msgContent.Length);
                    if (msgContent.Length > 100)
                    {
                        _logger.LogInformation("  message[{Index}] content preview: {Preview}...", i, msgContent.Substring(0, 100));
                    }
                    else
                    {
                        _logger.LogInformation("  message[{Index}] content: {Content}", i, msgContent);
                    }
                }
                _logger.LogInformation("Request JSON length: {JsonLength} characters", json.Length);
                _logger.LogInformation("Full Request JSON: {Json}", json);
                _logger.LogInformation("Request JSON Length: {JsonLength} characters", json.Length);
                _logger.LogInformation("=== END NANO_GPT REQUEST DEBUG ===");

                // Log the HttpClient BaseAddress for debugging
                _logger.LogInformation("{ProviderName} HttpClient BaseAddress: {BaseAddress}", ProviderName, _httpClient.BaseAddress?.ToString() ?? "NULL");

                // Log the full request URL being constructed
                var fullUrl = _httpClient.BaseAddress != null
                    ? new Uri(_httpClient.BaseAddress, "/chat/completions").ToString()
                    : "/chat/completions";
                _logger.LogInformation("{ProviderName} Request URL: {RequestUrl}", ProviderName, fullUrl);

                // Log timeout information (configured at DI level)
                _logger.LogInformation("Using HttpClient with timeout: {TimeoutSeconds}s (configured in DI)", _httpClient.Timeout.TotalSeconds);

                var response = await SendRequestWithRetryAsync(
                    () =>
                    {
                        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions")
                        {
                            Content = content
                        };

                        // Add required headers for NanoGpt API
                        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.ApiKey);
                        requestMessage.Headers.Add("Accept", "text/event-stream");
                        return requestMessage;
                    },
                    _settings.MaxRetries,
                    cancellationToken);

                if (response == null)
                {
                    throw new InvalidOperationException("No response received from NanoGpt API");
                }

                var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                // Log complete response details for debugging
                _logger.LogInformation("=== NANO_GPT RESPONSE DEBUG ===");
                _logger.LogInformation("Response Status: {StatusCode}", response.StatusCode);
                _logger.LogInformation("Response Headers:");
                foreach (var header in response.Headers)
                {
                    _logger.LogInformation("  {HeaderName}: {HeaderValue}", header.Key, string.Join(", ", header.Value));
                }
                _logger.LogInformation("Response Content Length: {ContentLength} characters", responseContent.Length);
                _logger.LogInformation("Full Response Content:");
                _logger.LogInformation("{ResponseContent}", responseContent);

                // Also log a preview of the response
                var previewLength = Math.Min(500, responseContent.Length);
                _logger.LogInformation("Response Content Preview (first {PreviewLength} chars):", previewLength);
                _logger.LogInformation("{Preview}", responseContent.Substring(0, previewLength));
                _logger.LogInformation("=== END NANO_GPT RESPONSE DEBUG ===");

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        // Parse non-streaming response format (OpenAI-compatible)
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
                                _logger.LogInformation("NanoGpt response successful, content length: {ContentLength}", finalContent.Length);

                                return new AIResponse
                                {
                                    Content = finalContent,
                                    TokensUsed = 0, // Token usage not available in this format
                                    ProviderName = ProviderName,
                                    IsSuccess = true,
                                    ResponseTime = DateTime.UtcNow - startTime
                                };
                            }
                        }

                        throw new Exception("Invalid response format: missing choices or message content");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to parse NanoGpt response for provider {ProviderName}", ProviderName);
                        return new AIResponse
                        {
                            Content = string.Empty,
                            TokensUsed = 0,
                            ProviderName = ProviderName,
                            IsSuccess = false,
                            ErrorMessage = $"Failed to parse response: {ex.Message}",
                            ResponseTime = DateTime.UtcNow - startTime
                        };
                    }
                }
                else
                {
                    _logger.LogError("{ProviderName} API returned error status: {StatusCode}, content: {ResponseContent}",
                        ProviderName, response.StatusCode, responseContent);

                    // Provide specific guidance based on the status code
                    string errorMessage;
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound || response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed)
                    {
                        errorMessage = $"NanoGpt API endpoint not found ({response.StatusCode}). This usually means either: 1) The API key is invalid/expired, 2) The endpoint URL is incorrect, or 3) The API service is down. Please verify your API key and ensure the service is active. Response: {responseContent}";
                    }
                    else
                    {
                        errorMessage = $"NanoGpt API returned status {response.StatusCode}: {responseContent}";
                    }

                    return new AIResponse
                    {
                        Content = string.Empty,
                        TokensUsed = 0,
                        ProviderName = ProviderName,
                        IsSuccess = false,
                        ErrorMessage = errorMessage,
                        ResponseTime = DateTime.UtcNow - startTime
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in NanoGptClient.CallAsync for provider {ProviderName}", ProviderName);
                return new AIResponse
                {
                    Content = string.Empty,
                    TokensUsed = 0,
                    ProviderName = ProviderName,
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    ResponseTime = DateTime.UtcNow - startTime
                };
            }
        }

        public override async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Simple health check - try to connect to the base URL
                var response = await _httpClient.GetAsync("", cancellationToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("{ProviderName} health check passed", ProviderName);
                }
                else
                {
                    _logger.LogWarning("{ProviderName} health check failed with status {StatusCode}", ProviderName, response.StatusCode);
                }
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in NanoGptClient.IsHealthyAsync for provider {ProviderName}", ProviderName);
                return false;
            }
        }

        public override async Task<IEnumerable<string>> GetModelsAsync()
        {
            try
            {
                // Call the models endpoint for NanoGpt (OpenAI-compatible)
                var response = await _httpClient.GetAsync("v1/models", CancellationToken.None).ConfigureAwait(false);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    using var doc = JsonDocument.Parse(responseContent);
                    var data = doc.RootElement.GetProperty("data");
                    
                    var models = new List<string>();
                    foreach (var model in data.EnumerateArray())
                    {
                        var id = model.GetProperty("id").GetString();
                        if (!string.IsNullOrEmpty(id))
                        {
                            models.Add(id);
                        }
                    }
                    
                    return models;
                }
                else
                {
                    _logger.LogWarning("NanoGptClient failed to retrieve models, status: {StatusCode}", response.StatusCode);
                    // Return a default empty list if the endpoint is not available
                    return new List<string> { _settings.DefaultModel };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in NanoGptClient.GetModelsAsync for provider {ProviderName}", ProviderName);
                return new List<string> { _settings.DefaultModel };
            }
        }
    }
}