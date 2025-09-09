using System;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Application.Interfaces;
using AIProjectOrchestrator.Domain.Models.AI;
using AIProjectOrchestrator.Domain.Models.PromptGeneration;
using AIProjectOrchestrator.Domain.Services;
using AIProjectOrchestrator.Infrastructure.AI;
using Microsoft.Extensions.Logging;

namespace AIProjectOrchestrator.Application.Services
{
    public class PromptGenerationService : IPromptGenerationService
    {
        private readonly IStoryGenerationService _storyGenerationService;
        private readonly IProjectPlanningService _projectPlanningService;
        private readonly IInstructionService _instructionService;
        private readonly IAIClientFactory _aiClientFactory;
        private readonly Lazy<IReviewService> _reviewService;
        private readonly ILogger<PromptGenerationService> _logger;
        private readonly ILogger<PromptContextAssembler> _loggerAssembler;
        private readonly Domain.Interfaces.IPromptGenerationRepository _promptGenerationRepository;
        private readonly Domain.Interfaces.IStoryGenerationRepository _storyGenerationRepository;

        public PromptGenerationService(
            IStoryGenerationService storyGenerationService,
            IProjectPlanningService projectPlanningService,
            IInstructionService instructionService,
            IAIClientFactory aiClientFactory,
            Lazy<IReviewService> reviewService,
            ILogger<PromptGenerationService> logger,
            ILogger<PromptContextAssembler> loggerAssembler,
            Domain.Interfaces.IPromptGenerationRepository promptGenerationRepository,
            Domain.Interfaces.IStoryGenerationRepository storyGenerationRepository)
        {
            _storyGenerationService = storyGenerationService;
            _projectPlanningService = projectPlanningService;
            _instructionService = instructionService;
            _aiClientFactory = aiClientFactory;
            _reviewService = reviewService;
            _logger = logger;
            _loggerAssembler = loggerAssembler;
            _promptGenerationRepository = promptGenerationRepository;
            _storyGenerationRepository = storyGenerationRepository;
        }

        public Task<bool> CanGeneratePromptAsync(Guid storyGenerationId, int storyIndex, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<PromptGenerationResponse> GeneratePromptAsync(PromptGenerationRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<PromptGenerationStatus> GetPromptStatusAsync(Guid promptId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<AIResponse> GeneratePromptFromPlaygroundAsync(string promptContent, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("=== PROMPT GENERATION SERVICE STARTING ===");
            _logger.LogInformation("Generating prompt from playground...");

            try
            {
                _logger.LogInformation("About to call AIClientFactory.GetClient('NanoGpt')");
                var aiClient = _aiClientFactory.GetClient("NanoGpt");
                _logger.LogInformation("AIClientFactory returned client: {ClientName}", aiClient?.ProviderName ?? "NULL");

                var aiRequest = new AIRequest
                {
                    Prompt = promptContent,
                    ModelName = "moonshotai/Kimi-K2-Instruct-0905", // or any other model
                    Temperature = 0.7,
                    MaxTokens = 2000
                };

                var aiResponse = await aiClient.CallAsync(aiRequest, cancellationToken);

                if (!aiResponse.IsSuccess)
                {
                    _logger.LogError("AI call from playground failed: {ErrorMessage}", aiResponse.ErrorMessage);
                    throw new InvalidOperationException($"AI call from playground failed: {aiResponse.ErrorMessage}");
                }

                _logger.LogInformation("Prompt from playground generated successfully.");
                return aiResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating prompt from playground");
                throw;
            }
        }

        public Task<PromptGenerationResponse> GetPromptAsync(Guid promptId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<PromptGenerationResponse>> GetPromptsByProjectAsync(int projectId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
