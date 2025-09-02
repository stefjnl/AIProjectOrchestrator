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
                
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
                {
                    Content = content
                };
                
                requestMessage.Headers.Add("Authorization", $"Bearer {_settings.ApiKey}");
                requestMessage.Headers.Add("HTTP-Referer", "AIProjectOrchestrator");
                requestMessage.Headers.Add("X-Title", "AIProjectOrchestrator");

                var response = await SendRequestWithRetryAsync(
                    requestMessage, 
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