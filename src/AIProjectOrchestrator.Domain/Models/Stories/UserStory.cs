using System.Collections.Generic;

namespace AIProjectOrchestrator.Domain.Models.Stories
{
    public class UserStory
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> AcceptanceCriteria { get; set; } = new();
        public string Priority { get; set; } = string.Empty;
        public string? EstimatedComplexity { get; set; }
    }
}