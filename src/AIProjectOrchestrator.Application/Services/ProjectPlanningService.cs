using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AIProjectOrchestrator.Domain.Services;
using AIProjectOrchestrator.Domain.Models;
using AIProjectOrchestrator.Domain.Models.AI;
using AIProjectOrchestrator.Domain.Models.Review;
using AIProjectOrchestrator.Infrastructure.AI;
using System.Collections.Concurrent;
using System.Text;

namespace AIProjectOrchestrator.Application.Services
{
    public class ProjectPlanningService : IProjectPlanningService
    {
        private readonly IRequirementsAnalysisService _requirementsAnalysisService;
        private readonly IInstructionService _instructionService;
        private readonly IAIClientFactory _aiClientFactory;
        private readonly IReviewService _reviewService;
        private readonly ILogger<ProjectPlanningService> _logger;
        private readonly ConcurrentDictionary<Guid, ProjectPlanningStatus> _planningStatuses;
        private readonly ConcurrentDictionary<Guid, ProjectPlanningResponse> _planningResults;

        public ProjectPlanningService(
            IRequirementsAnalysisService requirementsAnalysisService,
            IInstructionService instructionService,
            IAIClientFactory aiClientFactory,
            IReviewService reviewService,
            ILogger<ProjectPlanningService> logger)
        {
            _requirementsAnalysisService = requirementsAnalysisService;
            _instructionService = instructionService;
            _aiClientFactory = aiClientFactory;
            _reviewService = reviewService;
            _logger = logger;
            _planningStatuses = new ConcurrentDictionary<Guid, ProjectPlanningStatus>();
            _planningResults = new ConcurrentDictionary<Guid, ProjectPlanningResponse>();
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

                // Set status to processing
                _planningStatuses[planningId] = ProjectPlanningStatus.Processing;

                // Validate that requirements analysis exists and is approved
                var canCreatePlan = await CanCreatePlanAsync(request.RequirementsAnalysisId, cancellationToken);
                if (!canCreatePlan)
                {
                    _logger.LogWarning("Project planning {PlanningId} failed: Requirements analysis is not approved or does not exist", planningId);
                    _planningStatuses[planningId] = ProjectPlanningStatus.RequirementsNotApproved;
                    throw new InvalidOperationException("Requirements analysis is not approved or does not exist");
                }

                // Retrieve the approved requirements analysis results
                _logger.LogDebug("Retrieving requirements analysis results for planning {PlanningId}", planningId);
                var requirementsAnalysis = await _requirementsAnalysisService.GetAnalysisResultsAsync(
                    request.RequirementsAnalysisId, cancellationToken);
                
                if (requirementsAnalysis == null)
                {
                    _logger.LogError("Project planning {PlanningId} failed: Requirements analysis not found", planningId);
                    _planningStatuses[planningId] = ProjectPlanningStatus.Failed;
                    throw new InvalidOperationException("Requirements analysis not found");
                }

                // Load planning instructions
                _logger.LogDebug("Loading instructions for project planning {PlanningId}", planningId);
                var instructionContent = await _instructionService.GetInstructionAsync("ProjectPlanner", cancellationToken);
                
                if (!instructionContent.IsValid)
                {
                    _logger.LogError("Project planning {PlanningId} failed: Invalid instruction content - {ValidationMessage}", 
                        planningId, instructionContent.ValidationMessage);
                    _planningStatuses[planningId] = ProjectPlanningStatus.Failed;
                    throw new InvalidOperationException($"Failed to load valid instructions: {instructionContent.ValidationMessage}");
                }

                // Create AI request with combined context
                var aiRequest = new AIRequest
                {
                    SystemMessage = instructionContent.Content,
                    Prompt = CreatePromptFromContext(requirementsAnalysis, request),
                    ModelName = "claude-3-5-sonnet-20240620", // Default model for project planning
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

                // Get Claude AI client
                var aiClient = _aiClientFactory.GetClient("Claude");
                if (aiClient == null)
                {
                    _logger.LogError("Project planning {PlanningId} failed: Claude AI client not available", planningId);
                    _planningStatuses[planningId] = ProjectPlanningStatus.Failed;
                    throw new InvalidOperationException("Claude AI client is not available");
                }

                _logger.LogDebug("Calling AI client for project planning {PlanningId}", planningId);
                
                // Call AI
                var aiResponse = await aiClient.CallAsync(aiRequest, cancellationToken);
                
                if (!aiResponse.IsSuccess)
                {
                    _logger.LogError("Project planning {PlanningId} failed: AI call failed - {ErrorMessage}", 
                        planningId, aiResponse.ErrorMessage);
                    _planningStatuses[planningId] = ProjectPlanningStatus.Failed;
                    throw new InvalidOperationException($"AI call failed: {aiResponse.ErrorMessage}");
                }

                // Parse AI response into structured components
                var parsedResponse = ParseAIResponse(aiResponse.Content);

                // Submit for review
                _logger.LogDebug("Submitting AI response for review in project planning {PlanningId}", planningId);
                var correlationId = Guid.NewGuid().ToString();
                var reviewRequest = new SubmitReviewRequest
                {
                    ServiceName = "ProjectPlanning",
                    Content = aiResponse.Content,
                    CorrelationId = correlationId,
                    PipelineStage = "ProjectPlanning",
                    OriginalRequest = aiRequest,
                    AIResponse = aiResponse,
                    Metadata = new System.Collections.Generic.Dictionary<string, object>
                    {
                        { "PlanningId", planningId },
                        { "RequirementsAnalysisId", request.RequirementsAnalysisId }
                    }
                };

                var reviewResponse = await _reviewService.SubmitForReviewAsync(reviewRequest, cancellationToken);

                // Set status to pending review
                _planningStatuses[planningId] = ProjectPlanningStatus.PendingReview;

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

                // Store the planning result for later retrieval
                _planningResults[planningId] = response;

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Project planning {PlanningId} failed with exception", planningId);
                _planningStatuses[planningId] = ProjectPlanningStatus.Failed;
                throw;
            }
        }

        public async Task<ProjectPlanningStatus> GetPlanningStatusAsync(
            Guid planningId,
            CancellationToken cancellationToken = default)
        {
            if (_planningStatuses.TryGetValue(planningId, out var status))
            {
                return status;
            }
            
            // If we don't have the status in memory, it might have been cleaned up
            // In a production system, we would check a persistent store
            return ProjectPlanningStatus.Failed;
        }

        public async Task<bool> CanCreatePlanAsync(
            Guid requirementsAnalysisId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Check that requirements analysis exists
                var analysisResult = await _requirementsAnalysisService.GetAnalysisResultsAsync(
                    requirementsAnalysisId, cancellationToken);
                
                if (analysisResult == null)
                {
                    return false;
                }

                // Check that requirements analysis is approved
                // For now, we'll assume that if it exists and has a ReviewId, it's approved
                // In a more sophisticated system, we would check the review status
                return analysisResult.ReviewId != Guid.Empty && 
                       analysisResult.Status == RequirementsAnalysisStatus.PendingReview; // Approved requirements would be in PendingReview status
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if plan can be created for requirements analysis {RequirementsAnalysisId}", requirementsAnalysisId);
                return false;
            }
        }

        public async Task<string?> GetPlanningResultContentAsync(
            Guid planningId,
            CancellationToken cancellationToken = default)
        {
            if (_planningResults.TryGetValue(planningId, out var result))
            {
                // Combine all planning content into a single string
                return $"Project Roadmap:\n{result.ProjectRoadmap}\n\nArchitectural Decisions:\n{result.ArchitecturalDecisions}\n\nMilestones:\n{result.Milestones}";
            }

            // If we don't have the result in memory, it might have been cleaned up
            // In a production system, we would check a persistent store
            return null;
        }

        public async Task<Guid?> GetRequirementsAnalysisIdAsync(
            Guid planningId,
            CancellationToken cancellationToken = default)
        {
            if (_planningResults.TryGetValue(planningId, out var result))
            {
                return result.RequirementsAnalysisId;
            }

            // If we don't have the result in memory, it might have been cleaned up
            // In a production system, we would check a persistent store
            return null;
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

        private class ParsedProjectPlan
        {
            public string ProjectRoadmap { get; set; } = string.Empty;
            public string ArchitecturalDecisions { get; set; } = string.Empty;
            public string Milestones { get; set; } = string.Empty;
        }
    }
}
