using AIProjectOrchestrator.Domain.Models.Review;

namespace AIProjectOrchestrator.Domain.Entities
{
    public class Review
    {
        public int Id { get; set; }
        public Guid ReviewId { get; set; } // Match the Guid from ReviewSubmission
        public string Content { get; set; } = string.Empty;
        public ReviewStatus Status { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string PipelineStage { get; set; } = string.Empty;
        public string Feedback { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        
        // Foreign key properties for one-to-one relationships
        public int? RequirementsAnalysisId { get; set; }
        public int? ProjectPlanningId { get; set; }
        public int? StoryGenerationId { get; set; }
        public int? PromptGenerationId { get; set; }
        
        // Navigation properties - one-to-one relationships with workflow entities
        public RequirementsAnalysis? RequirementsAnalysis { get; set; }
        public ProjectPlanning? ProjectPlanning { get; set; }
        public StoryGeneration? StoryGeneration { get; set; }
        public PromptGeneration? PromptGeneration { get; set; }
    }
}
