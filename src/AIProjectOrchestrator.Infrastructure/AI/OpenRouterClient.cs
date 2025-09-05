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
using Microsoft.Extensions.Options;

namespace AIProjectOrchestrator.Infrastructure.AI
{
    public class OpenRouterClient : BaseAIClient, IAIClient
    {
        private readonly OpenRouterSettings _settings;
        
        public override string ProviderName => "OpenRouter";

        public OpenRouterClient(HttpClient httpClient, ILogger<OpenRouterClient> logger, IOptions<AIProviderSettings> settings) 
            : base(httpClient, logger)
        {
            _settings = settings.Value.OpenRouter;
            // Log settings for debugging
            logger.LogInformation("OpenRouter Settings - BaseUrl: {BaseUrl}, ApiKey Length: {ApiKeyLength}, DefaultModel: {DefaultModel}", 
                _settings.BaseUrl, _settings.ApiKey?.Length ?? 0, _settings.DefaultModel);
            
            // Log the actual API key prefix for debugging (first 10 characters)
            if (!string.IsNullOrEmpty(_settings.ApiKey))
            {
                logger.LogInformation("OpenRouter API Key prefix: {ApiKeyPrefix}", _settings.ApiKey.Substring(0, Math.Min(10, _settings.ApiKey.Length)));
            }
            
            // Also log the HttpClient BaseAddress in constructor
            logger.LogInformation("OpenRouter HttpClient BaseAddress in constructor: {BaseAddress}", httpClient.BaseAddress?.ToString() ?? "NULL");
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
                _logger.LogInformation("OpenRouter HttpClient BaseAddress: {BaseAddress}", _httpClient.BaseAddress?.ToString() ?? "NULL");
                
                // Log the full request URL being constructed
                var fullUrl = _httpClient.BaseAddress != null 
                    ? new Uri(_httpClient.BaseAddress, "chat/completions").ToString()
                    : "chat/completions";
                _logger.LogInformation("OpenRouter Full Request URL: {FullUrl}", fullUrl);
                
                var response = await SendRequestWithRetryAsync(
                    () => {
                        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
                        {
                            Content = content
                        };
                        
                        // Add required headers for OpenRouter API
                        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.ApiKey);
                        requestMessage.Headers.Add("HTTP-Referer", "AIProjectOrchestrator");
                        requestMessage.Headers.Add("X-Title", "AIProjectOrchestrator");
                        
                        return requestMessage;
                    }, 
                    _settings.MaxRetries, 
                    cancellationToken);

                var responseContent = await response.Content.ReadAsStringAsync();
                
                // Log response details for debugging
                _logger.LogInformation("OpenRouter API Response - Status: {StatusCode}, Content Length: {ContentLength}, Content Start: {ContentStart}", 
                    response.StatusCode, responseContent.Length, responseContent.Substring(0, Math.Min(200, responseContent.Length)));
                
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
                        _logger.LogError(jsonEx, "Failed to parse JSON response from OpenRouter. Response content: {ResponseContent}", responseContent);
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
