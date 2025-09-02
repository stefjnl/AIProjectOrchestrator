using System;

namespace AIProjectOrchestrator.Domain.Models
{
    public class ProjectPlanningResponse
    {
        public Guid PlanningId { get; set; }
        public Guid RequirementsAnalysisId { get; set; }
        public string ProjectRoadmap { get; set; } = string.Empty;
        public string ArchitecturalDecisions { get; set; } = string.Empty;
        public string Milestones { get; set; } = string.Empty;
        public Guid ReviewId { get; set; }
        public ProjectPlanningStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}