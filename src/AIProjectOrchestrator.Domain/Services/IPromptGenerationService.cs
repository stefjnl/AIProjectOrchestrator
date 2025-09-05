using System;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Models.PromptGeneration;

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
            Guid storyId,
            CancellationToken cancellationToken = default);
    }
}