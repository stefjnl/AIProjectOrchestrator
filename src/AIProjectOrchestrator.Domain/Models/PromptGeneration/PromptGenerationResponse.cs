using System;

namespace AIProjectOrchestrator.Domain.Models.PromptGeneration
{
    public class PromptGenerationResponse
    {
        public Guid PromptId { get; set; }
        public string GeneratedPrompt { get; set; } = string.Empty;
        public Guid ReviewId { get; set; }
        public PromptGenerationStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}