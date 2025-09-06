namespace AIProjectOrchestrator.Domain.Models.Stories
{
    public enum StoryGenerationStatus
    {
        NotStarted = 0,
        Processing,
        PendingReview, 
        Approved,
        Rejected,
        Failed,
        RequirementsNotApproved,
        PlanningNotApproved
    }
}
