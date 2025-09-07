using System.Collections.Generic;
using AIProjectOrchestrator.Domain.Entities;

namespace AIProjectOrchestrator.Domain.Models;

public class ComprehensiveContext
{
    public List<UserStory> Stories { get; set; } = new();
    public string TechnicalContext { get; set; } = string.Empty;
    public string BusinessContext { get; set; } = string.Empty;
    public int EstimatedTokens { get; set; }
}