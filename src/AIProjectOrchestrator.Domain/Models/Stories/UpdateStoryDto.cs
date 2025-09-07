using System.Collections.Generic;
using AIProjectOrchestrator.Domain.Entities;

namespace AIProjectOrchestrator.Domain.Models.Stories
{
    public class UpdateStoryDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public List<string> AcceptanceCriteria { get; set; }
        public string Priority { get; set; }
        public int? StoryPoints { get; set; }
        public List<string> Tags { get; set; }
        public string EstimatedComplexity { get; set; }
        public StoryStatus Status { get; set; }
    }
}