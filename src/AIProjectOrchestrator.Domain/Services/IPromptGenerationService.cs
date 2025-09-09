using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Models.AI;
using AIProjectOrchestrator.Domain.Models.PromptGeneration;

namespace AIProjectOrchestrator.Domain.Services
{
    public interface IPromptGenerationService
    {
        Task<PromptGenerationResponse> GeneratePromptAsync(PromptGenerationRequest request, CancellationToken cancellationToken = default);
        Task<PromptGenerationStatus> GetPromptStatusAsync(Guid promptId, CancellationToken cancellationToken = default);
        Task<bool> CanGeneratePromptAsync(Guid storyGenerationId, int storyIndex, CancellationToken cancellationToken = default);
        Task<AIResponse> GeneratePromptFromPlaygroundAsync(string promptContent, CancellationToken cancellationToken = default);
        Task<PromptGenerationResponse> GetPromptAsync(Guid promptId, CancellationToken cancellationToken = default);
        Task<IEnumerable<PromptGenerationResponse>> GetPromptsByProjectAsync(int projectId, CancellationToken cancellationToken = default);
    }
}