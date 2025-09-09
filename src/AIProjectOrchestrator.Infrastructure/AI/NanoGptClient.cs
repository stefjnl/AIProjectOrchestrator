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
            AIClientLogger.LogSettings(ProviderName, _settings.BaseUrl, _settings.ApiKey?.Length ?? 0, _settings.DefaultModel);

            // Log the actual API key prefix for debugging (first 10 characters)
            if (!string.IsNullOrEmpty(_settings.ApiKey))
            {
                AIClientLogger.LogApiKeyPrefix(ProviderName, _settings.ApiKey.Substring(0, Math.Min(10, _settings.ApiKey.Length)));
            }

            // Also log the HttpClient BaseAddress in constructor
            AIClientLogger.LogRequestUrl(ProviderName, httpClient.BaseAddress?.ToString() ?? "NULL");
        }

        public override async Task<AIResponse> CallAsync(AIRequest request, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // Create OpenAI-compatible request format for NanoGpt API
                var messages = new object[]
                {
                    new { role = "system", content = request.SystemMessage },
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

                // Log the HttpClient BaseAddress for debugging
                AIClientLogger.LogRequestUrl(ProviderName, _httpClient.BaseAddress?.ToString() ?? "NULL");

                // Log the full request URL being constructed
                var fullUrl = _httpClient.BaseAddress != null
                    ? new Uri(_httpClient.BaseAddress, "v1/chat/completions").ToString()
                    : "v1/chat/completions";
                AIClientLogger.LogRequestUrl(ProviderName, fullUrl);

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

                var responseContent = await response.Content.ReadAsStringAsync();

                // Log response details for debugging
                AIClientLogger.LogResponse(ProviderName, response.StatusCode, responseContent.Length, responseContent.Substring(0, Math.Min(200, responseContent.Length)));

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
                        AIClientLogger.LogException(ProviderName, 0, jsonEx);
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
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}