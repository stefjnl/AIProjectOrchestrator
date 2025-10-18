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
    public class OpenRouterClient : BaseAIClientHandler, IAIClient
    {
        private readonly AIProviderConfigurationService _configurationService;
        private readonly OpenRouterCredentials _settings;

        public override string ProviderName => "OpenRouter";

        public OpenRouterClient(HttpClient httpClient, ILogger<OpenRouterClient> logger, AIProviderConfigurationService configurationService)
            : base(httpClient, logger)
        {
            _configurationService = configurationService;
            _logger.LogInformation("OpenRouterClient constructor called");
            _settings = _configurationService.GetProviderSettings<OpenRouterCredentials>(ProviderName);

            // Log settings for debugging
            _logger.LogInformation("{ProviderName} Settings - BaseUrl: {BaseUrl}, ApiKey Length: {ApiKeyLength}, DefaultModel: {DefaultModel}",
                ProviderName, _settings.BaseUrl, _settings.ApiKey?.Length ?? 0, _settings.DefaultModel);

            // Log the actual API key prefix for debugging (first 10 characters)
            if (!string.IsNullOrEmpty(_settings.ApiKey))
            {
                _logger.LogInformation("{ProviderName} API Key prefix: {ApiKeyPrefix}", ProviderName, _settings.ApiKey.Substring(0, Math.Min(10, _settings.ApiKey.Length)));
            }
            else
            {
                _logger.LogError("{ProviderName} API Key is null or empty!", ProviderName);
            }

            // Also log the HttpClient BaseAddress in constructor
            _logger.LogInformation("{ProviderName} HttpClient BaseAddress: {BaseAddress}", ProviderName, httpClient.BaseAddress?.ToString() ?? "NULL");
        }

        public override async Task<AIResponse> CallAsync(AIRequest request, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                var messages = new object[]
                {
                    new { role = "system", content = request.SystemMessage },
                    new { role = "user", content = request.Prompt }
                };

                var openAIRequest = new
                {
                    model = string.IsNullOrEmpty(request.ModelName) ? _settings.DefaultModel : request.ModelName,
                    messages = messages,
                    temperature = request.Temperature,
                    max_tokens = request.MaxTokens
                };

                var json = JsonSerializer.Serialize(openAIRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Log the HttpClient BaseAddress for debugging
                _logger.LogInformation("{ProviderName} HttpClient BaseAddress: {BaseAddress}", ProviderName, _httpClient.BaseAddress?.ToString() ?? "NULL");

                // Log the full request URL being constructed
                var fullUrl = _httpClient.BaseAddress != null
                    ? new Uri(_httpClient.BaseAddress, "chat/completions").ToString()
                    : "chat/completions";
                _logger.LogInformation("{ProviderName} Request URL: {RequestUrl}", ProviderName, fullUrl);

                var response = await SendRequestWithRetryAsync(
                    () =>
                    {
                        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
                        {
                            Content = content
                        };

                        // Add required headers for OpenRouter API
                        _logger.LogInformation("Adding OpenRouter authentication headers - API Key length: {ApiKeyLength}", _settings.ApiKey?.Length ?? 0);
                        if (!string.IsNullOrEmpty(_settings.ApiKey))
                        {
                            requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.ApiKey);
                            _logger.LogInformation("Authorization header added: Bearer {ApiKeyPrefix}...", _settings.ApiKey.Substring(0, Math.Min(10, _settings.ApiKey.Length)));
                        }
                        else
                        {
                            _logger.LogError("API Key is null or empty for OpenRouter!");
                        }
                        requestMessage.Headers.Add("HTTP-Referer", "AIProjectOrchestrator");
                        requestMessage.Headers.Add("X-Title", "AIProjectOrchestrator");

                        // Log all headers that will be sent
                        _logger.LogInformation("Request headers being sent:");
                        foreach (var header in requestMessage.Headers)
                        {
                            var headerValue = header.Key == "Authorization" ? "[REDACTED]" : string.Join(", ", header.Value);
                            _logger.LogInformation("  {HeaderName}: {HeaderValue}", header.Key, headerValue);
                        }

                        return requestMessage;
                    },
                    _settings.MaxRetries,
                    cancellationToken);

                var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

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

                        var usage = root.GetProperty("usage");
                        var tokensUsed = usage.GetProperty("completion_tokens").GetInt32();

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
                        _logger.LogError(jsonEx, "Failed to parse OpenRouter JSON response for provider {ProviderName}", ProviderName);
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
                        ErrorMessage = $"OpenRouter API returned status {response.StatusCode}: {responseContent}",
                        ResponseTime = DateTime.UtcNow - startTime
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OpenRouterClient.CallAsync for provider {ProviderName}", ProviderName);
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
                _logger.LogError(ex, "Error in OpenRouterClient.IsHealthyAsync for provider {ProviderName}", ProviderName);
                return false;
            }
        }

        public override async Task<IEnumerable<string>> GetModelsAsync()
        {
            try
            {
                // Call the models endpoint for OpenRouter
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
                    _logger.LogWarning("OpenRouterClient failed to retrieve models, status: {StatusCode}", response.StatusCode);
                    // Return a default empty list if the endpoint is not available
                    return new List<string> { _settings.DefaultModel };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OpenRouterClient.GetModelsAsync for provider {ProviderName}", ProviderName);
                return new List<string> { _settings.DefaultModel };
            }
        }
    }
}
