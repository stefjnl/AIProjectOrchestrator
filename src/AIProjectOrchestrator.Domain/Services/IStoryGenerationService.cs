using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Models.Stories;

namespace AIProjectOrchestrator.Domain.Services
{
    public interface IStoryGenerationService
    {
        Task<StoryGenerationResponse> GenerateStoriesAsync(
            StoryGenerationRequest request,
            CancellationToken cancellationToken = default);
            
        Task<StoryGenerationStatus> GetGenerationStatusAsync(
            Guid generationId,
            CancellationToken cancellationToken = default);
            
        Task<List<UserStory>?> GetGenerationResultsAsync(
            Guid generationId,
            CancellationToken cancellationToken = default);
            
        Task<bool> CanGenerateStoriesAsync(
            Guid planningId,
            CancellationToken cancellationToken = default);
    }
}