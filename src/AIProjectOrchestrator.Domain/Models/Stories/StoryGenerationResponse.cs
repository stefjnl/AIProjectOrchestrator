using System;
using System.Collections.Generic;

namespace AIProjectOrchestrator.Domain.Models.Stories
{
    public class StoryGenerationResponse
    {
        public Guid GenerationId { get; set; }
        public List<UserStory> Stories { get; set; } = new();
        public Guid ReviewId { get; set; }
        public StoryGenerationStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ProcessingNotes { get; set; }
    }
}