using System;
using System.Collections.Generic;

namespace AIProjectOrchestrator.Domain.Models.Code
{
    public class CodeArtifactsResult
    {
        public Guid GenerationId { get; set; }
        public List<CodeArtifact> Artifacts { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
        public string PackageName { get; set; } = string.Empty;
        public long TotalSizeBytes { get; set; }
        public int FileCount { get; set; }
        public Dictionary<string, int> FileTypes { get; set; } = new();
    }
}