using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AIProjectOrchestrator.Domain.Models.Stories
{
    public class StoryGenerationRequest
    {
        [Required]
        public Guid PlanningId { get; set; }
        
        public string? StoryPreferences { get; set; }
        
        public string? ComplexityLevels { get; set; }
        
        public string? AdditionalGuidance { get; set; }
    }
}