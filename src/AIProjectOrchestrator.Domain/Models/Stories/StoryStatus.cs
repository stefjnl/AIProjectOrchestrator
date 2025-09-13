namespace AIProjectOrchestrator.Domain.Models.Stories
{
    public enum StoryStatus
    {
        Draft = 0,      // PENDING
        Approved = 1,   // Match frontend APPROVED
        Rejected = 2    // Match frontend REJECTED
    }
}