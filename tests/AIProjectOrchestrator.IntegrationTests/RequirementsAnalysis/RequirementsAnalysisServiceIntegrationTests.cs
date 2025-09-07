using AIProjectOrchestrator.Application.Services;
using AIProjectOrchestrator.Domain.Models;
using AIProjectOrchestrator.Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Xunit;

namespace AIProjectOrchestrator.IntegrationTests.RequirementsAnalysis
{
    [Collection("Sequential")]
    public class RequirementsAnalysisServiceIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public RequirementsAnalysisServiceIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task RequirementsAnalysisService_CanBeResolvedFromDIContainer()
        {
            // Arrange
            var scope = _factory.Services.CreateScope();
            var serviceProvider = scope.ServiceProvider;

            // Act
            var requirementsAnalysisService = serviceProvider.GetService<IRequirementsAnalysisService>();

            // Assert
            Assert.NotNull(requirementsAnalysisService);
            Assert.IsType<RequirementsAnalysisService>(requirementsAnalysisService);
        }

        [Fact]
        public async Task RequirementsAnalysisService_CanLoadInstructionFile()
        {
            // Arrange
            var scope = _factory.Services.CreateScope();
            var serviceProvider = scope.ServiceProvider;
            var requirementsAnalysisService = serviceProvider.GetService<IRequirementsAnalysisService>();
            var instructionService = serviceProvider.GetService<IInstructionService>();

            // Act
            var instructionResult = await instructionService!.GetInstructionAsync("RequirementsAnalyst");

            // Assert
            Assert.NotNull(instructionResult);
            Assert.Equal("RequirementsAnalyst", instructionResult.ServiceName);
            Assert.True(instructionResult.IsValid, $"Instruction should be valid. Validation message: {instructionResult.ValidationMessage}");
            Assert.NotNull(instructionResult.Content);
            Assert.NotEmpty(instructionResult.Content);
        }
    }
}
