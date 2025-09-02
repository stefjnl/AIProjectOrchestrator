using System.Collections.Generic;

namespace AIProjectOrchestrator.Domain.Configuration
{
    public class ReviewSettings
    {
        public const string SectionName = "ReviewSettings";
        
        public int MaxConcurrentReviews { get; set; } = 100;
        public int ReviewTimeoutHours { get; set; } = 24;
        public int CleanupIntervalMinutes { get; set; } = 60;
        public int MaxContentLength { get; set; } = 50000;
        public List<string> ValidPipelineStages { get; set; } = new() 
        { 
            "Analysis", "Planning", "Stories", "Implementation", "Review" 
        };
    }
}