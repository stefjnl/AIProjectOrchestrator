using AIProjectOrchestrator.Domain.Models;
using AIProjectOrchestrator.Domain.Models.PromptGeneration;

namespace AIProjectOrchestrator.Domain.Entities
{
    public class PromptGeneration
    {
        public int Id { get; set; }
        public int StoryGenerationId { get; set; }
        public int StoryIndex { get; set; }
        public string PromptId { get; set; } = string.Empty; // Preserve existing string IDs
        public PromptGenerationStatus Status { get; set; }
        public string Content { get; set; } = string.Empty;
        public string ReviewId { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        
        // Navigation properties
        public StoryGeneration StoryGeneration { get; set; } = null!;
        public Review? Review { get; set; }
    }
}
