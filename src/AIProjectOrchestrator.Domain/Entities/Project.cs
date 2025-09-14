namespace AIProjectOrchestrator.Domain.Entities
{
    public class Project
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        
        // New fields to support frontend requirements
        public string Status { get; set; } = "active";
        public string Type { get; set; } = "web";
        
        // Property for frontend compatibility (maps to CreatedDate)
        public DateTime CreatedAt => CreatedDate;
        
        // Navigation properties
        public ICollection<RequirementsAnalysis> RequirementsAnalyses { get; set; } = new List<RequirementsAnalysis>();
    }
}