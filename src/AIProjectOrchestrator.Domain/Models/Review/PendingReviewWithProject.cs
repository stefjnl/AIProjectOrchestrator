using System;
using System.Collections.Generic;
using AIProjectOrchestrator.Domain.Models.Review;

namespace AIProjectOrchestrator.Domain.Models.Review
{
    public class PendingReviewWithProject : ReviewSubmission
    {
        public string ProjectName { get; set; } = string.Empty;
        public string ProjectDescription { get; set; } = string.Empty;
        public string ProjectStage { get; set; } = string.Empty;
        public DateTime ProjectCreatedDate { get; set; }
    }
}