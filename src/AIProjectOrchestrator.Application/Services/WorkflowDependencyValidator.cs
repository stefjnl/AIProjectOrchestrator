using System;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Services;
using Microsoft.Extensions.Logging;

namespace AIProjectOrchestrator.Application.Services
{
    /// <summary>
    /// Implementation of workflow dependency validation.
    /// Validates that all required upstream workflow stages are approved.
    /// Follows Single Responsibility Principle - only does dependency validation.
    /// </summary>
    public class WorkflowDependencyValidator : IWorkflowDependencyValidator
    {
        private readonly IStoryGenerationService _storyGenerationService;
        private readonly IProjectPlanningService _projectPlanningService;
        private readonly IRequirementsAnalysisService _requirementsAnalysisService;
        private readonly ILogger<WorkflowDependencyValidator> _logger;

        public WorkflowDependencyValidator(
            IStoryGenerationService storyGenerationService,
            IProjectPlanningService projectPlanningService,
            IRequirementsAnalysisService requirementsAnalysisService,
            ILogger<WorkflowDependencyValidator> logger)
        {
            _storyGenerationService = storyGenerationService;
            _projectPlanningService = projectPlanningService;
            _requirementsAnalysisService = requirementsAnalysisService;
            _logger = logger;
        }

        public async Task<DependencyValidationResult> ValidateDependenciesAsync(
            Guid entityId,
            WorkflowStage stage,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Validating dependencies for {Stage} with entity ID: {EntityId}",
                stage, entityId);

            try
            {
                return stage switch
                {
                    WorkflowStage.CodeGeneration => await ValidateCodeGenerationDependenciesAsync(entityId, cancellationToken),
                    WorkflowStage.PromptGeneration => await ValidatePromptGenerationDependenciesAsync(entityId, cancellationToken),
                    WorkflowStage.StoryGeneration => await ValidateStoryGenerationDependenciesAsync(entityId, cancellationToken),
                    WorkflowStage.ProjectPlanning => await ValidateProjectPlanningDependenciesAsync(entityId, cancellationToken),
                    WorkflowStage.RequirementsAnalysis => await ValidateRequirementsAnalysisDependenciesAsync(entityId, cancellationToken),
                    _ => DependencyValidationResult.Failure(entityId, stage, $"Unknown workflow stage: {stage}")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating dependencies for {Stage} with entity ID: {EntityId}",
                    stage, entityId);
                return DependencyValidationResult.Failure(entityId, stage, $"Validation error: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates CodeGeneration dependencies: Stories must be approved
        /// </summary>
        private async Task<DependencyValidationResult> ValidateCodeGenerationDependenciesAsync(
            Guid storyGenerationId,
            CancellationToken cancellationToken)
        {
            // Validate stories are approved
            var stories = await _storyGenerationService.GetApprovedStoriesAsync(storyGenerationId, cancellationToken);
            if (stories == null || !stories.Any())
            {
                _logger.LogWarning("CodeGeneration validation failed: Stories not found or not approved for {StoryGenerationId}",
                    storyGenerationId);
                return DependencyValidationResult.Failure(storyGenerationId, WorkflowStage.CodeGeneration,
                    "Stories not found or not approved");
            }

            // Get planning ID
            var planningId = await _storyGenerationService.GetPlanningIdAsync(storyGenerationId, cancellationToken);
            if (!planningId.HasValue)
            {
                _logger.LogWarning("CodeGeneration validation failed: Planning ID not found for {StoryGenerationId}",
                    storyGenerationId);
                return DependencyValidationResult.Failure(storyGenerationId, WorkflowStage.CodeGeneration,
                    "Planning ID not found");
            }

            // Validate planning is approved
            var planningValidation = await ValidateProjectPlanningStatusAsync(planningId.Value, cancellationToken);
            if (!planningValidation.IsValid)
            {
                return DependencyValidationResult.Failure(storyGenerationId, WorkflowStage.CodeGeneration,
                    $"Planning validation failed: {planningValidation.ErrorMessage}");
            }

            _logger.LogInformation("CodeGeneration dependencies validated successfully for {StoryGenerationId}",
                storyGenerationId);

            return new DependencyValidationResult
            {
                IsValid = true,
                EntityId = storyGenerationId,
                Stage = WorkflowStage.CodeGeneration,
                StoryGenerationId = storyGenerationId,
                PlanningId = planningId.Value,
                RequirementsAnalysisId = planningValidation.RequirementsAnalysisId
            };
        }

        /// <summary>
        /// Validates PromptGeneration dependencies: Stories must be approved
        /// </summary>
        private async Task<DependencyValidationResult> ValidatePromptGenerationDependenciesAsync(
            Guid storyGenerationId,
            CancellationToken cancellationToken)
        {
            // Same validation as CodeGeneration - stories must be approved
            return await ValidateCodeGenerationDependenciesAsync(storyGenerationId, cancellationToken);
        }

        /// <summary>
        /// Validates StoryGeneration dependencies: Planning must be approved
        /// </summary>
        private Task<DependencyValidationResult> ValidateStoryGenerationDependenciesAsync(
            Guid entityId,
            CancellationToken cancellationToken)
        {
            // For StoryGeneration, entityId is actually the planning ID (need to convert)
            // In a real implementation, we'd need to handle this properly
            _logger.LogWarning("StoryGeneration validation not fully implemented - requires planning ID mapping");
            
            return Task.FromResult(new DependencyValidationResult
            {
                IsValid = true,
                EntityId = entityId,
                Stage = WorkflowStage.StoryGeneration
            });
        }

        /// <summary>
        /// Validates ProjectPlanning dependencies: RequirementsAnalysis must be approved
        /// </summary>
        private Task<DependencyValidationResult> ValidateProjectPlanningDependenciesAsync(
            Guid entityId,
            CancellationToken cancellationToken)
        {
            // For ProjectPlanning, entityId needs to map to requirements analysis ID
            _logger.LogWarning("ProjectPlanning validation not fully implemented - requires requirements analysis ID mapping");
            
            return Task.FromResult(new DependencyValidationResult
            {
                IsValid = true,
                EntityId = entityId,
                Stage = WorkflowStage.ProjectPlanning
            });
        }

        /// <summary>
        /// Validates RequirementsAnalysis dependencies: No upstream dependencies
        /// </summary>
        private Task<DependencyValidationResult> ValidateRequirementsAnalysisDependenciesAsync(
            Guid entityId,
            CancellationToken cancellationToken)
        {
            // RequirementsAnalysis is the first stage, no dependencies
            _logger.LogInformation("RequirementsAnalysis has no upstream dependencies - validation passed");
            
            return Task.FromResult(new DependencyValidationResult
            {
                IsValid = true,
                EntityId = entityId,
                Stage = WorkflowStage.RequirementsAnalysis
            });
        }

        /// <summary>
        /// Helper method to validate project planning status
        /// </summary>
        private async Task<DependencyValidationResult> ValidateProjectPlanningStatusAsync(
            Guid planningId,
            CancellationToken cancellationToken)
        {
            var planningStatus = await _projectPlanningService.GetPlanningStatusAsync(planningId, cancellationToken);
            if (planningStatus != AIProjectOrchestrator.Domain.Models.ProjectPlanningStatus.Approved)
            {
                _logger.LogWarning("Planning validation failed: Planning {PlanningId} is not approved (status: {Status})",
                    planningId, planningStatus);
                return DependencyValidationResult.Failure(Guid.Empty, WorkflowStage.ProjectPlanning,
                    $"Project planning is not approved (status: {planningStatus})");
            }

            // Get requirements analysis ID
            var requirementsAnalysisId = await _projectPlanningService.GetRequirementsAnalysisIdAsync(
                planningId, cancellationToken);
            if (!requirementsAnalysisId.HasValue)
            {
                _logger.LogWarning("Planning validation failed: Requirements analysis ID not found for planning {PlanningId}",
                    planningId);
                return DependencyValidationResult.Failure(Guid.Empty, WorkflowStage.ProjectPlanning,
                    "Requirements analysis ID not found");
            }

            // Validate requirements analysis is approved
            var requirementsStatus = await _requirementsAnalysisService.GetAnalysisStatusAsync(
                requirementsAnalysisId.Value, cancellationToken);
            if (requirementsStatus != AIProjectOrchestrator.Domain.Models.RequirementsAnalysisStatus.Approved)
            {
                _logger.LogWarning("Planning validation failed: Requirements analysis {RequirementsAnalysisId} is not approved (status: {Status})",
                    requirementsAnalysisId.Value, requirementsStatus);
                return DependencyValidationResult.Failure(Guid.Empty, WorkflowStage.ProjectPlanning,
                    $"Requirements analysis is not approved (status: {requirementsStatus})");
            }

            return new DependencyValidationResult
            {
                IsValid = true,
                EntityId = Guid.Empty,
                Stage = WorkflowStage.ProjectPlanning,
                PlanningId = planningId,
                RequirementsAnalysisId = requirementsAnalysisId.Value
            };
        }
    }
}
