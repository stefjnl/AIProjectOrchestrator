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
    public class LMStudioClient : BaseAIClientHandler, IAIClient
    {
        private readonly AIProviderConfigurationService _configurationService;
        private readonly LMStudioSettings _settings;

        public override string ProviderName => "LMStudio";

        public LMStudioClient(HttpClient httpClient, ILogger<LMStudioClient> logger, AIProviderConfigurationService configurationService)
            : base(httpClient, logger)
        {
            _configurationService = configurationService;
            _settings = configurationService.GetProviderSettings<LMStudioSettings>(ProviderName);

            // Log settings for debugging
            AIClientLogger.LogSettings(ProviderName, _settings.BaseUrl, 0, _settings.DefaultModel);

            // Also log the HttpClient BaseAddress in constructor
            AIClientLogger.LogRequestUrl(ProviderName, httpClient.BaseAddress?.ToString() ?? "NULL");
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

                        return requestMessage;
                    },
                    _settings.MaxRetries,
                    cancellationToken);

                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
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
                else
                {
                    return new AIResponse
                    {
                        Content = string.Empty,
                        TokensUsed = 0,
                        ProviderName = ProviderName,
                        IsSuccess = false,
                        ErrorMessage = $"LM Studio API returned status {response.StatusCode}: {responseContent}",
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