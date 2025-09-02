using System;
using Xunit;
using AIProjectOrchestrator.Domain.Exceptions;

namespace AIProjectOrchestrator.UnitTests.AI
{
    public class AIRateLimitExceptionTests
    {
        [Fact]
        public void AIRateLimitException_ShouldSetProviderNameAndRetryAfter()
        {
            // Arrange
            var providerName = "TestProvider";
            var retryAfter = TimeSpan.FromMinutes(1);

            // Act
            var exception = new AIRateLimitException(providerName, retryAfter);

            // Assert
            Assert.Equal(providerName, exception.ProviderName);
            Assert.Equal(retryAfter, exception.RetryAfter);
            Assert.Contains(providerName, exception.Message);
            Assert.Contains(retryAfter.ToString(), exception.Message);
        }
    }
}