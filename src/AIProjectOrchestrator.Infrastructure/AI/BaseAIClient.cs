using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Models.AI;
using AIProjectOrchestrator.Domain.Services;
using Microsoft.Extensions.Logging;

namespace AIProjectOrchestrator.Infrastructure.AI
{
    public abstract class BaseAIClient : IAIClient
    {
        protected readonly HttpClient _httpClient;
        protected readonly ILogger _logger;

        public abstract string ProviderName { get; }

        protected BaseAIClient(HttpClient httpClient, ILogger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public abstract Task<AIResponse> CallAsync(AIRequest request, CancellationToken cancellationToken = default);
        
        public abstract Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);

        protected async Task<HttpResponseMessage> SendRequestWithRetryAsync(
            Func<HttpRequestMessage> requestMessageFactory, 
            int maxRetries, 
            CancellationToken cancellationToken = default)
        {
            HttpResponseMessage? response = null;
            Exception? lastException = null;
            
            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    // Create a new request message for each attempt
                    var requestMessage = requestMessageFactory();
                    response = await _httpClient.SendAsync(requestMessage, cancellationToken);
                    
                    // If successful or not a retryable status code, return the response
                    if (response.IsSuccessStatusCode || 
                        response.StatusCode != System.Net.HttpStatusCode.RequestTimeout &&
                        response.StatusCode != System.Net.HttpStatusCode.TooManyRequests &&
                        (int)response.StatusCode < 500)
                    {
                        return response;
                    }
                    
                    // For retryable status codes, log and retry
                    _logger.LogWarning("Attempt {Attempt} failed with status {StatusCode} for provider {ProviderName}. Retrying...", 
                        attempt + 1, response.StatusCode, ProviderName);
                    
                    // If this is the last attempt, break and let the exception be thrown
                    if (attempt == maxRetries) break;
                    
                    // Wait before retrying with exponential backoff
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // If cancellation was requested, rethrow
                    throw;
                }
                catch (HttpRequestException ex) when (ex.InnerException is System.Net.Sockets.SocketException)
                {
                    // Handle DNS resolution errors (Name or service not known)
                    lastException = ex;
                    _logger.LogWarning(ex, "Attempt {Attempt} failed with network error for provider {ProviderName}. Retrying...", 
                        attempt + 1, ProviderName);
                    
                    // If this is the last attempt, break and let the exception be thrown
                    if (attempt == maxRetries) break;
                    
                    // Wait before retrying with exponential backoff
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), cancellationToken);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogWarning(ex, "Attempt {Attempt} failed with exception for provider {ProviderName}. Retrying...", 
                        attempt + 1, ProviderName);
                    
                    // If this is the last attempt, break and let the exception be thrown
                    if (attempt == maxRetries) break;
                    
                    // Wait before retrying with exponential backoff
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), cancellationToken);
                }
            }
            
            // If we get here, all retries have been exhausted
            if (lastException != null)
                throw lastException;
                
            return response ?? throw new InvalidOperationException("No response received");
        }
    }
}