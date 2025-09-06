using AIProjectOrchestrator.Domain.Models;
using AIProjectOrchestrator.Domain.Models.Stories;

namespace AIProjectOrchestrator.Domain.Entities
{
    public class StoryGeneration
    {
        public int Id { get; set; }
        public int ProjectPlanningId { get; set; }
        public string GenerationId { get; set; } = string.Empty; // Preserve existing string IDs
        public StoryGenerationStatus Status { get; set; }
        public string Content { get; set; } = string.Empty;
        public string ReviewId { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string StoriesJson { get; set; } = string.Empty; // JSON serialized stories for persistence
        
        // Navigation properties
        public ProjectPlanning ProjectPlanning { get; set; } = null!;
        public Review? Review { get; set; }
        public ICollection<PromptGeneration> PromptGenerations { get; set; } = new List<PromptGeneration>();
        public ICollection<UserStory> Stories { get; set; } = new List<UserStory>();
    }
}
