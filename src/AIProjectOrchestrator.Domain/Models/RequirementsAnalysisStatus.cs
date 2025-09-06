namespace AIProjectOrchestrator.Domain.Models
{
    public enum RequirementsAnalysisStatus
    {
        NotStarted = 0,
        Processing,
        PendingReview,
        Approved,
        Rejected,
        Failed
    }
}
