using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Application.Services;
using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Models;
using AIProjectOrchestrator.Domain.Models.AI;
using AIProjectOrchestrator.Domain.Models.Code;
using AIProjectOrchestrator.Domain.Services;
using AIProjectOrchestrator.Infrastructure.AI;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Language.Flow;
using Xunit;

namespace AIProjectOrchestrator.UnitTests.Code
{
    public class ImplementationGeneratorTests
    {
        private readonly Mock<IAIClientFactory> _mockAiClientFactory;
        private readonly Mock<ILogger<ImplementationGenerator>> _mockLogger;
        private readonly ImplementationGenerator _generator;

        public ImplementationGeneratorTests()
        {
            _mockAiClientFactory = new Mock<IAIClientFactory>();
            _mockLogger = new Mock<ILogger<ImplementationGenerator>>();
            _generator = new ImplementationGenerator(_mockAiClientFactory.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GenerateImplementationAsync_ValidInput_ReturnsCodeArtifacts()
        {
            // Arrange
            var instructionContent = "Generate implementation code following Clean Architecture principles.";
            var context = new ComprehensiveContext
            {
                Stories = new List<UserStory>
                {
                    new UserStory
                    {
                        Title = "User Service Implementation",
                        Description = "Implement user service with login functionality",
                        AcceptanceCriteria = new List<string> { "Validate user credentials", "Return authenticated user" }
                    }
                },
                TechnicalContext = ".NET 9 with Entity Framework Core",
                BusinessContext = "Enterprise user management system"
            };
            var testFiles = new List<CodeArtifact>
            {
                new CodeArtifact
                {
                    FileName = "UserServiceTests.cs",
                    Content = "public class UserServiceTests { [Fact] public void Login_ValidCredentials_Succeeds() { } }",
                    FileType = "Test"
                }
            };
            var selectedModel = "qwen3-coder";

            var mockAiClient = new Mock<IAIClient>();
            mockAiClient.Setup(c => c.CallAsync(It.IsAny<AIRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIResponse
                {
                    IsSuccess = true,
                    Content = @"```csharp:UserService.cs
using System;
public class UserService
{
    public async Task<User> LoginAsync(string username, string password)
    {
        // Implementation logic
        return new User { Username = username };
    }
}
```"
                });

            _mockAiClientFactory.Setup(f => f.GetClient(It.IsAny<string>())).Returns(mockAiClient.Object);

            // Act
            var result = await _generator.GenerateImplementationAsync(instructionContent, context, testFiles, selectedModel, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Implementation", result[0].FileType);
            Assert.Contains("UserService.cs", result[0].FileName);
            Assert.Contains("LoginAsync", result[0].Content);
            _mockAiClientFactory.Verify(f => f.GetClient(It.IsAny<string>()), Times.Once);
            mockAiClient.Verify(c => c.CallAsync(It.IsAny<AIRequest>(), It.IsAny<CancellationToken>()), Times.Once);
            // _mockLogger.Verify(l => l.LogInformation(It.IsAny<string>(), It.IsAny<object[]>()), Times.AtLeastOnce); // Extension method not supported by Moq
        }

        [Fact]
        public async Task GenerateImplementationAsync_NoCodeBlocksInResponse_ReturnsSingleArtifact()
        {
            // Arrange
            var instructionContent = "Generate code.";
            var context = new ComprehensiveContext { Stories = new List<UserStory>() };
            var testFiles = new List<CodeArtifact>();
            var selectedModel = "qwen3-coder";

            var mockAiClient = new Mock<IAIClient>();
            mockAiClient.Setup(c => c.CallAsync(It.IsAny<AIRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIResponse
                {
                    IsSuccess = true,
                    Content = "Implementation code without code blocks: public class Service {}"
                });

            _mockAiClientFactory.Setup(f => f.GetClient(It.IsAny<string>())).Returns(mockAiClient.Object);

            // Act
            var result = await _generator.GenerateImplementationAsync(instructionContent, context, testFiles, selectedModel, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Implementation", result[0].FileType);
            Assert.Equal("GeneratedImplementation.cs", result[0].FileName);
            Assert.Contains("public class Service", result[0].Content);
        }

        [Fact]
        public async Task GenerateImplementationAsync_AIClientNull_ThrowsInvalidOperationException()
        {
            // Arrange
            var instructionContent = "Test";
            var context = new ComprehensiveContext { Stories = new List<UserStory>() };
            var testFiles = new List<CodeArtifact>();
            var selectedModel = "qwen3-coder";

            _mockAiClientFactory.Setup(f => f.GetClient(It.IsAny<string>())).Returns((IAIClient?)null);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _generator.GenerateImplementationAsync(instructionContent, context, testFiles, selectedModel, CancellationToken.None));
        }

        [Fact]
        public async Task GenerateImplementationAsync_AIFailure_ThrowsInvalidOperationException()
        {
            // Arrange
            var instructionContent = "Test";
            var context = new ComprehensiveContext { Stories = new List<UserStory>() };
            var testFiles = new List<CodeArtifact>();
            var selectedModel = "qwen3-coder";

            var mockAiClient = new Mock<IAIClient>();
            mockAiClient.Setup(c => c.CallAsync(It.IsAny<AIRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIResponse { IsSuccess = false, ErrorMessage = "Service error" });

            _mockAiClientFactory.Setup(f => f.GetClient(It.IsAny<string>())).Returns(mockAiClient.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _generator.GenerateImplementationAsync(instructionContent, context, testFiles, selectedModel, CancellationToken.None));
        }

        // Note: Tests for private methods GetModelName, GetProviderName, CreateImplementationPromptFromContext,
        // and ParseAIResponseToCodeArtifacts are omitted as they are internal/private.
        // These should be tested through the public GenerateImplementationAsync method or made internal
        // with InternalsVisibleTo attribute if direct testing is required.
    }
}