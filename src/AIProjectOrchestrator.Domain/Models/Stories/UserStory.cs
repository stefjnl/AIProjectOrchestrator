using System;
using System.Collections.Generic;
using AIProjectOrchestrator.Domain.Entities;

namespace AIProjectOrchestrator.Domain.Models.Stories
{
    public enum StoryStatus
    {
        Draft,
        Approved,
        Rejected
    }

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
        public StoryStatus Status { get; set; } = StoryStatus.Draft;
    }
}
