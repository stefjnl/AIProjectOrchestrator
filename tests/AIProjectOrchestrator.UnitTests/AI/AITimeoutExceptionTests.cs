using System;
using Xunit;
using AIProjectOrchestrator.Domain.Exceptions;

namespace AIProjectOrchestrator.UnitTests.AI
{
    public class AITimeoutExceptionTests
    {
        [Fact]
        public void AITimeoutException_ShouldSetProviderNameAndTimeout()
        {
            // Arrange
            var providerName = "TestProvider";
            var timeout = TimeSpan.FromSeconds(30);

            // Act
            var exception = new AITimeoutException(providerName, timeout);

            // Assert
            Assert.Equal(providerName, exception.ProviderName);
            Assert.Contains(providerName, exception.Message);
            Assert.Contains(timeout.ToString(), exception.Message);
        }
    }
}