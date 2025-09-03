using Microsoft.AspNetCore.SignalR;

namespace AIProjectOrchestrator.Web.Hubs;

public class WorkflowHub : Hub
{
    public async Task RequirementsAnalysisStarted(Guid analysisId)
    {
        await Clients.All.SendAsync("RequirementsAnalysisStarted", analysisId);
    }

    public async Task RequirementsAnalysisCompleted(Guid analysisId, string status)
    {
        await Clients.All.SendAsync("RequirementsAnalysisCompleted", analysisId, status);
    }

    public async Task PlanningStarted(Guid planningId)
    {
        await Clients.All.SendAsync("PlanningStarted", planningId);
    }

    public async Task PlanningCompleted(Guid planningId, string status)
    {
        await Clients.All.SendAsync("PlanningCompleted", planningId, status);
    }

    public async Task StoryGenerationStarted(Guid generationId)
    {
        await Clients.All.SendAsync("StoryGenerationStarted", generationId);
    }

    public async Task StoryGenerationCompleted(Guid generationId, string status)
    {
        await Clients.All.SendAsync("StoryGenerationCompleted", generationId, status);
    }
}