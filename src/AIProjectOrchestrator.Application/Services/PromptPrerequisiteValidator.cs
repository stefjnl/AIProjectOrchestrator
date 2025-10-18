using System;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Services;
using AIProjectOrchestrator.Domain.Models.Stories;

namespace AIProjectOrchestrator.Application.Services
{
    public static class PromptPrerequisiteValidator
    {
        public static async Task<bool> ValidateStoryApprovalAsync(
            IStoryGenerationService storyGenerationService,
            Guid storyGenerationId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var status = await storyGenerationService.GetGenerationStatusAsync(storyGenerationId, cancellationToken).ConfigureAwait(false);
                return status == StoryGenerationStatus.Approved;
            }
            catch
            {
                return false;
            }
        }
        
        public static async Task<bool> ValidateStoryExistsAsync(
            IStoryGenerationService storyGenerationService,
            Guid storyGenerationId,
            int storyIndex,
            CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var storyCount = await storyGenerationService.GetStoryCountAsync(storyGenerationId, cancellationToken).ConfigureAwait(false);
                return storyIndex >= 0 && storyIndex < storyCount;
            }
            catch
            {
                return false;
            }
        }
    }
}