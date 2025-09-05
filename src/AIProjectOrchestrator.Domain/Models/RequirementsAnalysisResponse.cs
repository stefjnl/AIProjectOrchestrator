using System;

namespace AIProjectOrchestrator.Domain.Models
{
    public class RequirementsAnalysisResponse
    {
        public Guid AnalysisId { get; set; }
        public string ProjectDescription { get; set; } = string.Empty;
        public string AnalysisResult { get; set; } = string.Empty;
        public Guid ReviewId { get; set; }
        public RequirementsAnalysisStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ProjectId { get; set; } // Add ProjectId to track which project this analysis belongs to
    }
}