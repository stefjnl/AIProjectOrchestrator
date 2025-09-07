using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AIProjectOrchestrator.Domain.Services;
using AIProjectOrchestrator.Domain.Models;
using AIProjectOrchestrator.Domain.Models.AI;
using AIProjectOrchestrator.Domain.Models.Review;
using AIProjectOrchestrator.Infrastructure.AI;
using System.Text;
using AIProjectOrchestrator.Domain.Interfaces;
using AIProjectOrchestrator.Domain.Entities;

namespace AIProjectOrchestrator.Application.Services
{
    public class ProjectPlanningService : IProjectPlanningService
    {
        private readonly IRequirementsAnalysisService _requirementsAnalysisService;
        private readonly IInstructionService _instructionService;
        private readonly IAIClientFactory _aiClientFactory;
        private readonly Lazy<IReviewService> _reviewService;
        private readonly ILogger<ProjectPlanningService> _logger;
        private readonly IProjectPlanningRepository _projectPlanningRepository;

        public ProjectPlanningService(
            IRequirementsAnalysisService requirementsAnalysisService,
            IInstructionService instructionService,
            IAIClientFactory aiClientFactory,
            Lazy<IReviewService> reviewService,
            ILogger<ProjectPlanningService> logger,
            IProjectPlanningRepository projectPlanningRepository)
        {
            _requirementsAnalysisService = requirementsAnalysisService;
            _instructionService = instructionService;
            _aiClientFactory = aiClientFactory;
            _reviewService = reviewService;
            _logger = logger;
            _projectPlanningRepository = projectPlanningRepository;
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
                var canCreatePlan = await CanCreatePlanAsync(request.RequirementsAnalysisId, cancellationToken);
                if (!canCreatePlan)
                {
                    _logger.LogWarning("Project planning {PlanningId} failed: Requirements analysis is not approved or does not exist", planningId);
                    throw new InvalidOperationException("Requirements analysis is not approved or does not exist");
                }

                // Retrieve the approved requirements analysis results
                _logger.LogDebug("Retrieving requirements analysis results for planning {PlanningId}", planningId);
                var requirementsAnalysis = await _requirementsAnalysisService.GetAnalysisResultsAsync(
                    request.RequirementsAnalysisId, cancellationToken);
                
                if (requirementsAnalysis == null)
                {
                    _logger.LogError("Project planning {PlanningId} failed: Requirements analysis not found", planningId);
                    throw new InvalidOperationException("Requirements analysis not found");
                }

                // Load planning instructions
                _logger.LogDebug("Loading instructions for project planning {PlanningId}", planningId);
                var instructionContent = await _instructionService.GetInstructionAsync("ProjectPlanner", cancellationToken);
                
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
                    ModelName = "qwen/qwen3-coder", // Default model for project planning via OpenRouter
                    Temperature = 0.7,
                    MaxTokens = 4000 // Larger response expected for project planning
                };

                // Log context size metrics
                var contextSize = Encoding.UTF8.GetByteCount(aiRequest.SystemMessage + aiRequest.Prompt);
                _logger.LogInformation("Project planning {PlanningId} context size: {ContextSize} bytes", planningId, contextSize);

                // Warn if context size is approaching limits
                if (contextSize > 100000) // Roughly 25K tokens
                {
                    _logger.LogWarning("Project planning {PlanningId} context size is large: {ContextSize} bytes", planningId, contextSize);
                }

                // Get OpenRouter AI client
                var aiClient = _aiClientFactory.GetClient("OpenRouter");
                if (aiClient == null)
                {
                    _logger.LogError("Project planning {PlanningId} failed: OpenRouter AI client not available", planningId);
                    throw new InvalidOperationException("OpenRouter AI client is not available");
                }

                _logger.LogDebug("Calling AI client for project planning {PlanningId}", planningId);
                
                // Call AI
                var aiResponse = await aiClient.CallAsync(aiRequest, cancellationToken);
                
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

                await _projectPlanningRepository.AddAsync(projectPlanningEntity, cancellationToken);
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

                var reviewResponse = await _reviewService.Value.SubmitForReviewAsync(reviewRequest, cancellationToken);

                // Update the project planning entity with the review ID
                projectPlanningEntity.ReviewId = reviewResponse.ReviewId.ToString();
                await _projectPlanningRepository.UpdateAsync(projectPlanningEntity, cancellationToken);

                _logger.LogInformation("Project planning {PlanningId} completed successfully. Review ID: {ReviewId}", 
                    planningId, reviewResponse.ReviewId);

                var response = new ProjectPlanningResponse
                {
                    PlanningId = planningId,
                    RequirementsAnalysisId = request.RequirementsAnalysisId,
                    ProjectRoadmap = parsedResponse.ProjectRoadmap,
                    ArchitecturalDecisions = parsedResponse.ArchitecturalDecisions,
                    Milestones = parsedResponse.Milestones,
                    ReviewId = reviewResponse.ReviewId,
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
            var projectPlanning = await _projectPlanningRepository.GetByPlanningIdAsync(planningId.ToString(), cancellationToken);
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
                    requirementsAnalysisId, cancellationToken);
                
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
            var projectPlanning = await _projectPlanningRepository.GetByPlanningIdAsync(planningId.ToString(), cancellationToken);
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
            var projectPlanning = await _projectPlanningRepository.GetByPlanningIdAsync(planningId.ToString(), cancellationToken);
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
            var projectPlanning = await _projectPlanningRepository.GetByPlanningIdAsync(planningId.ToString(), cancellationToken);
            if (projectPlanning != null)
            {
                // Get the requirements analysis ID from the database entity
                var requirementsAnalysis = await _requirementsAnalysisService.GetAnalysisResultsAsync(
                    Guid.Empty, // We'll need to map this properly
                    cancellationToken);
                
                // For now, return the ID from the entity
                return Guid.NewGuid(); // This needs to be fixed properly
            }

            return null;
        }

        public async Task<string?> GetTechnicalContextAsync(Guid planningId, CancellationToken cancellationToken = default)
        {
            var projectPlanning = await _projectPlanningRepository.GetByPlanningIdAsync(planningId.ToString(), cancellationToken);
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
            var projectPlanning = await _projectPlanningRepository.GetByPlanningIdAsync(planningId.ToString(), cancellationToken);
            if (projectPlanning != null)
            {
                projectPlanning.Status = status;
                await _projectPlanningRepository.UpdateAsync(projectPlanning, cancellationToken);
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
                var planningEntity = await _projectPlanningRepository.GetByProjectIdAsync(projectId, cancellationToken);
                
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

        private readonly IRequirementsAnalysisRepository _requirementsAnalysisRepository;

        public ProjectPlanningService(
            IRequirementsAnalysisService requirementsAnalysisService,
            IInstructionService instructionService,
            IAIClientFactory aiClientFactory,
            Lazy<IReviewService> reviewService,
            ILogger<ProjectPlanningService> logger,
            IProjectPlanningRepository projectPlanningRepository,
            IRequirementsAnalysisRepository requirementsAnalysisRepository)
        {
            _requirementsAnalysisService = requirementsAnalysisService;
            _instructionService = instructionService;
            _aiClientFactory = aiClientFactory;
            _reviewService = reviewService;
            _logger = logger;
            _projectPlanningRepository = projectPlanningRepository;
            _requirementsAnalysisRepository = requirementsAnalysisRepository;
        }

        private async Task<int?> GetRequirementsAnalysisEntityId(Guid requirementsAnalysisId, CancellationToken cancellationToken)
        {
            var analysisIdStr = requirementsAnalysisId.ToString();
            var entityId = await _requirementsAnalysisRepository.GetEntityIdByAnalysisIdAsync(analysisIdStr, cancellationToken);
            
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
    }
}
