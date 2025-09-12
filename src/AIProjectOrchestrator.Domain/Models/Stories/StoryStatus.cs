namespace AIProjectOrchestrator.Domain.Models.Stories
{
    public enum StoryStatus
    {
        Draft = 0,
        ReadyForReview,
        Approved,
        Rejected,
        Archived
    }
}