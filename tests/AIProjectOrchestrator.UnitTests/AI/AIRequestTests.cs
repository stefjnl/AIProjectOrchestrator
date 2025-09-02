using Xunit;
using AIProjectOrchestrator.Domain.Models.AI;

namespace AIProjectOrchestrator.UnitTests.AI
{
    public class AIRequestTests
    {
        [Fact]
        public void AIRequest_ShouldInitializeWithDefaultValues()
        {
            // Arrange & Act
            var request = new AIRequest();

            // Assert
            Assert.Equal(string.Empty, request.Prompt);
            Assert.Equal(string.Empty, request.SystemMessage);
            Assert.Equal(string.Empty, request.ModelName);
            Assert.Equal(0.7, request.Temperature);
            Assert.Equal(1000, request.MaxTokens);
            Assert.NotNull(request.AdditionalProperties);
            Assert.Empty(request.AdditionalProperties);
        }

        [Fact]
        public void AIRequest_ShouldAllowSettingProperties()
        {
            // Arrange
            var request = new AIRequest();
            var prompt = "Test prompt";
            var systemMessage = "Test system message";
            var modelName = "test-model";
            var temperature = 0.5;
            var maxTokens = 500;

            // Act
            request.Prompt = prompt;
            request.SystemMessage = systemMessage;
            request.ModelName = modelName;
            request.Temperature = temperature;
            request.MaxTokens = maxTokens;

            // Assert
            Assert.Equal(prompt, request.Prompt);
            Assert.Equal(systemMessage, request.SystemMessage);
            Assert.Equal(modelName, request.ModelName);
            Assert.Equal(temperature, request.Temperature);
            Assert.Equal(maxTokens, request.MaxTokens);
        }
    }
}