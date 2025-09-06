using AIProjectOrchestrator.Domain.Models;

namespace AIProjectOrchestrator.Domain.Entities
{
    public class ProjectPlanning
    {
        public int Id { get; set; }
        public int RequirementsAnalysisId { get; set; }
        public string PlanningId { get; set; } = string.Empty; // Preserve existing string IDs
        public ProjectPlanningStatus Status { get; set; }
        public string Content { get; set; } = string.Empty;
        public string ReviewId { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        
        // Navigation properties
        public RequirementsAnalysis RequirementsAnalysis { get; set; } = null!;
        public Review? Review { get; set; }
        public ICollection<StoryGeneration> StoryGenerations { get; set; } = new List<StoryGeneration>();
    }
}
