using AIProjectOrchestrator.Domain.Models;

namespace AIProjectOrchestrator.Domain.Entities
{
    public class RequirementsAnalysis
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string AnalysisId { get; set; } = string.Empty; // Preserve existing string IDs
        public RequirementsAnalysisStatus Status { get; set; }
        public string Content { get; set; } = string.Empty;
        public string ReviewId { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        
        // Navigation properties
        public Project Project { get; set; } = null!;
        public Review? Review { get; set; }
        public ICollection<ProjectPlanning> ProjectPlannings { get; set; } = new List<ProjectPlanning>();
    }
}
