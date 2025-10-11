namespace AIProjectOrchestrator.Domain.Models;

public class ProjectDetailsDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public ExecutionStatusDto? RequirementsAnalysis { get; set; }
    public ExecutionStatusDto? ProjectPlan { get; set; }
    public ExecutionStatusDto? StoryGeneration { get; set; }
    public ExecutionStatusDto? CodeGeneration { get; set; }
}

public class ExecutionStatusDto
{
    public string Status { get; set; } = string.Empty;
    public int? ExecutionId { get; set; }
}
