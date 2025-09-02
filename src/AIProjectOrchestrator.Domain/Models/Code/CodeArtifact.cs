using System.Collections.Generic;

namespace AIProjectOrchestrator.Domain.Models.Code
{
    public class CodeArtifact
    {
        public string FileName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty; // "Controller", "Service", "Model", "Test"
        public string RelativePath { get; set; } = string.Empty;
        public bool CompilationValid { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
    }
}