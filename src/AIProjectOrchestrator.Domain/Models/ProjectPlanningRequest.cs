using System;
using System.ComponentModel.DataAnnotations;

namespace AIProjectOrchestrator.Domain.Models
{
    public class ProjectPlanningRequest
    {
        [Required]
        public Guid RequirementsAnalysisId { get; set; }
        
        public string? PlanningPreferences { get; set; }
        
        public string? TechnicalConstraints { get; set; }
        
        public string? TimelineConstraints { get; set; }
    }
}