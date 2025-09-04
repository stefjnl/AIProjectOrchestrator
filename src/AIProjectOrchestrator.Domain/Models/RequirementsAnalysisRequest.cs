using System.ComponentModel.DataAnnotations;

namespace AIProjectOrchestrator.Domain.Models
{
    public class RequirementsAnalysisRequest
    {
        [Required]
        [MinLength(10, ErrorMessage = "Project description must be at least 10 characters long")]
        public string ProjectDescription { get; set; } = string.Empty;
        
        public string? AdditionalContext { get; set; }
        
        public string? Constraints { get; set; }
        
        public string? ProjectId { get; set; } // Added to track project ID for workflow correlation
    }
}