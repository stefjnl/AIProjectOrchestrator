using System;

namespace AIProjectOrchestrator.Domain.Exceptions
{
    public class AIProviderException : Exception
    {
        public string ProviderName { get; }
        
        public AIProviderException(string providerName, string message) : base(message)
            => ProviderName = providerName;
            
        public AIProviderException(string providerName, string message, Exception innerException) 
            : base(message, innerException) => ProviderName = providerName;
    }

    public class AIRateLimitException : AIProviderException
    {
        public TimeSpan RetryAfter { get; }
        
        public AIRateLimitException(string providerName, TimeSpan retryAfter) 
            : base(providerName, $"Rate limit exceeded for {providerName}. Retry after {retryAfter}")
            => RetryAfter = retryAfter;
    }

    public class AITimeoutException : AIProviderException
    {
        public AITimeoutException(string providerName, TimeSpan timeout)
            : base(providerName, $"Request to {providerName} timed out after {timeout}") { }
    }
}