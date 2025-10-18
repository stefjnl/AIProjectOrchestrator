using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Models.Code;

namespace AIProjectOrchestrator.Domain.Services
{
    /// <summary>
    /// Context for code generation orchestration.
    /// Contains all necessary information for generating code.
    /// </summary>
    public class CodeGenerationContext
    {
        /// <summary>
        /// Unique identifier for this generation process
        /// </summary>
        public Guid GenerationId { get; set; }

        /// <summary>
        /// The story generation ID this code is being generated for
        /// </summary>
        public Guid StoryGenerationId { get; set; }

        /// <summary>
        /// Comprehensive context retrieved from upstream services
        /// </summary>
        public string ComprehensiveContext { get; set; } = string.Empty;

        /// <summary>
        /// Selected AI model for code generation
        /// </summary>
        public string SelectedModel { get; set; } = "qwen3-coder";

        /// <summary>
        /// Model-specific instruction content
        /// </summary>
        public string InstructionContent { get; set; } = string.Empty;

        /// <summary>
        /// Planning ID from dependency validation
        /// </summary>
        public int? PlanningId { get; set; }

        /// <summary>
        /// Requirements Analysis ID from dependency validation
        /// </summary>
        public int? RequirementsAnalysisId { get; set; }
    }

    /// <summary>
    /// Result of code generation orchestration.
    /// Contains the generated artifacts and metadata.
    /// </summary>
    public class CodeGenerationResult
    {
        /// <summary>
        /// Generation ID
        /// </summary>
        public Guid GenerationId { get; set; }

        /// <summary>
        /// Generated code files (excluding tests)
        /// </summary>
        public List<CodeArtifact> GeneratedFiles { get; set; } = new();

        /// <summary>
        /// Generated test files
        /// </summary>
        public List<CodeArtifact> TestFiles { get; set; } = new();

        /// <summary>
        /// All files organized by project structure
        /// </summary>
        public List<CodeArtifact> OrganizedFiles { get; set; } = new();

        /// <summary>
        /// Selected AI model used
        /// </summary>
        public string SelectedModel { get; set; } = string.Empty;

        /// <summary>
        /// Generation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Story Generation ID
        /// </summary>
        public Guid StoryGenerationId { get; set; }
    }

    /// <summary>
    /// Service for orchestrating the code generation workflow.
    /// Follows Single Responsibility Principle - only orchestrates the generation process.
    /// </summary>
    public interface ICodeGenerationOrchestrator
    {
        /// <summary>
        /// Orchestrates the complete code generation workflow:
        /// 1. Generate tests (TDD approach)
        /// 2. Generate implementation
        /// 3. Validate generated code
        /// 4. Organize files by structure
        /// </summary>
        /// <param name="context">The generation context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The generation result with all artifacts</returns>
        Task<CodeGenerationResult> OrchestrateGenerationAsync(
            CodeGenerationContext context,
            CancellationToken cancellationToken = default);
    }
}
