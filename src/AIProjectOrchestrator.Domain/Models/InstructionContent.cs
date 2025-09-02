using System;

namespace AIProjectOrchestrator.Domain.Models
{
    public class InstructionContent
    {
        public string ServiceName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }
        public bool IsValid { get; set; }
        public string ValidationMessage { get; set; } = string.Empty;
    }
}