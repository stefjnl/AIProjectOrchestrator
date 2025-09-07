using System;
using System.Collections.Generic;
using System.Linq;
using AIProjectOrchestrator.Domain.Models.Stories;
using AIProjectOrchestrator.Domain.Entities;

namespace AIProjectOrchestrator.Application.Services;

public class ContextOptimizer
{
    public PromptContext OptimizeContext(PromptContext context)
    {
        // Summarize lengthy project planning content
        var summarizedArchitecture = SummarizeArchitecture(context.ProjectArchitecture);

        // Limit related stories to most relevant ones
        var prioritizedStories = PrioritizeRelatedStories(context.RelatedStories, context.TargetStory);

        // Compress technical preferences if too long (unlikely)
        var compressedPreferences = context.TechnicalPreferences.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Length > 100 ? kvp.Value.Substring(0, 100) + "..." : kvp.Value);

        // Update integration guidance with prioritized stories
        var updatedGuidance = $"Integrate with prioritized stories: {string.Join(", ", prioritizedStories.Select(s => s.Title))}";

        // Target ~40% of AI context window (estimate 8000-10000 characters total)
        var totalContext = CalculateTotalContextSize(summarizedArchitecture, prioritizedStories, compressedPreferences);
        if (totalContext > 10000)
        {
            // Further truncate if needed
            summarizedArchitecture = summarizedArchitecture.Substring(0, Math.Min(2000, summarizedArchitecture.Length));
        }

        return context with
        {
            ProjectArchitecture = summarizedArchitecture,
            RelatedStories = prioritizedStories,
            TechnicalPreferences = compressedPreferences,
            IntegrationGuidance = updatedGuidance
        };
    }

    private string SummarizeArchitecture(string architecture)
    {
        // Simple summarization: Take first 1500 chars and ensure it ends on sentence
        if (architecture.Length <= 1500) return architecture;

        var summary = architecture.Substring(0, 1500);
        var lastPeriod = summary.LastIndexOf('.');
        if (lastPeriod > 1000) summary = summary.Substring(0, lastPeriod + 1);
        return summary + "... (summarized for context optimization)";
    }

    private List<UserStory> PrioritizeRelatedStories(List<UserStory> stories, UserStory target)
    {
        // Select most relevant stories based on tags, titles, dependencies
        // Simple keyword overlap from target description and acceptance criteria
        var targetKeywords = GetKeywordsFromStory(target);
        var scoredStories = stories.Select(story => new
        {
            Story = story,
            Score = CalculateKeywordOverlap(targetKeywords, GetKeywordsFromStory(story))
        }).OrderByDescending(s => s.Score).Take(3).Select(s => s.Story).ToList();

        return scoredStories;
    }

    private HashSet<string> GetKeywordsFromStory(UserStory story)
    {
        var text = story.Description + " " + string.Join(" ", story.AcceptanceCriteria) + " " + string.Join(" ", story.Tags);
        // Simple keyword extraction: words longer than 3 chars, lowercase
        return new HashSet<string>(text.Split(new char[] { ' ', '.', ',', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(word => word.Length > 3)
            .Select(word => word.ToLowerInvariant()));
    }

    private int CalculateKeywordOverlap(HashSet<string> targetKeywords, HashSet<string> storyKeywords)
    {
        return targetKeywords.Intersect(storyKeywords).Count();
    }

    private int CalculateTotalContextSize(string architecture, List<UserStory> stories, Dictionary<string, string> preferences)
    {
        var size = architecture.Length;
        size += stories.Sum(s => s.Title.Length + s.Description.Length + string.Join("", s.AcceptanceCriteria).Length);
        size += preferences.Sum(p => p.Key.Length + p.Value.Length);
        return size;
    }

    public int EstimateTokenCount(string content)
    {
        // Rough token estimation for context management: approx 4 chars per token
        return (int)Math.Ceiling(content.Length / 4.0);
    }
}
