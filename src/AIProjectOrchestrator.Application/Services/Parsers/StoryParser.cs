using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Interfaces;
using AIProjectOrchestrator.Domain.Models.Stories;
using Microsoft.Extensions.Logging;

namespace AIProjectOrchestrator.Application.Services.Parsers
{
    public class StoryParser : IStoryParser
    {
        private readonly ILogger<StoryParser> _logger;

        public StoryParser(ILogger<StoryParser> logger)
        {
            _logger = logger;
        }

        public async Task<List<UserStory>> ParseAsync(string aiResponse, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var stories = new List<UserStory>();

            // Simple junior-level parsing - no regex, just string operations
            var lines = aiResponse.Split('\n');
            var currentStory = new UserStory();
            var inStory = false;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();

                // Check for story start
                if (line.StartsWith("#### Story"))
                {
                    if (inStory && !string.IsNullOrEmpty(currentStory.Title))
                    {
                        // Save previous story
                        stories.Add(currentStory);
                        currentStory = new UserStory();
                    }
                    inStory = true;
                    continue;
                }

                if (!inStory) continue;

                // Simple field extraction
                if (line.StartsWith("**Title**:"))
                {
                    currentStory.Title = line.Substring("**Title**:".Length).Trim();
                }
                else if (line.StartsWith("**Description**:"))
                {
                    currentStory.Description = line.Substring("**Description**:".Length).Trim();
                }
                else if (line.StartsWith("**Acceptance Criteria**:"))
                {
                    // Collect acceptance criteria until next field
                    var criteria = new List<string>();
                    for (int j = i + 1; j < lines.Length; j++)
                    {
                        var criteriaLine = lines[j].Trim();
                        if (criteriaLine.StartsWith("**")) break; // Next field
                        if (!string.IsNullOrEmpty(criteriaLine))
                        {
                            criteria.Add(criteriaLine.Replace("- ", "").Replace("• ", "").Trim());
                        }
                    }
                    currentStory.AcceptanceCriteria = criteria;
                }
                else if (line.StartsWith("**Priority**:"))
                {
                    currentStory.Priority = line.Substring("**Priority**:".Length).Trim();
                }
                else if (line.StartsWith("**Estimated Complexity**:"))
                {
                    currentStory.EstimatedComplexity = line.Substring("**Estimated Complexity**:".Length).Trim();
                }
            }

            // Don't forget the last story
            if (inStory && !string.IsNullOrEmpty(currentStory.Title))
            {
                stories.Add(currentStory);
            }

            // Set defaults for missing values
            foreach (var story in stories)
            {
                if (string.IsNullOrEmpty(story.Title))
                    story.Title = "Untitled Story";
                if (string.IsNullOrEmpty(story.Description))
                    story.Description = "";
                if (string.IsNullOrEmpty(story.Priority))
                    story.Priority = "Medium";
                if (story.AcceptanceCriteria == null)
                    story.AcceptanceCriteria = new List<string>();
            }

            _logger.LogInformation("Parsed {StoryCount} stories from AI response", stories.Count);

            return await Task.FromResult(stories);
        }
    }
}
