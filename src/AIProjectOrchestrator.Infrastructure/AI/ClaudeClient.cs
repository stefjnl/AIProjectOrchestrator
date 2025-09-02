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
    public class ClaudeClient : BaseAIClient, IAIClient
    {
        private readonly ClaudeSettings _settings;
        
        public override string ProviderName => "Claude";

        public ClaudeClient(HttpClient httpClient, ILogger<ClaudeClient> logger, IOptions<AIProviderSettings> settings) 
            : base(httpClient, logger)
        {
            _settings = settings.Value.Claude;
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
                
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, "v1/messages")
                {
                    Content = content
                };
                
                requestMessage.Headers.Add("x-api-key", _settings.ApiKey);
                requestMessage.Headers.Add("anthropic-version", "2023-06-01");

                var response = await SendRequestWithRetryAsync(
                    requestMessage, 
                    _settings.MaxRetries, 
                    cancellationToken);

                var responseContent = await response.Content.ReadAsStringAsync();
                
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