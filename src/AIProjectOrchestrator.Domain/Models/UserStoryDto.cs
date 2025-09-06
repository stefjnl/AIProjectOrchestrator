using System.Collections.Generic;

namespace AIProjectOrchestrator.Domain.Models
{
    public class UserStoryDto
    {
        public int Index { get; set; }
        public string Title { get; set; } = string.Empty;
        public string AsA { get; set; } = string.Empty;
        public string IWant { get; set; } = string.Empty;
        public string SoThat { get; set; } = string.Empty;
        public List<string> AcceptanceCriteria { get; set; } = new();
        public int? StoryPoints { get; set; }
    }
}
