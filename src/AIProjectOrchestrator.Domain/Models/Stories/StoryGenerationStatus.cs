namespace AIProjectOrchestrator.Domain.Models.Stories
{
    public enum StoryGenerationStatus
    {
        Processing,
        PendingReview, 
        Approved,
        Rejected,
        Failed,
        RequirementsNotApproved,
        PlanningNotApproved
    }
}