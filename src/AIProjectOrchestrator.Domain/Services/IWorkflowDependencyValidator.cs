using System;
using System.Threading;
using System.Threading.Tasks;

namespace AIProjectOrchestrator.Domain.Services
{
    /// <summary>
    /// Workflow stage enumeration for dependency validation
    /// </summary>
    public enum WorkflowStage
    {
        RequirementsAnalysis,
        ProjectPlanning,
        StoryGeneration,
        PromptGeneration,
        CodeGeneration
    }

    /// <summary>
    /// Result of workflow dependency validation
    /// </summary>
    public class DependencyValidationResult
    {
        /// <summary>
        /// Whether all dependencies are satisfied
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Error message if validation failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// The entity ID that was validated
        /// </summary>
        public Guid EntityId { get; set; }

        /// <summary>
        /// The workflow stage being validated
        /// </summary>
        public WorkflowStage Stage { get; set; }

        /// <summary>
        /// Story Generation ID (if applicable)
        /// </summary>
        public Guid? StoryGenerationId { get; set; }

        /// <summary>
        /// Planning ID (if applicable)
        /// </summary>
        public Guid? PlanningId { get; set; }

        /// <summary>
        /// Requirements Analysis ID (if applicable)
        /// </summary>
        public Guid? RequirementsAnalysisId { get; set; }

        /// <summary>
        /// Creates a successful validation result
        /// </summary>
        public static DependencyValidationResult Success(Guid entityId, WorkflowStage stage)
        {
            return new DependencyValidationResult
            {
                IsValid = true,
                EntityId = entityId,
                Stage = stage
            };
        }

        /// <summary>
        /// Creates a failed validation result
        /// </summary>
        public static DependencyValidationResult Failure(Guid entityId, WorkflowStage stage, string errorMessage)
        {
            return new DependencyValidationResult
            {
                IsValid = false,
                EntityId = entityId,
                Stage = stage,
                ErrorMessage = errorMessage
            };
        }
    }

    /// <summary>
    /// Service for validating workflow stage dependencies.
    /// Ensures that all upstream stages are approved before proceeding.
    /// Follows Single Responsibility Principle by focusing only on dependency validation.
    /// </summary>
    public interface IWorkflowDependencyValidator
    {
        /// <summary>
        /// Validates that all required dependencies for a workflow stage are satisfied.
        /// For example, CodeGeneration requires approved Stories, which require approved Planning,
        /// which requires approved RequirementsAnalysis.
        /// </summary>
        /// <param name="entityId">The ID of the entity to validate (e.g., StoryGenerationId for CodeGeneration)</param>
        /// <param name="stage">The workflow stage being validated</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result with details about dependencies</returns>
        Task<DependencyValidationResult> ValidateDependenciesAsync(
            Guid entityId,
            WorkflowStage stage,
            CancellationToken cancellationToken = default);
    }
}
