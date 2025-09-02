using AIProjectOrchestrator.Application.Services;
using AIProjectOrchestrator.Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Xunit;

namespace AIProjectOrchestrator.IntegrationTests.Services
{
    [Collection("Sequential")]
    public class InstructionServiceIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public InstructionServiceIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task InstructionService_CanBeResolvedFromDIContainer()
        {
            // Arrange
            var scope = _factory.Services.CreateScope();
            var serviceProvider = scope.ServiceProvider;

            // Act
            var instructionService = serviceProvider.GetService<IInstructionService>();

            // Assert
            Assert.NotNull(instructionService);
            Assert.IsType<InstructionService>(instructionService);
        }

        [Fact]
        public async Task InstructionService_CanLoadExistingInstructionFile()
        {
            // Arrange
            var scope = _factory.Services.CreateScope();
            var serviceProvider = scope.ServiceProvider;
            var instructionService = serviceProvider.GetService<IInstructionService>();

            // Act
            var result = await instructionService!.GetInstructionAsync("RequirementsAnalysisService");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("RequirementsAnalysisService", result.ServiceName);
            Assert.NotEqual(string.Empty, result.Content);
            Assert.True(result.IsValid);
            Assert.Equal(string.Empty, result.ValidationMessage);
        }

        [Fact]
        public async Task InstructionService_ReturnsInvalidForNonExistentService()
        {
            // Arrange
            var scope = _factory.Services.CreateScope();
            var serviceProvider = scope.ServiceProvider;
            var instructionService = serviceProvider.GetService<IInstructionService>();

            // Act
            var result = await instructionService!.GetInstructionAsync("NonExistentService");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("NonExistentService", result.ServiceName);
            Assert.False(result.IsValid);
            Assert.Contains("not found", result.ValidationMessage);
        }
    }
}