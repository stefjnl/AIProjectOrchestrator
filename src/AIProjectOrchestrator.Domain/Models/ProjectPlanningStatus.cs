namespace AIProjectOrchestrator.Domain.Models
{
    public enum ProjectPlanningStatus
    {
        Processing,
        PendingReview, 
        Approved,
        Rejected,
        Failed,
        RequirementsNotApproved
    }
}