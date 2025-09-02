using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AIProjectOrchestrator.Domain.Models.AI;

namespace AIProjectOrchestrator.Domain.Models.Review
{
    public class SubmitReviewRequest
    {
        [Required, MaxLength(100)]
        public string ServiceName { get; set; } = string.Empty;
        
        [Required, MaxLength(50000)]
        public string Content { get; set; } = string.Empty;
        
        [Required]
        public string CorrelationId { get; set; } = string.Empty;
        
        [Required, MaxLength(50)]
        public string PipelineStage { get; set; } = string.Empty;
        
        public AIRequest? OriginalRequest { get; set; }
        public AIResponse? AIResponse { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class ReviewDecisionRequest
    {
        [Required]
        public string Reason { get; set; } = string.Empty;
        
        public string Feedback { get; set; } = string.Empty;
        public Dictionary<string, string> InstructionImprovements { get; set; } = new();
    }

    public class ReviewResponse
    {
        public Guid ReviewId { get; set; }
        public ReviewStatus Status { get; set; }
        public DateTime SubmittedAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}