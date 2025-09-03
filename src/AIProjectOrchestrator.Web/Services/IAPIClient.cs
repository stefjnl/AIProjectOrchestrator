namespace AIProjectOrchestrator.Web.Services;

public interface IAPIClient
{
    // Project Management
    Task<IEnumerable<Project>> GetProjectsAsync();
    Task<Project> GetProjectAsync(int id);
    Task<Project> CreateProjectAsync(Project project);
    Task<Project> UpdateProjectAsync(int id, Project project);
    Task DeleteProjectAsync(int id);
    
    // AI Orchestration Workflow
    Task<RequirementsAnalysisResponse> AnalyzeRequirementsAsync(RequirementsAnalysisRequest request);
    Task<RequirementsAnalysisStatus> GetRequirementsStatusAsync(Guid analysisId);
    
    Task<ProjectPlanningResponse> CreatePlanAsync(ProjectPlanningRequest request);
    Task<ProjectPlanningStatus> GetPlanningStatusAsync(Guid planningId);
    Task<bool> CanCreatePlanAsync(Guid requirementsAnalysisId);
    
    Task<StoryGenerationResponse> GenerateStoriesAsync(StoryGenerationRequest request);
    Task<StoryGenerationStatus> GetStoryStatusAsync(Guid generationId);
    Task<StoryResults?> GetStoryResultsAsync(Guid generationId);
    Task<bool> CanGenerateStoriesAsync(Guid projectPlanningId);
    
    // Review Management
    Task<ReviewSubmission> GetReviewAsync(Guid reviewId);
    Task<bool> ApproveReviewAsync(Guid reviewId, string? feedback = null);
    Task<bool> RejectReviewAsync(Guid reviewId, string feedback);
    Task<IEnumerable<ReviewSubmission>> GetPendingReviewsAsync();
    
    // System Health
    Task<HealthCheckResult?> GetSystemHealthAsync();
}