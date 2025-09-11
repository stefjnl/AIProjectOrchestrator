using System;
using System.Collections.Generic;
using AIProjectOrchestrator.Domain.Models.AI;

namespace AIProjectOrchestrator.Domain.Models.Review
{
    public class ReviewSubmission
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ServiceName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
        public string PipelineStage { get; set; } = string.Empty;
        public ReviewStatus Status { get; set; } = ReviewStatus.Pending;
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }
        public AIRequest? OriginalRequest { get; set; }
        public AIResponse? AIResponse { get; set; }
        public ReviewDecision? Decision { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public int? ProjectId { get; set; }
    }

    public class ReviewDecision
    {
        public ReviewStatus Status { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Feedback { get; set; } = string.Empty;
        public DateTime DecidedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, string> InstructionImprovements { get; set; } = new();
    }

    public enum ReviewStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2,
        Expired = 3,
        Failed = 4
    }
}