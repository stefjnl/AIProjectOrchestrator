namespace AIProjectOrchestrator.Domain.Models
{
    public enum ProjectPlanningStatus
    {
        NotStarted = 0,
        Processing,
        PendingReview, 
        Approved,
        Rejected,
        Failed,
        RequirementsNotApproved
    }
}
