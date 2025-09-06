using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Application.Interfaces;
using AIProjectOrchestrator.Application.Services;
using AIProjectOrchestrator.Domain.Models.Stories;
using AIProjectOrchestrator.Domain.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AIProjectOrchestrator.UnitTests.Services;

public class PromptContextAssemblerTests
{
    private readonly Mock<IProjectPlanningService> _mockProjectPlanningService;
    private readonly Mock<IStoryGenerationService> _mockStoryGenerationService;
    private readonly Mock<ILogger<PromptContextAssembler>> _mockLogger;
    private readonly PromptContextAssembler _assembler;

    public PromptContextAssemblerTests()
    {
        _mockProjectPlanningService = new Mock<IProjectPlanningService>();
        _mockStoryGenerationService = new Mock<IStoryGenerationService>();
        _mockLogger = new Mock<ILogger<PromptContextAssembler>>();

        _assembler = new PromptContextAssembler(
            _mockProjectPlanningService.Object,
            _mockStoryGenerationService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task AssembleContextAsync_WithValidStory_ReturnsCompleteContext()
    {
        // Arrange
        var storyGenerationId = Guid.NewGuid();
        var storyIndex = 0;
        var stories = new List<UserStory>
        {
            new UserStory { Id = Guid.NewGuid(), Title = "Test Story", Description = "Test Description", AcceptanceCriteria = new List<string> { "AC1" } }
        };
        _mockStoryGenerationService.Setup(s => s.GetGenerationResultsAsync(storyGenerationId, It.IsAny<CancellationToken>())).ReturnsAsync(stories);
        _mockStoryGenerationService.Setup(s => s.GetPlanningIdAsync(storyGenerationId, It.IsAny<CancellationToken>())).ReturnsAsync(Guid.NewGuid());
        _mockProjectPlanningService.Setup(p => p.GetTechnicalContextAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync("Test Architecture");

        // Act
        var context = await _assembler.AssembleContextAsync(storyGenerationId, storyIndex);

        // Assert
        Assert.NotNull(context);
        Assert.Equal(stories[0], context.TargetStory);
        Assert.Contains("Test Architecture", context.ProjectArchitecture);
        _mockStoryGenerationService.Verify(s => s.GetGenerationResultsAsync(storyGenerationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetRelatedStoriesAsync_ReturnsRelevantStories()
    {
        // Arrange
        var storyGenerationId = Guid.NewGuid();
        var currentIndex = 1;
        var allStories = new List<UserStory>
        {
            new UserStory { Title = "Story 0" },
            new UserStory { Title = "Story 1" },
            new UserStory { Title = "Story 2" },
            new UserStory { Title = "Story 3" },
            new UserStory { Title = "Story 4" }
        };
        _mockStoryGenerationService.Setup(s => s.GetGenerationResultsAsync(storyGenerationId, It.IsAny<CancellationToken>())).ReturnsAsync(allStories);

        // Act
        var related = await _assembler.GetRelatedStoriesAsync(storyGenerationId, currentIndex);

        // Assert
        Assert.Equal(4, related.Count); // 0,2,3,4 (skips 1)
        Assert.Contains("Story 0", related.Select(r => r.Title));
        Assert.Contains("Story 2", related.Select(r => r.Title));
        Assert.Contains("Story 3", related.Select(r => r.Title));
        Assert.Contains("Story 4", related.Select(r => r.Title));
    }

    [Fact]
    public async Task GetProjectArchitectureAsync_ReturnsFormattedArchitecture()
    {
        // Arrange
        var planningId = Guid.NewGuid();
        var expectedArchitecture = "Test Architecture";
        _mockProjectPlanningService.Setup(p => p.GetTechnicalContextAsync(planningId, It.IsAny<CancellationToken>())).ReturnsAsync(expectedArchitecture);

        // Act
        var architecture = await _assembler.GetProjectArchitectureAsync(planningId);

        // Assert
        Assert.Contains(expectedArchitecture, architecture);
        Assert.Contains("Technology Stack: .NET 9", architecture);
    }
}
