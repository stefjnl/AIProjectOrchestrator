using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using AIProjectOrchestrator.Application.Interfaces;
using AIProjectOrchestrator.Domain.Models.Stories;
using AIProjectOrchestrator.Domain.Services;

namespace AIProjectOrchestrator.Application.Services;

public record PromptContext(
    UserStory TargetStory,
    string ProjectArchitecture,
    List<UserStory> RelatedStories,
    Dictionary<string, string> TechnicalPreferences,
    string IntegrationGuidance
);

public class PromptContextAssembler
{
    private readonly IProjectPlanningService _projectPlanningService;
    private readonly IStoryGenerationService _storyGenerationService;
    private readonly ILogger<PromptContextAssembler> _logger;

    public PromptContextAssembler(
        IProjectPlanningService projectPlanningService,
        IStoryGenerationService storyGenerationService,
        ILogger<PromptContextAssembler> logger)
    {
        _projectPlanningService = projectPlanningService;
        _storyGenerationService = storyGenerationService;
        _logger = logger;
    }

    public async Task<PromptContext> AssembleContextAsync(Guid storyGenerationId, int storyIndex, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Assembling context for story {StoryIndex} in generation {StoryGenerationId}", storyIndex, storyGenerationId);

        // Retrieve individual story
        var stories = await _storyGenerationService.GetGenerationResultsAsync(storyGenerationId, cancellationToken);
        if (storyIndex < 0 || storyIndex >= stories.Count)
        {
            _logger.LogWarning("Invalid story index {StoryIndex} for generation {StoryGenerationId}", storyIndex, storyGenerationId);
            throw new ArgumentOutOfRangeException(nameof(storyIndex));
        }

        var targetStory = stories[storyIndex];

        var planningId = await _storyGenerationService.GetPlanningIdAsync(storyGenerationId, cancellationToken) ?? Guid.Empty;

        // Get approved project planning context
        var projectArchitecture = await GetProjectArchitectureAsync(planningId, cancellationToken);

        // Get related stories for integration context
        var relatedStories = await GetRelatedStoriesAsync(storyGenerationId, storyIndex, cancellationToken);

        // Derive technical preferences and integration guidance (simplified; in real, from planning)
        var technicalPreferences = new Dictionary<string, string>
        {
            { "Framework", ".NET 9" },
            { "Architecture", "Clean Architecture" },
            { "Database", "PostgreSQL with EF Core" }
        }; // Assume fetched from planning; placeholder

        var integrationGuidance = $"Integrate with related stories: {string.Join(", ", relatedStories.Select(s => s.Title))}";

        return new PromptContext(targetStory, projectArchitecture, relatedStories, technicalPreferences, integrationGuidance);
    }

    public async Task<string> GetProjectArchitectureAsync(Guid planningId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting project architecture for planning {PlanningId}", planningId);

        // Extract architecture decisions from project planning
        var technicalContext = await _projectPlanningService.GetTechnicalContextAsync(planningId, cancellationToken);
        var architecture = technicalContext ?? "Standard Clean Architecture: Domain, Application, Infrastructure, API layers with .NET 9 Web API and PostgreSQL.";
        
        // Format for prompt consumption
        return $"Project Architecture:\n{architecture}\nTechnology Stack: .NET 9, ASP.NET Core, Entity Framework Core.\nIntegration Points: Use dependency injection for services and repositories.";
    }

    public async Task<List<UserStory>> GetRelatedStoriesAsync(Guid storyGenerationId, int currentIndex, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting related stories for index {CurrentIndex} in {StoryGenerationId}", currentIndex, storyGenerationId);

        var allStories = await _storyGenerationService.GetGenerationResultsAsync(storyGenerationId, cancellationToken);
        
        // Limit to essential context: up to 2 before and 2 after, max 4 total
        var related = new List<UserStory>();
        for (int i = Math.Max(0, currentIndex - 2); i <= Math.Min(allStories.Count - 1, currentIndex + 2); i++)
        {
            if (i != currentIndex)
            {
                related.Add(allStories[i]);
            }
        }
        
        // Further limit to manage size
        return related.Take(4).ToList();
    }
}
