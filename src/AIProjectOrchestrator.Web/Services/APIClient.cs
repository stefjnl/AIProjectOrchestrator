using System.Text;
using System.Text.Json;

namespace AIProjectOrchestrator.Web.Services;

public class APIClient : IAPIClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<APIClient> _logger;

    public APIClient(HttpClient httpClient, ILogger<APIClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    // Project Management
    public async Task<IEnumerable<Project>> GetProjectsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/projects");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var projects = JsonSerializer.Deserialize<IEnumerable<Project>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return projects ?? new List<Project>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching projects");
            return new List<Project>();
        }
    }

    public async Task<Project> GetProjectAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/projects/{id}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var project = JsonSerializer.Deserialize<Project>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return project ?? throw new InvalidOperationException("Project not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching project {ProjectId}", id);
            throw;
        }
    }

    public async Task<Project> CreateProjectAsync(Project project)
    {
        try
        {
            var json = JsonSerializer.Serialize(project);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/projects", content);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            var createdProject = JsonSerializer.Deserialize<Project>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return createdProject ?? throw new InvalidOperationException("Failed to create project");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating project");
            throw;
        }
    }

    public async Task<Project> UpdateProjectAsync(int id, Project project)
    {
        try
        {
            var json = JsonSerializer.Serialize(project);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"/api/projects/{id}", content);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            var updatedProject = JsonSerializer.Deserialize<Project>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return updatedProject ?? throw new InvalidOperationException("Failed to update project");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating project {ProjectId}", id);
            throw;
        }
    }

    public async Task DeleteProjectAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/projects/{id}");
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting project {ProjectId}", id);
            throw;
        }
    }

    // AI Orchestration Workflow
    public async Task<RequirementsAnalysisResponse> AnalyzeRequirementsAsync(RequirementsAnalysisRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/requirements/analyze", content);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<RequirementsAnalysisResponse>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return result ?? throw new InvalidOperationException("Failed to analyze requirements");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing requirements");
            throw;
        }
    }

    public async Task<RequirementsAnalysisStatus> GetRequirementsStatusAsync(Guid analysisId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/requirements/{analysisId}/status");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var status = JsonSerializer.Deserialize<RequirementsAnalysisStatus>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching requirements status for {AnalysisId}", analysisId);
            throw;
        }
    }

    public async Task<ProjectPlanningResponse> CreatePlanAsync(ProjectPlanningRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/planning/create", content);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ProjectPlanningResponse>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return result ?? throw new InvalidOperationException("Failed to create plan");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating project plan");
            throw;
        }
    }

    public async Task<ProjectPlanningStatus> GetPlanningStatusAsync(Guid planningId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/planning/{planningId}/status");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var status = JsonSerializer.Deserialize<ProjectPlanningStatus>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching planning status for {PlanningId}", planningId);
            throw;
        }
    }

    public async Task<bool> CanCreatePlanAsync(Guid requirementsAnalysisId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/planning/can-create/{requirementsAnalysisId}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<bool>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if plan can be created for {RequirementsAnalysisId}", requirementsAnalysisId);
            throw;
        }
    }

    public async Task<StoryGenerationResponse> GenerateStoriesAsync(StoryGenerationRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/stories/generate", content);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<StoryGenerationResponse>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return result ?? throw new InvalidOperationException("Failed to generate stories");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating stories");
            throw;
        }
    }

    public async Task<StoryGenerationStatus> GetStoryStatusAsync(Guid generationId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/stories/{generationId}/status");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var status = JsonSerializer.Deserialize<StoryGenerationStatus>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching story status for {GenerationId}", generationId);
            throw;
        }
    }

    public async Task<StoryResults?> GetStoryResultsAsync(Guid generationId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/stories/{generationId}/results");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<StoryResults>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching story results for {GenerationId}", generationId);
            throw;
        }
    }

    public async Task<bool> CanGenerateStoriesAsync(Guid projectPlanningId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/stories/can-generate/{projectPlanningId}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<bool>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if stories can be generated for {ProjectPlanningId}", projectPlanningId);
            throw;
        }
    }

    // Review Management
    public async Task<ReviewSubmission> GetReviewAsync(Guid reviewId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/review/{reviewId}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var review = JsonSerializer.Deserialize<ReviewSubmission>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return review ?? throw new InvalidOperationException("Review not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching review {ReviewId}", reviewId);
            throw;
        }
    }

    public async Task<bool> ApproveReviewAsync(Guid reviewId, string? feedback = null)
    {
        try
        {
            var request = new { Feedback = feedback ?? string.Empty };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"/api/review/{reviewId}/approve", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving review {ReviewId}", reviewId);
            throw;
        }
    }

    public async Task<bool> RejectReviewAsync(Guid reviewId, string feedback)
    {
        try
        {
            var request = new { Feedback = feedback };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"/api/review/{reviewId}/reject", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting review {ReviewId}", reviewId);
            throw;
        }
    }

    public async Task<IEnumerable<ReviewSubmission>> GetPendingReviewsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/review/pending");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var reviews = JsonSerializer.Deserialize<IEnumerable<ReviewSubmission>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return reviews ?? Enumerable.Empty<ReviewSubmission>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching pending reviews");
            throw;
        }
    }

    // System Health
    public async Task<HealthCheckResult?> GetSystemHealthAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/health");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var health = JsonSerializer.Deserialize<HealthCheckResult>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return health;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching system health");
            throw;
        }
    }
}