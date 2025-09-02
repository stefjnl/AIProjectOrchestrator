using System;
using System.Collections.Generic;

namespace AIProjectOrchestrator.Domain.Models.Code
{
    public class CodeGenerationResponse
    {
        public Guid GenerationId { get; set; }
        public List<CodeArtifact> GeneratedFiles { get; set; } = new();
        public List<CodeArtifact> TestFiles { get; set; } = new();
        public string CompilationStatus { get; set; } = string.Empty;
        public Guid ReviewId { get; set; }
        public CodeGenerationStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ProcessingNotes { get; set; }
        public string? SelectedModel { get; set; }
    }
}