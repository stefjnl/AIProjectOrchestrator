using System;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Models.PromptGeneration;
using AIProjectOrchestrator.Domain.Entities;

namespace AIProjectOrchestrator.Domain.Services
{
    public interface IPromptGenerationService
    {
        Task<PromptGenerationResponse> GeneratePromptAsync(
            PromptGenerationRequest request,
            CancellationToken cancellationToken = default);

        Task<PromptGenerationStatus> GetPromptStatusAsync(
            Guid promptId,
            CancellationToken cancellationToken = default);

        Task<bool> CanGeneratePromptAsync(
            Guid storyGenerationId,
            int storyIndex,
            CancellationToken cancellationToken = default);
            
        // Overload for backward compatibility
        Task<bool> CanGeneratePromptAsync(
            Guid storyId,
            CancellationToken cancellationToken = default);

        Task<PromptGenerationResponse?> GetPromptAsync(
            Guid promptId,
            CancellationToken cancellationToken = default);

        // Get prompts by project for workflow state
        Task<IEnumerable<PromptGeneration>> GetPromptsByProjectAsync(
            int projectId,
            CancellationToken cancellationToken = default);
    }
}
