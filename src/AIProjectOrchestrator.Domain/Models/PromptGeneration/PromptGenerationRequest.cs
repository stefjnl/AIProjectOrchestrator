using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AIProjectOrchestrator.Domain.Models.PromptGeneration
{
    public class PromptGenerationRequest
    {
        [Required]
        public Guid StoryGenerationId { get; set; }
        
        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Story index must be a non-negative integer")]
        public int StoryIndex { get; set; }
        
        public Dictionary<string, string> TechnicalPreferences { get; set; } = new Dictionary<string, string>();
        
        public string? PromptStyle { get; set; }
    }
}