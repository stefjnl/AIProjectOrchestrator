using System.Collections.Generic;
using System.Linq;
using Moq;
using Xunit;
using AIProjectOrchestrator.Domain.Services;
using AIProjectOrchestrator.Infrastructure.AI;
using Microsoft.Extensions.Logging;

namespace AIProjectOrchestrator.UnitTests.AI
{
    public class AIClientFactoryTests
    {
        [Fact]
        public void GetClient_WithValidProviderName_ShouldReturnCorrectClient()
        {
            // Arrange
            var mockClient1 = new Mock<IAIClient>();
            mockClient1.SetupGet(c => c.ProviderName).Returns("Claude");
            
            var mockClient2 = new Mock<IAIClient>();
            mockClient2.SetupGet(c => c.ProviderName).Returns("LMStudio");
            
            var clients = new List<IAIClient> { mockClient1.Object, mockClient2.Object };
            var fallbackService = new Mock<AIClientFallbackService>(clients, new Mock<ILogger<AIClientFallbackService>>().Object);
            var factory = new AIClientFactory(clients, fallbackService.Object);
            
            // Act
            var result = factory.GetClient("Claude");
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal("Claude", result?.ProviderName);
        }
        
        [Fact]
        public void GetClient_WithInvalidProviderName_ShouldReturnNull()
        {
            // Arrange
            var mockClient = new Mock<IAIClient>();
            mockClient.SetupGet(c => c.ProviderName).Returns("Claude");
            
            var clients = new List<IAIClient> { mockClient.Object };
            var fallbackService = new Mock<AIClientFallbackService>(clients, new Mock<ILogger<AIClientFallbackService>>().Object);
            var factory = new AIClientFactory(clients, fallbackService.Object);
            
            // Act
            var result = factory.GetClient("NonExistentProvider");
            
            // Assert
            Assert.Null(result);
        }
        
        [Fact]
        public void GetAllClients_ShouldReturnAllClients()
        {
            // Arrange
            var mockClient1 = new Mock<IAIClient>();
            mockClient1.SetupGet(c => c.ProviderName).Returns("Claude");
            
            var mockClient2 = new Mock<IAIClient>();
            mockClient2.SetupGet(c => c.ProviderName).Returns("LMStudio");
            
            var clients = new List<IAIClient> { mockClient1.Object, mockClient2.Object };
            var fallbackService = new Mock<AIClientFallbackService>(clients, new Mock<ILogger<AIClientFallbackService>>().Object);
            var factory = new AIClientFactory(clients, fallbackService.Object);
            
            // Act
            var result = factory.GetAllClients();
            
            // Assert
            Assert.Equal(2, result.Count());
            Assert.Contains(result, c => c.ProviderName == "Claude");
            Assert.Contains(result, c => c.ProviderName == "LMStudio");
        }
    }
}