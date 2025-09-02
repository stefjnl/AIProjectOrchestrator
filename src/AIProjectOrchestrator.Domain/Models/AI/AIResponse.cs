using System;
using System.Collections.Generic;

namespace AIProjectOrchestrator.Domain.Models.AI
{
    public class AIResponse
    {
        public string Content { get; set; } = string.Empty;
        public int TokensUsed { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}