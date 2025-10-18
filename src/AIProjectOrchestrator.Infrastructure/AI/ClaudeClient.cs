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
    public class ClaudeClient : BaseAIClientHandler, IAIClient
    {
        private readonly AIProviderConfigurationService _configurationService;
        private readonly ClaudeCredentials _settings;

        public override string ProviderName => "Claude";

        public ClaudeClient(HttpClient httpClient, ILogger<ClaudeClient> logger, AIProviderConfigurationService configurationService)
            : base(httpClient, logger)
        {
            _configurationService = configurationService;
            _settings = _configurationService.GetProviderSettings<ClaudeCredentials>(ProviderName);

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
                var claudeRequest = new
                {
                    model = string.IsNullOrEmpty(request.ModelName) ? _settings.DefaultModel : request.ModelName,
                    max_tokens = request.MaxTokens,
                    messages = new[]
                    {
                        new { role = "user", content = request.Prompt }
                    },
                    system = request.SystemMessage,
                    temperature = request.Temperature
                };

                var json = JsonSerializer.Serialize(claudeRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Log the HttpClient BaseAddress for debugging
                _logger.LogInformation("{ProviderName} HttpClient BaseAddress: {BaseAddress}", ProviderName, _httpClient.BaseAddress?.ToString() ?? "NULL");

                // Log the full request URL being constructed
                var fullUrl = _httpClient.BaseAddress != null
                    ? new Uri(_httpClient.BaseAddress, "messages").ToString()
                    : "messages";
                _logger.LogInformation("{ProviderName} Request URL: {RequestUrl}", ProviderName, fullUrl);

                var response = await SendRequestWithRetryAsync(
                    () =>
                    {
                        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "messages")
                        {
                            Content = content
                        };

                        // Add required headers for Claude API
                        requestMessage.Headers.Add("x-api-key", _settings.ApiKey);
                        requestMessage.Headers.Add("anthropic-version", "2023-06-01");

                        return requestMessage;
                    },
                    _settings.MaxRetries,
                    cancellationToken).ConfigureAwait(false);

                var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    // Parse Claude's response format
                    using var doc = JsonDocument.Parse(responseContent);
                    var root = doc.RootElement;

                    var contentElement = root.GetProperty("content");
                    var firstContent = contentElement[0];
                    var text = firstContent.GetProperty("text").GetString() ?? string.Empty;

                    var usage = root.GetProperty("usage");
                    var tokensUsed = usage.GetProperty("output_tokens").GetInt32();

                    return new AIResponse
                    {
                        Content = text,
                        TokensUsed = tokensUsed,
                        ProviderName = ProviderName,
                        IsSuccess = true,
                        ResponseTime = DateTime.UtcNow - startTime
                    };
                }
                else
                {
                    return new AIResponse
                    {
                        Content = string.Empty,
                        TokensUsed = 0,
                        ProviderName = ProviderName,
                        IsSuccess = false,
                        ErrorMessage = $"Claude API returned status {response.StatusCode}: {responseContent}",
                        ResponseTime = DateTime.UtcNow - startTime
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ClaudeClient.CallAsync for provider {ProviderName}", ProviderName);
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
                _logger.LogError(ex, "Error in ClaudeClient.IsHealthyAsync for provider {ProviderName}", ProviderName);
                return false;
            }
        }

        public override Task<IEnumerable<string>> GetModelsAsync()
        {
            try
            {
                // Claude API doesn't have a direct models endpoint like OpenAI
                // Instead, return the default models that Claude supports
                var defaultClaudeModels = new[]
                {
                    "claude-3-5-sonnet-20241022",
                    "claude-3-5-sonnet-latest",
                    "claude-3-opus-20240229",
                    "claude-3-sonnet-20240229",
                    "claude-3-haiku-20240307",
                    _settings.DefaultModel // Include the configured default model
                };

                // Filter out any null/empty entries
                return Task.FromResult(defaultClaudeModels.Where(m => !string.IsNullOrEmpty(m)).Distinct());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ClaudeClient.GetModelsAsync for provider {ProviderName}", ProviderName);
                return Task.FromResult<IEnumerable<string>>(new List<string>());
            }
        }
    }
}