using System;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Models.Code;

namespace AIProjectOrchestrator.Domain.Services
{
    /// <summary>
    /// Service for managing code generation state.
    /// Follows Single Responsibility Principle - only manages state persistence and retrieval.
    /// Future: Can be enhanced to use Redis, database, or other persistent storage.
    /// </summary>
    public interface ICodeGenerationStateManager
    {
        /// <summary>
        /// Saves the state of a code generation process
        /// </summary>
        /// <param name="generationId">The unique identifier for the generation</param>
        /// <param name="state">The current state</param>
        Task SaveStateAsync(Guid generationId, CodeGenerationResponse state);

        /// <summary>
        /// Retrieves the state of a code generation process
        /// </summary>
        /// <param name="generationId">The unique identifier for the generation</param>
        /// <returns>The state, or null if not found</returns>
        Task<CodeGenerationResponse?> GetStateAsync(Guid generationId);

        /// <summary>
        /// Gets the status of a code generation process
        /// </summary>
        /// <param name="generationId">The unique identifier for the generation</param>
        /// <returns>The status, or Failed if not found</returns>
        Task<CodeGenerationStatus> GetStatusAsync(Guid generationId);

        /// <summary>
        /// Updates the status of a code generation process
        /// </summary>
        /// <param name="generationId">The unique identifier for the generation</param>
        /// <param name="status">The new status</param>
        Task UpdateStatusAsync(Guid generationId, CodeGenerationStatus status);
    }
}
