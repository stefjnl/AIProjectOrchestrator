using System;
using System.Collections.Generic;

namespace AIProjectOrchestrator.Domain.Models.Code
{
    public class CodeGenerationRequest
    {
        public Guid StoryGenerationId { get; set; }
        public string? TechnicalPreferences { get; set; }
        public string? TargetFramework { get; set; } = ".NET 9";
        public string? CodeStylePreferences { get; set; }
        public string? AdditionalInstructions { get; set; }
    }
}