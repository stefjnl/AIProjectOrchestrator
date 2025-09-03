using System;
using System.Collections.Generic;

namespace AIProjectOrchestrator.Domain.Models.Review.Dashboard
{
    public class ReviewDashboardData
    {
        public List<PendingReviewItem> PendingReviews { get; set; } = new();
        public List<WorkflowStatusItem> ActiveWorkflows { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }

    public class PendingReviewItem
    {
        public Guid ReviewId { get; set; }
        public string ServiceType { get; set; } = string.Empty; // "Requirements", "Planning", "Stories"
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string OriginalRequest { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
    }

    public class WorkflowStatusItem
    {
        public Guid ProjectId { get; set; }
        public string ProjectTitle { get; set; } = string.Empty;
        public WorkflowStage CurrentStage { get; set; }
        public List<StageStatus> StageStatuses { get; set; } = new();
        public string NextAction { get; set; } = string.Empty;
    }

    public class StageStatus
    {
        public string StageName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // "Pending", "InProgress", "Completed", "Failed"
        public Guid? ReviewId { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public enum WorkflowStage
    {
        RequirementsAnalysis,
        ProjectPlanning,
        StoryGeneration,
        Completed
    }

    public class TestScenarioRequest
    {
        public string ScenarioName { get; set; } = string.Empty;
        public string ProjectDescription { get; set; } = string.Empty;
        public string AdditionalContext { get; set; } = string.Empty;
        public string Constraints { get; set; } = string.Empty;
    }

    public class TestScenarioResponse
    {
        public Guid ProjectId { get; set; }
        public Guid RequirementsAnalysisId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    // This is the class that was referenced in the controller but not defined
    // We'll add it here to avoid compilation errors
    public class WorkflowStatus
    {
        public Guid ProjectId { get; set; }
        public WorkflowStage CurrentStage { get; set; }
        public List<StageStatus> Stages { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }
}