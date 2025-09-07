using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AIProjectOrchestrator.Application.Interfaces;
using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Models;
using AIProjectOrchestrator.Domain.Services;

namespace AIProjectOrchestrator.Application.Services;

public class ContextRetriever : IContextRetriever
{
    private readonly IStoryGenerationService _storyGenerationService;
    private readonly IProjectPlanningService _projectPlanningService;
    private readonly IRequirementsAnalysisService _requirementsAnalysisService;
    private readonly ILogger<ContextRetriever> _logger;

    public ContextRetriever(
        IStoryGenerationService storyGenerationService,
        IProjectPlanningService projectPlanningService,
        IRequirementsAnalysisService requirementsAnalysisService,
        ILogger<ContextRetriever> logger)
    {
        _storyGenerationService = storyGenerationService;
        _projectPlanningService = projectPlanningService;
        _requirementsAnalysisService = requirementsAnalysisService;
        _logger = logger;
    }

    public async Task<ComprehensiveContext> RetrieveComprehensiveContextAsync(Guid storyGenerationId, CancellationToken cancellationToken = default)
    {
        // Get user stories
        var stories = await _storyGenerationService.GetApprovedStoriesAsync(storyGenerationId, cancellationToken) ?? new List<UserStory>();

        // Get the planning ID from the story generation
        var planningId = await _storyGenerationService.GetPlanningIdAsync(storyGenerationId, cancellationToken);

        string technicalContext = string.Empty;
        string businessContext = string.Empty;

        if (planningId.HasValue)
        {
            // Get planning context (architectural decisions, technical constraints)
            technicalContext = await _projectPlanningService.GetTechnicalContextAsync(planningId.Value, cancellationToken) ?? string.Empty;

            // Get the requirements analysis ID from the planning
            var requirementsAnalysisId = await _projectPlanningService.GetRequirementsAnalysisIdAsync(
                planningId.Value, cancellationToken);

            if (requirementsAnalysisId.HasValue)
            {
                // Get requirements context (business rules)
                businessContext = await _requirementsAnalysisService.GetBusinessContextAsync(
                    requirementsAnalysisId.Value, cancellationToken) ?? string.Empty;
            }
        }

        // Monitor total context size
        var totalContextSize = Encoding.UTF8.GetByteCount(
            string.Join("", stories.Select(s => s.Title + s.Description)) +
            technicalContext +
            businessContext);

        _logger.LogInformation("Comprehensive context size: {TokenCount} bytes", totalContextSize);

        // Apply token optimization if context is too large
        if (totalContextSize > 150000) // Roughly 37.5K tokens
        {
            _logger.LogWarning("Context size is large ({TokenCount} bytes), applying optimization", totalContextSize);
            stories = OptimizeStoriesContext(stories);
            technicalContext = OptimizeTechnicalContext(technicalContext);
            businessContext = OptimizeBusinessContext(businessContext);

            // Recalculate size after optimization
            totalContextSize = Encoding.UTF8.GetByteCount(
                string.Join("", stories.Select(s => s.Title + s.Description)) +
                technicalContext +
                businessContext);

            _logger.LogInformation("Context size after optimization: {TokenCount} bytes", totalContextSize);
        }

        return new ComprehensiveContext
        {
            Stories = stories,
            TechnicalContext = technicalContext,
            BusinessContext = businessContext,
            EstimatedTokens = totalContextSize / 4 // Rough approximation of tokens
        };
    }

    public List<UserStory> OptimizeStoriesContext(List<UserStory> stories)
    {
        // Filter and prioritize stories based on relevance
        // For now, we'll just truncate descriptions if they're too long
        var optimizedStories = new List<UserStory>();

        foreach (var story in stories)
        {
            var optimizedStory = new UserStory
            {
                Title = story.Title,
                Description = story.Description.Length > 500 ? story.Description.Substring(0, 500) + "..." : story.Description,
                AcceptanceCriteria = story.AcceptanceCriteria.Take(5).ToList(), // Limit to 5 criteria
                Priority = story.Priority,
                EstimatedComplexity = story.EstimatedComplexity
            };

            optimizedStories.Add(optimizedStory);
        }

        return optimizedStories;
    }

    public string OptimizeTechnicalContext(string technicalContext)
    {
        // Compress technical context by removing redundant information
        if (string.IsNullOrEmpty(technicalContext))
            return technicalContext;

        // For now, we'll just truncate if it's too long
        if (technicalContext.Length > 2000)
        {
            return technicalContext.Substring(0, 2000) + "...";
        }

        return technicalContext;
    }

    public string OptimizeBusinessContext(string businessContext)
    {
        // Compress business context by removing redundant information
        if (string.IsNullOrEmpty(businessContext))
            return businessContext;

        // For now, we'll just truncate if it's too long
        if (businessContext.Length > 2000)
        {
            return businessContext.Substring(0, 2000) + "...";
        }

        return businessContext;
    }
}