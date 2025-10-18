using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Models.Code;
using AIProjectOrchestrator.Domain.Services;
using Microsoft.Extensions.Logging;

namespace AIProjectOrchestrator.Application.Services
{
    /// <summary>
    /// In-memory implementation of code generation state management.
    /// Follows Single Responsibility Principle - only manages state.
    /// Future enhancement: Replace with Redis or database for persistence across restarts.
    /// </summary>
    public class CodeGenerationStateManager : ICodeGenerationStateManager
    {
        private readonly ConcurrentDictionary<Guid, CodeGenerationResponse> _states;
        private readonly ILogger<CodeGenerationStateManager> _logger;

        public CodeGenerationStateManager(ILogger<CodeGenerationStateManager> logger)
        {
            _states = new ConcurrentDictionary<Guid, CodeGenerationResponse>();
            _logger = logger;
        }

        public Task SaveStateAsync(Guid generationId, CodeGenerationResponse state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            _states[generationId] = state;
            _logger.LogDebug("Saved state for generation {GenerationId} with status {Status}",
                generationId, state.Status);
            
            return Task.CompletedTask;
        }

        public Task<CodeGenerationResponse?> GetStateAsync(Guid generationId)
        {
            if (_states.TryGetValue(generationId, out var state))
            {
                _logger.LogDebug("Retrieved state for generation {GenerationId} with status {Status}",
                    generationId, state.Status);
                return Task.FromResult<CodeGenerationResponse?>(state);
            }

            _logger.LogWarning("State not found for generation {GenerationId}", generationId);
            return Task.FromResult<CodeGenerationResponse?>(null);
        }

        public async Task<CodeGenerationStatus> GetStatusAsync(Guid generationId)
        {
            var state = await GetStateAsync(generationId).ConfigureAwait(false);
            return state?.Status ?? CodeGenerationStatus.Failed;
        }

        public async Task UpdateStatusAsync(Guid generationId, CodeGenerationStatus status)
        {
            var state = await GetStateAsync(generationId).ConfigureAwait(false);
            if (state != null)
            {
                state.Status = status;
                await SaveStateAsync(generationId, state).ConfigureAwait(false);
                _logger.LogInformation("Updated status for generation {GenerationId} to {Status}",
                    generationId, status);
            }
            else
            {
                _logger.LogWarning("Cannot update status for generation {GenerationId} - state not found",
                    generationId);
            }
        }
    }
}
