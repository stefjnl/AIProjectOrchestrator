using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AIProjectOrchestrator.Domain.Services;
using AIProjectOrchestrator.Domain.Models;
using AIProjectOrchestrator.Domain.Models.AI;
using AIProjectOrchestrator.Domain.Models.Review;
using AIProjectOrchestrator.Infrastructure.AI;
using System.Collections.Concurrent;

namespace AIProjectOrchestrator.Application.Services
{
    public class RequirementsAnalysisService : IRequirementsAnalysisService
    {
        private readonly IInstructionService _instructionService;
        private readonly IAIClientFactory _aiClientFactory;
        private readonly IReviewService _reviewService;
        private readonly ILogger<RequirementsAnalysisService> _logger;
        private readonly ConcurrentDictionary<Guid, RequirementsAnalysisStatus> _analysisStatuses;
        private readonly ConcurrentDictionary<Guid, RequirementsAnalysisResponse> _analysisResults;

        public RequirementsAnalysisService(
            IInstructionService instructionService,
            IAIClientFactory aiClientFactory,
            IReviewService reviewService,
            ILogger<RequirementsAnalysisService> logger)
        {
            _instructionService = instructionService;
            _aiClientFactory = aiClientFactory;
            _reviewService = reviewService;
            _logger = logger;
            _analysisStatuses = new ConcurrentDictionary<Guid, RequirementsAnalysisStatus>();
            _analysisResults = new ConcurrentDictionary<Guid, RequirementsAnalysisResponse>();
        }

        public async Task<RequirementsAnalysisResponse> AnalyzeRequirementsAsync(
            RequirementsAnalysisRequest request,
            CancellationToken cancellationToken = default)
        {
            var analysisId = Guid.NewGuid();
            _logger.LogInformation("Starting requirements analysis {AnalysisId} for project: {ProjectDescription}", 
                analysisId, request.ProjectDescription);

            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(request.ProjectDescription))
                {
                    _logger.LogWarning("Requirements analysis {AnalysisId} failed: Project description is required", analysisId);
                    throw new ArgumentException("Project description is required");
                }

                if (request.ProjectDescription.Length < 10)
                {
                    _logger.LogWarning("Requirements analysis {AnalysisId} failed: Project description is too short", analysisId);
                    throw new ArgumentException("Project description must be at least 10 characters long");
                }

                // Set status to processing
                _analysisStatuses[analysisId] = RequirementsAnalysisStatus.Processing;

                // Load instructions
                _logger.LogDebug("Loading instructions for requirements analysis {AnalysisId}", analysisId);
                var instructionContent = await _instructionService.GetInstructionAsync("RequirementsAnalyst", cancellationToken);
                
                if (!instructionContent.IsValid)
                {
                    _logger.LogError("Requirements analysis {AnalysisId} failed: Invalid instruction content - {ValidationMessage}", 
                        analysisId, instructionContent.ValidationMessage);
                    _analysisStatuses[analysisId] = RequirementsAnalysisStatus.Failed;
                    throw new InvalidOperationException($"Failed to load valid instructions: {instructionContent.ValidationMessage}");
                }

                // Create AI request
                var aiRequest = new AIRequest
                {
                    SystemMessage = instructionContent.Content,
                    Prompt = CreatePromptFromRequest(request),
                    ModelName = "claude-3-5-sonnet-20240620", // Default model for requirements analysis
                    Temperature = 0.7,
                    MaxTokens = 2000
                };

                // Get Claude AI client
                var aiClient = _aiClientFactory.GetClient("Claude");
                if (aiClient == null)
                {
                    _logger.LogError("Requirements analysis {AnalysisId} failed: Claude AI client not available", analysisId);
                    _analysisStatuses[analysisId] = RequirementsAnalysisStatus.Failed;
                    throw new InvalidOperationException("Claude AI client is not available");
                }

                _logger.LogDebug("Calling AI client for requirements analysis {AnalysisId}", analysisId);
                
                // Call AI
                var aiResponse = await aiClient.CallAsync(aiRequest, cancellationToken);
                
                if (!aiResponse.IsSuccess)
                {
                    _logger.LogError("Requirements analysis {AnalysisId} failed: AI call failed - {ErrorMessage}", 
                        analysisId, aiResponse.ErrorMessage);
                    _analysisStatuses[analysisId] = RequirementsAnalysisStatus.Failed;
                    throw new InvalidOperationException($"AI call failed: {aiResponse.ErrorMessage}");
                }

                // Submit for review
                _logger.LogDebug("Submitting AI response for review in requirements analysis {AnalysisId}", analysisId);
                var correlationId = Guid.NewGuid().ToString();
                var reviewRequest = new SubmitReviewRequest
                {
                    ServiceName = "RequirementsAnalysis",
                    Content = aiResponse.Content,
                    CorrelationId = correlationId,
                    PipelineStage = "RequirementsAnalysis",
                    OriginalRequest = aiRequest,
                    AIResponse = aiResponse,
                    Metadata = new System.Collections.Generic.Dictionary<string, object>
                    {
                        { "AnalysisId", analysisId },
                        { "ProjectDescription", request.ProjectDescription }
                    }
                };

                var reviewResponse = await _reviewService.SubmitForReviewAsync(reviewRequest, cancellationToken);

                // Set status to pending review
                _analysisStatuses[analysisId] = RequirementsAnalysisStatus.PendingReview;

                _logger.LogInformation("Requirements analysis {AnalysisId} completed successfully. Review ID: {ReviewId}", 
                    analysisId, reviewResponse.ReviewId);

                var response = new RequirementsAnalysisResponse
                {
                    AnalysisId = analysisId,
                    ProjectDescription = request.ProjectDescription,
                    AnalysisResult = aiResponse.Content,
                    ReviewId = reviewResponse.ReviewId,
                    Status = RequirementsAnalysisStatus.PendingReview,
                    CreatedAt = DateTime.UtcNow
                };

                // Store the analysis result for later retrieval
                _analysisResults[analysisId] = response;

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Requirements analysis {AnalysisId} failed with exception", analysisId);
                _analysisStatuses[analysisId] = RequirementsAnalysisStatus.Failed;
                throw;
            }
        }

        public async Task<RequirementsAnalysisStatus> GetAnalysisStatusAsync(
            Guid analysisId,
            CancellationToken cancellationToken = default)
        {
            if (_analysisStatuses.TryGetValue(analysisId, out var status))
            {
                return status;
            }
            
            // If we don't have the status in memory, it might have been cleaned up
            // In a production system, we would check a persistent store
            return RequirementsAnalysisStatus.Failed;
        }

        public async Task<RequirementsAnalysisResponse?> GetAnalysisResultsAsync(
            Guid analysisId,
            CancellationToken cancellationToken = default)
        {
            if (_analysisResults.TryGetValue(analysisId, out var result))
            {
                return result;
            }
            
            // If we don't have the result in memory, it might have been cleaned up
            // In a production system, we would check a persistent store
            return null;
        }

        public async Task<string?> GetAnalysisResultContentAsync(
            Guid analysisId,
            CancellationToken cancellationToken = default)
        {
            if (_analysisResults.TryGetValue(analysisId, out var result))
            {
                return result.AnalysisResult;
            }
            
            // If we don't have the result in memory, it might have been cleaned up
            // In a production system, we would check a persistent store
            return null;
        }

        public async Task<bool> CanAnalyzeRequirementsAsync(
            Guid projectId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // For now, always allow requirements analysis
                // In a production system, this might check:
                // - If the project exists
                // - If requirements analysis hasn't already been completed
                // - If there are any business rules preventing analysis
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if requirements can be analyzed for project {ProjectId}", projectId);
                return false;
            }
        }

        private string CreatePromptFromRequest(RequirementsAnalysisRequest request)
        {
            var prompt = $"Project Description: {request.ProjectDescription}";

            if (!string.IsNullOrWhiteSpace(request.AdditionalContext))
            {
                prompt += $"\n\nAdditional Context: {request.AdditionalContext}";
            }

            if (!string.IsNullOrWhiteSpace(request.Constraints))
            {
                prompt += $"\n\nConstraints: {request.Constraints}";
            }

            return prompt;
        }
    }
}
