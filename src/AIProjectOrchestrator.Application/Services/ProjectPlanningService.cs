using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AIProjectOrchestrator.Domain.Services;
using AIProjectOrchestrator.Domain.Models;
using AIProjectOrchestrator.Domain.Models.AI;
using AIProjectOrchestrator.Domain.Models.Review;
using AIProjectOrchestrator.Infrastructure.AI;
using AIProjectOrchestrator.Infrastructure.AI.Providers;
using System.Text;
using AIProjectOrchestrator.Domain.Interfaces;
using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Configuration;
using AIProjectOrchestrator.Domain.Common;
using AIProjectOrchestrator.Infrastructure.Data;

namespace AIProjectOrchestrator.Application.Services
{
    public class ProjectPlanningService : IProjectPlanningService
    {
        private readonly IRequirementsAnalysisService _requirementsAnalysisService;
        private readonly IInstructionService _instructionService;
        private readonly IPlanningAIProvider _planningAIProvider;
        private readonly Lazy<IReviewService> _reviewService;
        private readonly ILogger<ProjectPlanningService> _logger;
        private readonly IProjectPlanningRepository _projectPlanningRepository;
        private readonly IRequirementsAnalysisRepository _requirementsAnalysisRepository;
        private readonly IOptions<ReviewSettings> _reviewSettings;
        private readonly AppDbContext _dbContext;

        public ProjectPlanningService(
            IRequirementsAnalysisService requirementsAnalysisService,
            IInstructionService instructionService,
            IPlanningAIProvider planningAIProvider,
            Lazy<IReviewService> reviewService,
            ILogger<ProjectPlanningService> logger,
            IProjectPlanningRepository projectPlanningRepository,
            IRequirementsAnalysisRepository requirementsAnalysisRepository,
            IOptions<ReviewSettings> reviewSettings,
            AppDbContext dbContext)
        {
            _requirementsAnalysisService = requirementsAnalysisService;
            _instructionService = instructionService;
            _planningAIProvider = planningAIProvider;
            _reviewService = reviewService;
            _logger = logger;
            _projectPlanningRepository = projectPlanningRepository;
            _requirementsAnalysisRepository = requirementsAnalysisRepository;
            _reviewSettings = reviewSettings;
            _dbContext = dbContext;
        }

        public async Task<ProjectPlanningResponse> CreateProjectPlanAsync(
            ProjectPlanningRequest request,
            CancellationToken cancellationToken = default)
        {
            var planningId = Guid.NewGuid();
            _logger.LogInformation("Starting project planning {PlanningId} for requirements analysis: {RequirementsAnalysisId}",
                planningId, request.RequirementsAnalysisId);

            try
            {
                // Validate input
                if (request.RequirementsAnalysisId == Guid.Empty)
                {
                    _logger.LogWarning("Project planning {PlanningId} failed: Requirements analysis ID is required", planningId);
                    throw new ArgumentException("Requirements analysis ID is required");
                }

                // Validate that requirements analysis exists and is approved
                var canCreatePlan = await CanCreatePlanAsync(request.RequirementsAnalysisId, cancellationToken).ConfigureAwait(false);
                if (!canCreatePlan)
                {
                    _logger.LogWarning("Project planning {PlanningId} failed: Requirements analysis is not approved or does not exist", planningId);
                    throw new InvalidOperationException("Requirements analysis is not approved or does not exist");
                }

                // Retrieve the approved requirements analysis results
                _logger.LogDebug("Retrieving requirements analysis results for planning {PlanningId}", planningId);
                var requirementsAnalysis = await _requirementsAnalysisService.GetAnalysisResultsAsync(
                    request.RequirementsAnalysisId, cancellationToken).ConfigureAwait(false);

                if (requirementsAnalysis == null)
                {
                    _logger.LogError("Project planning {PlanningId} failed: Requirements analysis not found", planningId);
                    throw new InvalidOperationException("Requirements analysis not found");
                }

                // Load planning instructions
                _logger.LogDebug("Loading instructions for project planning {PlanningId}", planningId);
                var instructionContent = await _instructionService.GetInstructionAsync("ProjectPlanner", cancellationToken).ConfigureAwait(false);

                if (!instructionContent.IsValid)
                {
                    _logger.LogError("Project planning {PlanningId} failed: Invalid instruction content - {ValidationMessage}",
                        planningId, instructionContent.ValidationMessage);
                    throw new InvalidOperationException($"Failed to load valid instructions: {instructionContent.ValidationMessage}");
                }

                // Create AI request with combined context
                var aiRequest = new AIRequest
                {
                    SystemMessage = instructionContent.Content,
                    Prompt = CreatePromptFromContext(requirementsAnalysis, request),
                    ModelName = string.Empty, // Will be set by the provider
                    Temperature = 0.7, // Default value, will be overridden by provider
                    MaxTokens = 1000  // Default value, will be overridden by provider
                };

                // Log context size metrics
                var contextSize = Encoding.UTF8.GetByteCount(aiRequest.SystemMessage + aiRequest.Prompt);
                _logger.LogInformation("Project planning {PlanningId} context size: {ContextSize} bytes", planningId, contextSize);

                // Warn if context size is approaching limits
                if (contextSize > AIConstants.MaxContextSizeBytes) // Roughly 25K tokens
                {
                    _logger.LogWarning("Project planning {PlanningId} context size is large: {ContextSize} bytes", planningId, contextSize);
                }

                _logger.LogDebug("Calling AI provider for project planning {PlanningId}", planningId);

                // Parallelize metadata save with AI call
                var metadataTask = SaveMetadataAsync(request, planningId, cancellationToken);

                // Call AI using the planning-specific provider
                var generatedContent = await _planningAIProvider.GenerateContentAsync(aiRequest.Prompt, aiRequest.SystemMessage).ConfigureAwait(false);
                
                // GenerateContentAsync returns the content directly, so we need to create an AIResponse
                var aiResponse = new AIResponse
                {
                    Content = generatedContent,
                    TokensUsed = 0, // We don't have token info from GenerateContentAsync
                    ProviderName = _planningAIProvider.ProviderName,
                    IsSuccess = true,
                    ErrorMessage = null
                };

                // Validate AI response content before persistence
                if (string.IsNullOrWhiteSpace(aiResponse.Content))
                {
                    _logger.LogError("AI response content is null or empty for planning {PlanningId}", planningId);
                    throw new InvalidOperationException("AI response cannot be null or empty");
                }

                var maxLen = _reviewSettings.Value?.MaxContentLength ?? int.MaxValue;
                if (aiResponse.Content.Length > maxLen)
                {
                    _logger.LogError("AI response length {Length} exceeds maximum {Max} for planning {PlanningId}", aiResponse.Content.Length, maxLen, planningId);
                    throw new InvalidOperationException($"AI response exceeds maximum length of {maxLen}");
                }

                // Await metadata save completion
                await metadataTask;

                if (!aiResponse.IsSuccess)
                {
                    _logger.LogError("Project planning {PlanningId} failed: AI call failed - {ErrorMessage}",
                        planningId, aiResponse.ErrorMessage);
                    throw new InvalidOperationException($"AI call failed: {aiResponse.ErrorMessage}");
                }

                // Parse AI response into structured components
                var parsedResponse = ParseAIResponse(aiResponse.Content);

                // Create and store the project planning entity first to get the entity ID
                var requirementsAnalysisEntityId = await GetRequirementsAnalysisEntityId(request.RequirementsAnalysisId, cancellationToken);
                var projectPlanningEntity = new ProjectPlanning
                {
                    PlanningId = planningId.ToString(),
                    RequirementsAnalysisId = requirementsAnalysisEntityId.Value, // Safe cast since we throw if null
                    Status = ProjectPlanningStatus.PendingReview,
                    Content = aiResponse.Content,
                    ReviewId = string.Empty, // Will be updated after review submission
                    CreatedDate = DateTime.UtcNow
                };

                // Persist and review within a transaction for consistency
                using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    await _projectPlanningRepository.AddAsync(projectPlanningEntity, cancellationToken).ConfigureAwait(false);
                    var savedPlanningId = projectPlanningEntity.Id; // Get the database-generated int ID

                    // Submit for review
                    _logger.LogDebug("Submitting AI response for review in project planning {PlanningId}", planningId);
                    var correlationId = Guid.NewGuid().ToString();
                    // Get project ID from requirements analysis if available
                    string projectId = "unknown";
                    if (requirementsAnalysis != null)
                    {
                        projectId = requirementsAnalysis.ProjectId ?? "unknown";
                    }

                    var reviewRequest = new SubmitReviewRequest
                    {
                        ServiceName = "ProjectPlanning",
                        Content = aiResponse.Content,
                        CorrelationId = correlationId,
                        PipelineStage = "Planning",
                        OriginalRequest = aiRequest,
                        AIResponse = aiResponse,
                        Metadata = new System.Collections.Generic.Dictionary<string, object>
                        {
                            { "PlanningId", planningId },
                            { "EntityId", savedPlanningId }, // Pass the entity int ID for FK linking
                            { "RequirementsAnalysisId", request.RequirementsAnalysisId },
                            { "ProjectId", projectId }
                        }
                    };

                    var reviewResponse = await _reviewService.Value.SubmitForReviewAsync(reviewRequest, cancellationToken).ConfigureAwait(false);

                    // Update the project planning entity with the review ID
                    projectPlanningEntity.ReviewId = reviewResponse.ReviewId.ToString();
                    await _projectPlanningRepository.UpdateAsync(projectPlanningEntity, cancellationToken).ConfigureAwait(false);

                    // Ensure changes are flushed before commit
                    await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

                    _logger.LogInformation("Project planning {PlanningId} persisted and review linked within transaction. Review ID: {ReviewId}",
                        planningId, reviewResponse.ReviewId);
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    throw;
                }

                var response = new ProjectPlanningResponse
                {
                    PlanningId = planningId,
                    RequirementsAnalysisId = request.RequirementsAnalysisId,
                    ProjectRoadmap = parsedResponse.ProjectRoadmap,
                    ArchitecturalDecisions = parsedResponse.ArchitecturalDecisions,
                    Milestones = parsedResponse.Milestones,
                    ReviewId = Guid.Parse(projectPlanningEntity.ReviewId),
                    Status = ProjectPlanningStatus.PendingReview,
                    CreatedAt = DateTime.UtcNow
                };

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Project planning {PlanningId} failed with exception", planningId);
                throw;
            }
        }

        public async Task<ProjectPlanningStatus> GetPlanningStatusAsync(
            Guid planningId,
            CancellationToken cancellationToken = default)
        {
            var projectPlanning = await _projectPlanningRepository.GetByPlanningIdAsync(planningId.ToString(), cancellationToken).ConfigureAwait(false);
            if (projectPlanning != null)
            {
                return projectPlanning.Status;
            }

            return ProjectPlanningStatus.Failed;
        }

        public async Task<bool> CanCreatePlanAsync(
            Guid requirementsAnalysisId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("ProjectPlanningService: Checking if plan can be created for requirements analysis {RequirementsAnalysisId}", requirementsAnalysisId);
            try
            {
                // Check that requirements analysis exists
                var analysisResult = await _requirementsAnalysisService.GetAnalysisResultsAsync(
                    requirementsAnalysisId, cancellationToken).ConfigureAwait(false);

                if (analysisResult == null)
                {
                    _logger.LogWarning("ProjectPlanningService: Analysis result is null for {RequirementsAnalysisId}", requirementsAnalysisId);
                    return false;
                }

                _logger.LogInformation("ProjectPlanningService: Analysis result for {RequirementsAnalysisId} has status {Status}", requirementsAnalysisId, analysisResult.Status);

                // Check that requirements analysis is approved
                var canCreate = analysisResult.ReviewId != Guid.Empty &&
                       analysisResult.Status == RequirementsAnalysisStatus.Approved;

                _logger.LogInformation("ProjectPlanningService: CanCreatePlan for {RequirementsAnalysisId} is {CanCreate}", requirementsAnalysisId, canCreate);

                return canCreate;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking if plan can be created for requirements analysis {RequirementsAnalysisId}", requirementsAnalysisId);
                return false;
            }
        }

        public async Task<string?> GetPlanningResultContentAsync(
            Guid planningId,
            CancellationToken cancellationToken = default)
        {
            var projectPlanning = await _projectPlanningRepository.GetByPlanningIdAsync(planningId.ToString(), cancellationToken).ConfigureAwait(false);
            if (projectPlanning != null)
            {
                return projectPlanning.Content;
            }

            return null;
        }

        public async Task<ProjectPlanningResponse?> GetPlanningResultsAsync(
            Guid planningId,
            CancellationToken cancellationToken = default)
        {
            var projectPlanning = await _projectPlanningRepository.GetByPlanningIdAsync(planningId.ToString(), cancellationToken).ConfigureAwait(false);
            if (projectPlanning != null)
            {
                return new ProjectPlanningResponse
                {
                    PlanningId = Guid.Parse(projectPlanning.PlanningId),
                    RequirementsAnalysisId = Guid.NewGuid(), // We don't store this as a GUID in the entity
                    ProjectRoadmap = projectPlanning.Content ?? string.Empty,
                    ArchitecturalDecisions = string.Empty, // Property doesn't exist in entity
                    Milestones = string.Empty, // Property doesn't exist in entity
                    ReviewId = Guid.Parse(projectPlanning.ReviewId),
                    Status = projectPlanning.Status,
                    CreatedAt = projectPlanning.CreatedDate
                };
            }

            return null;
        }

        public async Task<Guid?> GetRequirementsAnalysisIdAsync(
            Guid planningId,
            CancellationToken cancellationToken = default)
        {
            var projectPlanning = await _projectPlanningRepository.GetByPlanningIdAsync(planningId.ToString(), cancellationToken).ConfigureAwait(false);
            if (projectPlanning != null)
            {
                // Get the requirements analysis entity to retrieve the AnalysisId (GUID)
                var requirementsAnalysisEntity = await _requirementsAnalysisRepository.GetByIdAsync(projectPlanning.RequirementsAnalysisId, cancellationToken).ConfigureAwait(false);
                if (requirementsAnalysisEntity != null)
                {
                    // Parse the AnalysisId string to GUID and return it
                    if (Guid.TryParse(requirementsAnalysisEntity.AnalysisId, out var analysisId))
                    {
                        return analysisId;
                    }
                }
            }

            return null;
        }

        public async Task<string?> GetTechnicalContextAsync(Guid planningId, CancellationToken cancellationToken = default)
        {
            var projectPlanning = await _projectPlanningRepository.GetByPlanningIdAsync(planningId.ToString(), cancellationToken).ConfigureAwait(false);
            if (projectPlanning != null)
            {
                return projectPlanning.Content;
            }

            return null;
        }

        public async Task UpdatePlanningStatusAsync(
            Guid planningId,
            ProjectPlanningStatus status,
            CancellationToken cancellationToken = default)
        {
            var projectPlanning = await _projectPlanningRepository.GetByPlanningIdAsync(planningId.ToString(), cancellationToken).ConfigureAwait(false);
            if (projectPlanning != null)
            {
                projectPlanning.Status = status;
                await _projectPlanningRepository.UpdateAsync(projectPlanning, cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Updated project planning {PlanningId} status to {Status}", planningId, status);
            }
            else
            {
                _logger.LogWarning("Project planning {PlanningId} not found to update status", planningId);
            }
        }

        public async Task<ProjectPlanning?> GetPlanningByProjectAsync(int projectId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("ProjectPlanningService: Getting planning for project {ProjectId}", projectId);

            try
            {
                // Query the repository for planning by project ID
                var planningEntity = await _projectPlanningRepository.GetByProjectIdAsync(projectId, cancellationToken).ConfigureAwait(false);

                _logger.LogDebug("ProjectPlanningService: Found planning for project {ProjectId}: {Found}",
                    projectId, planningEntity != null ? "Yes" : "No");

                return planningEntity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ProjectPlanningService: Error getting planning for project {ProjectId}", projectId);
                return null;
            }
        }

        private string CreatePromptFromContext(
            RequirementsAnalysisResponse requirementsAnalysis,
            ProjectPlanningRequest request)
        {
            var prompt = new StringBuilder();

            prompt.AppendLine("# Project Planning Request");
            prompt.AppendLine();
            prompt.AppendLine("## Approved Requirements Analysis");
            prompt.AppendLine(requirementsAnalysis.AnalysisResult);
            prompt.AppendLine();

            if (!string.IsNullOrWhiteSpace(request.PlanningPreferences))
            {
                prompt.AppendLine("## Planning Preferences");
                prompt.AppendLine(request.PlanningPreferences);
                prompt.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(request.TechnicalConstraints))
            {
                prompt.AppendLine("## Technical Constraints");
                prompt.AppendLine(request.TechnicalConstraints);
                prompt.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(request.TimelineConstraints))
            {
                prompt.AppendLine("## Timeline Constraints");
                prompt.AppendLine(request.TimelineConstraints);
                prompt.AppendLine();
            }

            prompt.AppendLine("## Instructions");
            prompt.AppendLine("Please provide a detailed project plan with the following sections:");
            prompt.AppendLine("1. Project Roadmap - Phases, timelines, and dependencies");
            prompt.AppendLine("2. Architectural Decisions - Technology stack, patterns, and infrastructure");
            prompt.AppendLine("3. Milestones - Key deliverables, success criteria, and checkpoints");

            return prompt.ToString();
        }

        private ParsedProjectPlan ParseAIResponse(string aiResponse)
        {
            // Simple parsing - in a production system, this would be more sophisticated
            // For now, we'll just return the full response in each field
            return new ParsedProjectPlan
            {
                ProjectRoadmap = aiResponse,
                ArchitecturalDecisions = aiResponse,
                Milestones = aiResponse
            };
        }


        private async Task<int?> GetRequirementsAnalysisEntityId(Guid requirementsAnalysisId, CancellationToken cancellationToken)
        {
            var analysisIdStr = requirementsAnalysisId.ToString();
            var entityId = await _requirementsAnalysisRepository.GetEntityIdByAnalysisIdAsync(analysisIdStr, cancellationToken).ConfigureAwait(false);

            if (!entityId.HasValue)
            {
                _logger.LogError("Could not find RequirementsAnalysis entity for ID {RequirementsAnalysisId}", requirementsAnalysisId);
                throw new InvalidOperationException($"RequirementsAnalysis with ID {requirementsAnalysisId} not found");
            }

            return entityId.Value;
        }

        private class ParsedProjectPlan
        {
            public string ProjectRoadmap { get; set; } = string.Empty;
            public string ArchitecturalDecisions { get; set; } = string.Empty;
            public string Milestones { get; set; } = string.Empty;
        }

        private async Task SaveMetadataAsync(ProjectPlanningRequest request, Guid planningId, CancellationToken cancellationToken)
        {
            // Save request metadata (preferences, constraints, timestamps) in parallel with AI call
            // This reduces overall latency by overlapping I/O operations
            _logger.LogDebug("Saving metadata for project planning {PlanningId} in parallel", planningId);

            // Placeholder for actual metadata persistence - could save to DB or cache
            // For example: await _metadataRepository.SaveAsync(new PlanningMetadata { PlanningId = planningId, Request = request, CreatedAt = DateTime.UtcNow });
            await Task.Delay(100, cancellationToken).ConfigureAwait(false); // Simulate async I/O (replace with real save)
        }
    }
}
