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
        private readonly NanoGptSettings _settings;

        public override string ProviderName => "NanoGpt";

        public NanoGptClient(HttpClient httpClient, ILogger<NanoGptClient> logger, AIProviderConfigurationService configurationService)
            : base(httpClient, logger)
        {
            _configurationService = configurationService;
            _settings = _configurationService.GetProviderSettings<NanoGptSettings>(ProviderName);

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
                    stream = false // Disable streaming for this implementation
                };

                var json = JsonSerializer.Serialize(nanoGptRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Log the request size for debugging
                _logger.LogInformation("NanoGpt request JSON length: {JsonLength} characters", json.Length);

                // Log the HttpClient BaseAddress for debugging
                _logger.LogInformation("{ProviderName} HttpClient BaseAddress: {BaseAddress}", ProviderName, _httpClient.BaseAddress?.ToString() ?? "NULL");

                // Log the full request URL being constructed
                var fullUrl = _httpClient.BaseAddress != null
                    ? new Uri(_httpClient.BaseAddress, "chat/completions").ToString()
                    : "chat/completions";
                _logger.LogInformation("{ProviderName} Request URL: {RequestUrl}", ProviderName, fullUrl);

                // Log timeout information (configured at DI level)
                _logger.LogInformation("Using HttpClient with timeout: {TimeoutSeconds}s (configured in DI)", _httpClient.Timeout.TotalSeconds);

                var response = await SendRequestWithRetryAsync(
                    () =>
                    {
                        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
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

                var responseContent = await response.Content.ReadAsStringAsync();

                // Log response details for debugging
                _logger.LogInformation("{ProviderName} API Response - Status: {StatusCode}, Content Length: {ContentLength}, Content Start: {ContentStart}",
                    ProviderName, response.StatusCode, responseContent.Length, responseContent.Substring(0, Math.Min(200, responseContent.Length)));

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        // Parse OpenAI-compatible response format
                        using var doc = JsonDocument.Parse(responseContent);
                        var root = doc.RootElement;

                        var choices = root.GetProperty("choices");
                        var firstChoice = choices[0];
                        var message = firstChoice.GetProperty("message");
                        var text = message.GetProperty("content").GetString() ?? string.Empty;

                        // Extract token usage if available
                        var tokensUsed = 0;
                        if (root.TryGetProperty("usage", out var usageElement))
                        {
                            tokensUsed = usageElement.GetProperty("completion_tokens").GetInt32();
                        }

                        _logger.LogInformation("NanoGpt response successful, content length: {ContentLength}, tokens used: {TokensUsed}",
                            text.Length, tokensUsed);

                        return new AIResponse
                        {
                            Content = text,
                            TokensUsed = tokensUsed,
                            ProviderName = ProviderName,
                            IsSuccess = true,
                            ResponseTime = DateTime.UtcNow - startTime
                        };
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogError(jsonEx, "Failed to parse NanoGpt JSON response for provider {ProviderName}", ProviderName);
                        return new AIResponse
                        {
                            Content = string.Empty,
                            TokensUsed = 0,
                            ProviderName = ProviderName,
                            IsSuccess = false,
                            ErrorMessage = $"Failed to parse JSON response: {jsonEx.Message}. Response content starts with: {responseContent.Substring(0, Math.Min(100, responseContent.Length))}",
                            ResponseTime = DateTime.UtcNow - startTime
                        };
                    }
                }
                else
                {
                    _logger.LogWarning("{ProviderName} API returned error status: {StatusCode}, content: {ResponseContent}",
                        ProviderName, response.StatusCode, responseContent);
                    return new AIResponse
                    {
                        Content = string.Empty,
                        TokensUsed = 0,
                        ProviderName = ProviderName,
                        IsSuccess = false,
                        ErrorMessage = $"NanoGpt API returned status {response.StatusCode}: {responseContent}",
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
                var response = await _httpClient.GetAsync("", cancellationToken);
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
    }
}