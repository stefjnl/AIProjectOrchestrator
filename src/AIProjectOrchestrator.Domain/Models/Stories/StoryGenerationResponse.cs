using System;
using System.Collections.Generic;
using AIProjectOrchestrator.Domain.Entities;

namespace AIProjectOrchestrator.Domain.Models.Stories
{
    public class StoryGenerationResponse
    {
        public Guid GenerationId { get; set; }
        public Guid PlanningId { get; set; }
        public List<UserStory> Stories { get; set; } = new();
        public Guid ReviewId { get; set; }
        public StoryGenerationStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ProcessingNotes { get; set; }
    }
}