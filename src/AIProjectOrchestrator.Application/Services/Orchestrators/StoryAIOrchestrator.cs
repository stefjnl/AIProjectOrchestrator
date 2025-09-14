using System;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Interfaces;
using AIProjectOrchestrator.Domain.Models.AI;
using AIProjectOrchestrator.Infrastructure.AI.Providers;
using Microsoft.Extensions.Logging;

namespace AIProjectOrchestrator.Application.Services.Orchestrators
{
    public class StoryAIOrchestrator : IStoryAIOrchestrator
    {
        private readonly IStoryAIProvider _storyAIProvider;
        private readonly ILogger<StoryAIOrchestrator> _logger;

        public StoryAIOrchestrator(
            IStoryAIProvider storyAIProvider,
            ILogger<StoryAIOrchestrator> logger)
        {
            _storyAIProvider = storyAIProvider;
            _logger = logger;
        }

        public async Task<AIResponse> GenerateAsync(AIRequest request, Guid generationId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Calling AI provider for story generation {GenerationId}", generationId);

            // Call AI using the story-specific provider
            var generatedContent = await _storyAIProvider.GenerateContentAsync(request.Prompt, request.SystemMessage);

            // GenerateContentAsync returns the content directly, so we need to create an AIResponse
            var aiResponse = new AIResponse
            {
                Content = generatedContent,
                TokensUsed = 0, // We don't have token info from GenerateContentAsync
                ProviderName = _storyAIProvider.ProviderName,
                IsSuccess = true,
                ErrorMessage = null
            };

            if (!aiResponse.IsSuccess)
            {
                _logger.LogError("Story generation {GenerationId} failed: AI call failed - {ErrorMessage}",
                    generationId, aiResponse.ErrorMessage);
                throw new InvalidOperationException($"AI call failed: {aiResponse.ErrorMessage}");
            }

            _logger.LogDebug("AI generation succeeded for story generation {GenerationId}", generationId);

            return aiResponse;
        }
    }
}
