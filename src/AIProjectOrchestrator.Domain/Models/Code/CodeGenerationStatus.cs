namespace AIProjectOrchestrator.Domain.Models.Code
{
    public enum CodeGenerationStatus
    {
        Processing,
        SelectingModel,
        GeneratingTests,
        GeneratingCode,
        ValidatingOutput,
        PendingReview,
        Approved,
        Rejected,
        Failed,
        StoriesNotApproved
    }
}