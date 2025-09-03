using System.Collections.Generic;
using AIProjectOrchestrator.Domain.Models.Stories;

namespace AIProjectOrchestrator.Web.Services;

public class StoryResults
{
    public List<UserStory> Stories { get; set; } = new();
}