using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using AIProjectOrchestrator.Domain.Models.Stories;

namespace AIProjectOrchestrator.Domain.Models.Stories
{
    public class UpdateStoryDto
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string Description { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Acceptance criteria cannot exceed 1000 characters total")]
        public List<string> AcceptanceCriteria { get; set; } = new();

        [StringLength(50)]
        public string? Priority { get; set; }

        public int? StoryPoints { get; set; }

        public List<string> Tags { get; set; } = new();

        [StringLength(100)]
        public string? EstimatedComplexity { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public StoryStatus Status { get; set; } = StoryStatus.Draft;
    }
}
