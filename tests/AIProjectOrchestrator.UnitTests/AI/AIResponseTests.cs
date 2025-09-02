using System;
using Xunit;
using AIProjectOrchestrator.Domain.Models.AI;

namespace AIProjectOrchestrator.UnitTests.AI
{
    public class AIResponseTests
    {
        [Fact]
        public void AIResponse_ShouldInitializeWithDefaultValues()
        {
            // Arrange & Act
            var response = new AIResponse();

            // Assert
            Assert.Equal(string.Empty, response.Content);
            Assert.Equal(0, response.TokensUsed);
            Assert.Equal(string.Empty, response.ProviderName);
            Assert.False(response.IsSuccess);
            Assert.Null(response.ErrorMessage);
            Assert.Equal(TimeSpan.Zero, response.ResponseTime);
            Assert.NotNull(response.Metadata);
            Assert.Empty(response.Metadata);
        }

        [Fact]
        public void AIResponse_ShouldAllowSettingProperties()
        {
            // Arrange
            var response = new AIResponse();
            var content = "Test response";
            var tokensUsed = 100;
            var providerName = "TestProvider";
            var isSuccess = true;
            var errorMessage = "Test error";
            var responseTime = TimeSpan.FromMilliseconds(100);

            // Act
            response.Content = content;
            response.TokensUsed = tokensUsed;
            response.ProviderName = providerName;
            response.IsSuccess = isSuccess;
            response.ErrorMessage = errorMessage;
            response.ResponseTime = responseTime;

            // Assert
            Assert.Equal(content, response.Content);
            Assert.Equal(tokensUsed, response.TokensUsed);
            Assert.Equal(providerName, response.ProviderName);
            Assert.Equal(isSuccess, response.IsSuccess);
            Assert.Equal(errorMessage, response.ErrorMessage);
            Assert.Equal(responseTime, response.ResponseTime);
        }
    }
}