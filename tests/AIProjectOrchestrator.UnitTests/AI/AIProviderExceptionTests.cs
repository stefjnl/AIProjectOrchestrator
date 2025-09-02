using System;
using Xunit;
using AIProjectOrchestrator.Domain.Exceptions;

namespace AIProjectOrchestrator.UnitTests.AI
{
    public class AIProviderExceptionTests
    {
        [Fact]
        public void AIProviderException_ShouldSetProviderName()
        {
            // Arrange
            var providerName = "TestProvider";
            var message = "Test message";

            // Act
            var exception = new AIProviderException(providerName, message);

            // Assert
            Assert.Equal(providerName, exception.ProviderName);
            Assert.Equal(message, exception.Message);
        }

        [Fact]
        public void AIProviderException_WithInnerException_ShouldSetProviderNameAndInnerException()
        {
            // Arrange
            var providerName = "TestProvider";
            var message = "Test message";
            var innerException = new Exception("Inner exception");

            // Act
            var exception = new AIProviderException(providerName, message, innerException);

            // Assert
            Assert.Equal(providerName, exception.ProviderName);
            Assert.Equal(message, exception.Message);
            Assert.Equal(innerException, exception.InnerException);
        }
    }
}