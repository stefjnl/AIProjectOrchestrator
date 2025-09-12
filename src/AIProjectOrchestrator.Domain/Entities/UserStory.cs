using System;
using System.Collections.Generic;

namespace AIProjectOrchestrator.Domain.Entities
{
    using AIProjectOrchestrator.Domain.Models.Stories;

    public class UserStory
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int StoryGenerationId { get; set; }
        public StoryGeneration StoryGeneration { get; set; } = null!;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> AcceptanceCriteria { get; set; } = new();
        public string Priority { get; set; } = string.Empty;
        public int? StoryPoints { get; set; }
        public List<string> Tags { get; set; } = new();
        public string? EstimatedComplexity { get; set; }
        public Models.Stories.StoryStatus Status { get; set; } = Models.Stories.StoryStatus.Draft;

        // Prompt generation tracking
        public bool HasPrompt { get; set; } = false;
        public string? PromptId { get; set; }
        
        // Navigation properties
        public List<PromptGeneration> PromptGenerations { get; set; } = new();
    }
}
