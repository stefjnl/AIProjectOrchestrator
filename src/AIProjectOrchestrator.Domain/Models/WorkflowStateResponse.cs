using AIProjectOrchestrator.Domain.Models;
using AIProjectOrchestrator.Domain.Models.Stories;
using AIProjectOrchestrator.Domain.Models.PromptGeneration;

namespace AIProjectOrchestrator.Domain.Models
{
    public class WorkflowStateResponse
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public RequirementsAnalysisState RequirementsAnalysis { get; set; } = new();
        public ProjectPlanningState ProjectPlanning { get; set; } = new();
        public StoryGenerationState StoryGeneration { get; set; } = new();
        public PromptGenerationState PromptGeneration { get; set; } = new();
    }

    public class RequirementsAnalysisState
    {
        public string AnalysisId { get; set; } = string.Empty;
        public RequirementsAnalysisStatus Status { get; set; } = RequirementsAnalysisStatus.NotStarted;
        public string ReviewId { get; set; } = string.Empty;
        public bool IsApproved => Status == RequirementsAnalysisStatus.Approved;
        public bool IsPending => Status == RequirementsAnalysisStatus.PendingReview;
    }

    public class ProjectPlanningState
    {
        public string PlanningId { get; set; } = string.Empty;
        public ProjectPlanningStatus Status { get; set; } = ProjectPlanningStatus.NotStarted;
        public string ReviewId { get; set; } = string.Empty;
        public bool IsApproved => Status == ProjectPlanningStatus.Approved;
        public bool IsPending => Status == ProjectPlanningStatus.PendingReview;
    }

    public class StoryGenerationState
    {
        public string GenerationId { get; set; } = string.Empty;
        public StoryGenerationStatus Status { get; set; } = StoryGenerationStatus.NotStarted;
        public string ReviewId { get; set; } = string.Empty;
        public bool IsApproved => Status == StoryGenerationStatus.Approved;
        public bool IsPending => Status == StoryGenerationStatus.PendingReview;
        public int StoryCount { get; set; }
    }

    public class PromptGenerationState
    {
        public List<StoryPromptState> StoryPrompts { get; set; } = new();
        public int CompletedCount => StoryPrompts.Count(sp => sp.IsApproved);
        public int TotalCount => StoryPrompts.Count;
        public decimal CompletionPercentage => TotalCount > 0 ? (decimal)CompletedCount / TotalCount * 100 : 0;
    }

    public class StoryPromptState
    {
        public int StoryIndex { get; set; }
        public string StoryTitle { get; set; } = string.Empty;
        public string PromptId { get; set; } = string.Empty;
        public PromptGenerationStatus Status { get; set; } = PromptGenerationStatus.NotStarted;
        public string ReviewId { get; set; } = string.Empty;
        public bool IsApproved => Status == PromptGenerationStatus.Approved;
        public bool IsPending => Status == PromptGenerationStatus.PendingReview;
    }
}
