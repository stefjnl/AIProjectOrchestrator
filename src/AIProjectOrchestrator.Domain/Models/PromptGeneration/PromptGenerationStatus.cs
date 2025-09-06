namespace AIProjectOrchestrator.Domain.Models.PromptGeneration
{
    public enum PromptGenerationStatus
    {
        NotStarted = 0,
        Processing,
        PendingReview,
        Approved,
        Rejected,
        Failed
    }
}
