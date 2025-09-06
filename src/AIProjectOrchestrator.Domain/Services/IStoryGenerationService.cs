using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Models.Stories;
using AIProjectOrchestrator.Domain.Entities;

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
            
        Task<List<UserStory>?> GetApprovedStoriesAsync(Guid storyGenerationId, CancellationToken cancellationToken = default);
        
        Task<Guid?> GetPlanningIdAsync(Guid storyGenerationId, CancellationToken cancellationToken = default);
        
        // Method to update story generation status when review is approved
        Task UpdateGenerationStatusAsync(
            Guid generationId,
            StoryGenerationStatus status,
            CancellationToken cancellationToken = default);
            
        // New methods for Phase 2
        Task<UserStory> GetIndividualStoryAsync(
            Guid storyGenerationId, 
            int storyIndex, 
            CancellationToken cancellationToken = default);
            
        Task<List<UserStory>> GetAllStoriesAsync(
            Guid storyGenerationId, 
            CancellationToken cancellationToken = default);
            
        Task<int> GetStoryCountAsync(
            Guid storyGenerationId, 
            CancellationToken cancellationToken = default);

        // Get generation by project for workflow state
        Task<StoryGeneration?> GetGenerationByProjectAsync(
            int projectId,
            CancellationToken cancellationToken = default);
    }
}
