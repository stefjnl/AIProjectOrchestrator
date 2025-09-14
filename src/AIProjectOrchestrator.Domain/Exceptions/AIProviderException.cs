using System;

namespace AIProjectOrchestrator.Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when an AI provider encounters an error.
    /// </summary>
    public class AIProviderException : Exception
    {
        public string ProviderName { get; }
        
        public AIProviderException(string providerName, string message) : base(message)
        {
            ProviderName = providerName;
        }
            
        public AIProviderException(string providerName, string message, Exception innerException)
            : base(message, innerException)
        {
            ProviderName = providerName;
        }
    }

    /// <summary>
    /// Exception thrown when an AI provider rate limit is exceeded.
    /// </summary>
    public class AIRateLimitException : AIProviderException
    {
        public TimeSpan RetryAfter { get; }
        
        public AIRateLimitException(string providerName, TimeSpan retryAfter)
            : base(providerName, $"Rate limit exceeded for {providerName}. Retry after {retryAfter}")
        {
            RetryAfter = retryAfter;
        }
    }

    /// <summary>
    /// Exception thrown when an AI request times out.
    /// </summary>
    public class AITimeoutException : AIProviderException
    {
        public AITimeoutException(string providerName, TimeSpan timeout)
            : base(providerName, $"Request to {providerName} timed out after {timeout}") { }
    }

    /// <summary>
    /// Exception thrown when validation fails.
    /// </summary>
    public class ValidationException : ArgumentException
    {
        public ValidationException(string message) : base(message) { }
    }
}