namespace AIProjectOrchestrator.Domain.Models.PromptGeneration
{
    public enum PromptGenerationStatus
    {
        Processing,
        PendingReview,
        Approved,
        Rejected,
        Failed
    }
}