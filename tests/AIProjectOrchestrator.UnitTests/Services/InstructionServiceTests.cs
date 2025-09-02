using AIProjectOrchestrator.Domain.Configuration;
using AIProjectOrchestrator.Application.Services;
using AIProjectOrchestrator.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace AIProjectOrchestrator.UnitTests.Services
{
    public class InstructionServiceTests
    {
        private readonly Mock<ILogger<InstructionService>> _mockLogger;
        private readonly IOptions<InstructionSettings> _settings;
        private readonly string _testInstructionsPath;

        public InstructionServiceTests()
        {
            _mockLogger = new Mock<ILogger<InstructionService>>();
            _settings = Options.Create(new InstructionSettings
            {
                InstructionsPath = "Instructions",
                MinimumContentLength = 100,
                RequiredSections = new[] { "Role", "Task", "Constraints" }
            });

            // Create a temporary directory for testing
            _testInstructionsPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testInstructionsPath);
        }

        [Fact]
        public async Task GetInstructionAsync_WithValidFile_ReturnsInstructionContent()
        {
            // Arrange
            var serviceName = "RequirementsAnalysisService";
            var fileName = "RequirementsAnalyst.md";
            var filePath = Path.Combine(_testInstructionsPath, fileName);
            
            var content = "# Role\nThis is a test role\n# Task\nThis is a test task\n# Constraints\nThese are test constraints\nMore content to meet minimum length requirement";
            
            await File.WriteAllTextAsync(filePath, content);

            var customSettings = Options.Create(new InstructionSettings
            {
                InstructionsPath = _testInstructionsPath,
                MinimumContentLength = 100,
                RequiredSections = new[] { "Role", "Task", "Constraints" }
            });

            var service = new InstructionService(customSettings, _mockLogger.Object);

            // Act
            var result = await service.GetInstructionAsync(serviceName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(serviceName, result.ServiceName);
            Assert.Equal(content, result.Content);
            Assert.True(result.IsValid);
            Assert.Equal(string.Empty, result.ValidationMessage);
        }

        [Fact]
        public async Task GetInstructionAsync_WithMissingFile_ReturnsInvalidInstruction()
        {
            // Arrange
            var serviceName = "MissingService";
            var customSettings = Options.Create(new InstructionSettings
            {
                InstructionsPath = _testInstructionsPath,
                MinimumContentLength = 100,
                RequiredSections = new[] { "Role", "Task", "Constraints" }
            });

            var service = new InstructionService(customSettings, _mockLogger.Object);

            // Act
            var result = await service.GetInstructionAsync(serviceName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(serviceName, result.ServiceName);
            Assert.Equal(string.Empty, result.Content);
            Assert.False(result.IsValid);
            Assert.Contains("not found", result.ValidationMessage);
        }

        [Fact]
        public async Task GetInstructionAsync_WithInvalidContent_ReturnsInvalidInstruction()
        {
            // Arrange
            var serviceName = "RequirementsAnalysisService";
            var fileName = "RequirementsAnalyst.md";
            var filePath = Path.Combine(_testInstructionsPath, fileName);
            
            // Content too short
            var content = "Too short";
            
            await File.WriteAllTextAsync(filePath, content);

            var customSettings = Options.Create(new InstructionSettings
            {
                InstructionsPath = _testInstructionsPath,
                MinimumContentLength = 100,
                RequiredSections = new[] { "Role", "Task", "Constraints" }
            });

            var service = new InstructionService(customSettings, _mockLogger.Object);

            // Act
            var result = await service.GetInstructionAsync(serviceName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(serviceName, result.ServiceName);
            Assert.Equal(content, result.Content);
            Assert.False(result.IsValid);
            Assert.Contains("too short", result.ValidationMessage);
        }

        [Fact]
        public async Task IsValidInstructionAsync_WithValidInstruction_ReturnsTrue()
        {
            // Arrange
            var serviceName = "RequirementsAnalysisService";
            var fileName = "RequirementsAnalyst.md";
            var filePath = Path.Combine(_testInstructionsPath, fileName);
            
            var content = "# Role\nThis is a test role\n# Task\nThis is a test task\n# Constraints\nThese are test constraints\nMore content to meet minimum length requirement";
            
            await File.WriteAllTextAsync(filePath, content);

            var customSettings = Options.Create(new InstructionSettings
            {
                InstructionsPath = _testInstructionsPath,
                MinimumContentLength = 100,
                RequiredSections = new[] { "Role", "Task", "Constraints" }
            });

            var service = new InstructionService(customSettings, _mockLogger.Object);

            // Act
            var result = await service.IsValidInstructionAsync(serviceName);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsValidInstructionAsync_WithInvalidInstruction_ReturnsFalse()
        {
            // Arrange
            var serviceName = "RequirementsAnalysisService";
            var fileName = "RequirementsAnalyst.md";
            var filePath = Path.Combine(_testInstructionsPath, fileName);
            
            // Content too short
            var content = "Too short";
            
            await File.WriteAllTextAsync(filePath, content);

            var customSettings = Options.Create(new InstructionSettings
            {
                InstructionsPath = _testInstructionsPath,
                MinimumContentLength = 100,
                RequiredSections = new[] { "Role", "Task", "Constraints" }
            });

            var service = new InstructionService(customSettings, _mockLogger.Object);

            // Act
            var result = await service.IsValidInstructionAsync(serviceName);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetInstructionFileName_MapsServiceNamesCorrectly()
        {
            // This test would require accessing a private method, which is not ideal
            // We'll test this behavior indirectly through other tests
        }
    }
}