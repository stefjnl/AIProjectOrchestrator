using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AIProjectOrchestrator.Domain.Models.PromptGeneration
{
    public class PromptGenerationRequest
    {
        [Required]
        public Guid StoryId { get; set; }
        
        public Dictionary<string, string> TechnicalPreferences { get; set; } = new Dictionary<string, string>();
        
        public string? PromptStyle { get; set; }
    }
}