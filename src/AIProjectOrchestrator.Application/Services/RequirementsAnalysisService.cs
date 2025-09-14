using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AIProjectOrchestrator.Domain.Services;
using AIProjectOrchestrator.Domain.Models;
using AIProjectOrchestrator.Domain.Models.AI;
using AIProjectOrchestrator.Domain.Models.Review;
using AIProjectOrchestrator.Infrastructure.AI;
using AIProjectOrchestrator.Infrastructure.AI.Providers;
using AIProjectOrchestrator.Domain.Interfaces;
using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Exceptions;

namespace AIProjectOrchestrator.Application.Services
{
    public class RequirementsAnalysisService : IRequirementsAnalysisService
    {
        private readonly IInstructionService _instructionService;
        private readonly IRequirementsAIProvider _requirementsAIProvider;
        private readonly Lazy<IReviewService> _reviewService;
        private readonly ILogger<RequirementsAnalysisService> _logger;
        private readonly IRequirementsAnalysisRepository _requirementsAnalysisRepository;

        public RequirementsAnalysisService(
            IInstructionService instructionService,
            IRequirementsAIProvider requirementsAIProvider,
            Lazy<IReviewService> reviewService,
            ILogger<RequirementsAnalysisService> logger,
            IRequirementsAnalysisRepository requirementsAnalysisRepository)
        {
            _instructionService = instructionService;
            _requirementsAIProvider = requirementsAIProvider;
            _reviewService = reviewService;
            _logger = logger;
            _requirementsAnalysisRepository = requirementsAnalysisRepository;
        }

        public async Task<RequirementsAnalysisResponse> AnalyzeRequirementsAsync(
            RequirementsAnalysisRequest request,
            CancellationToken cancellationToken = default)
        {
            var analysisId = Guid.NewGuid().ToString();
            
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["Operation"] = "AnalyzeRequirements",
                ["AnalysisId"] = analysisId,
                ["ProjectId"] = request.ProjectId
            });

            _logger.LogInformation("Starting requirements analysis for project");

            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(request.ProjectDescription))
                {
                    _logger.LogWarning("Validation failed: Project description is required");
                    throw new ValidationException("Project description is required");
                }

                if (request.ProjectDescription.Length < 10)
                {
                    _logger.LogWarning("Validation failed: Project description is too short");
                    throw new ValidationException("Project description must be at least 10 characters long");
                }

                // Load instructions
                _logger.LogDebug("Loading instructions from RequirementsAnalyst");
                var instructionContent = await _instructionService.GetInstructionAsync("RequirementsAnalyst", cancellationToken);
                
                if (!instructionContent.IsValid)
                {
                    _logger.LogError("Failed to load valid instructions: {ValidationMessage}",
                        instructionContent.ValidationMessage);
                    throw new InvalidOperationException($"Failed to load valid instructions: {instructionContent.ValidationMessage}");
                }

                // Create AI request
                var aiRequest = new AIRequest
                {
                    SystemMessage = instructionContent.Content,
                    Prompt = CreatePromptFromRequest(request),
                    ModelName = string.Empty, // Will be set by the provider
                    Temperature = 0.7, // Default value, will be overridden by provider
                    MaxTokens = 1000  // Default value, will be overridden by provider
                };

                _logger.LogDebug("Calling AI provider: {ProviderName}", _requirementsAIProvider.ProviderName);
                
                // Call AI using the requirements-specific provider
                var generatedContent = await _requirementsAIProvider.GenerateContentAsync(aiRequest.Prompt, aiRequest.SystemMessage);
                
                // GenerateContentAsync returns the content directly, so we need to create an AIResponse
                var aiResponse = new AIResponse
                {
                    Content = generatedContent,
                    TokensUsed = 0, // We don't have token info from GenerateContentAsync
                    ProviderName = _requirementsAIProvider.ProviderName,
                    IsSuccess = true,
                    ErrorMessage = null
                };

                // Create and store the analysis entity first to get the entity ID
                var analysisEntity = new RequirementsAnalysis
                {
                    AnalysisId = analysisId,
                    ProjectId = int.TryParse(request.ProjectId, out var projectId) ? projectId : 0,
                    Status = RequirementsAnalysisStatus.PendingReview,
                    Content = aiResponse.Content,
                    ReviewId = string.Empty, // Will be updated after review submission
                    CreatedDate = DateTime.UtcNow
                };

                await _requirementsAnalysisRepository.AddAsync(analysisEntity, cancellationToken);
                var savedAnalysisId = analysisEntity.Id; // Get the database-generated int ID

                // Submit for review
                _logger.LogDebug("Submitting AI response for review");
                var correlationId = Guid.NewGuid().ToString();
                var reviewRequest = new SubmitReviewRequest
                {
                    ServiceName = "RequirementsAnalysis",
                    Content = aiResponse.Content,
                    CorrelationId = correlationId,
                    PipelineStage = "Analysis",
                    OriginalRequest = aiRequest,
                    AIResponse = aiResponse,
                    Metadata = new System.Collections.Generic.Dictionary<string, object>
                    {
                        { "AnalysisId", analysisId },
                        { "EntityId", savedAnalysisId }, // Pass the entity int ID for FK linking
                        { "ProjectDescription", request.ProjectDescription },
                        { "ProjectId", request.ProjectId ?? "unknown" } // Include project ID for workflow correlation
                    }
                };

                var reviewResponse = await _reviewService.Value.SubmitForReviewAsync(reviewRequest, cancellationToken);

                // Update the analysis entity with the review ID
                analysisEntity.ReviewId = reviewResponse.ReviewId.ToString();
                await _requirementsAnalysisRepository.UpdateAsync(analysisEntity, cancellationToken);

                _logger.LogInformation("Requirements analysis completed successfully. Review ID: {ReviewId}",
                    reviewResponse.ReviewId);

                var response = new RequirementsAnalysisResponse
                {
                    AnalysisId = Guid.Parse(analysisId),
                    ProjectDescription = request.ProjectDescription,
                    AnalysisResult = aiResponse.Content,
                    ReviewId = reviewResponse.ReviewId,
                    Status = RequirementsAnalysisStatus.PendingReview,
                    CreatedAt = DateTime.UtcNow,
                    ProjectId = request.ProjectId
                };

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Requirements analysis failed with exception");
                throw; // Let middleware handle the response
            }
        }

        public async Task<RequirementsAnalysisStatus> GetAnalysisStatusAsync(
            Guid analysisId,
            CancellationToken cancellationToken = default)
        {
            var analysisEntity = await _requirementsAnalysisRepository.GetByAnalysisIdAsync(analysisId.ToString(), cancellationToken);
            if (analysisEntity != null)
            {
                return analysisEntity.Status;
            }
            
            return RequirementsAnalysisStatus.Failed;
        }

        public async Task<RequirementsAnalysisResponse?> GetAnalysisResultsAsync(
            Guid analysisId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("RequirementsAnalysisService: Getting analysis results for {AnalysisId}", analysisId);
            var analysisEntity = await _requirementsAnalysisRepository.GetByAnalysisIdAsync(analysisId.ToString(), cancellationToken);
            if (analysisEntity != null)
            {
                _logger.LogInformation("RequirementsAnalysisService: Found analysis {AnalysisId} with status {Status}", analysisId, analysisEntity.Status);
                return new RequirementsAnalysisResponse
                {
                    AnalysisId = Guid.Parse(analysisEntity.AnalysisId),
                    ProjectDescription = "", // This isn't stored in the entity
                    AnalysisResult = analysisEntity.Content,
                    ReviewId = Guid.Parse(analysisEntity.ReviewId),
                    Status = analysisEntity.Status,
                    CreatedAt = analysisEntity.CreatedDate,
                    ProjectId = analysisEntity.ProjectId.ToString()
                };
            }
            
            _logger.LogWarning("RequirementsAnalysisService: Analysis {AnalysisId} not found in database", analysisId);
            return null;
        }

        public async Task<string?> GetAnalysisResultContentAsync(
            Guid analysisId,
            CancellationToken cancellationToken = default)
        {
            var analysisEntity = await _requirementsAnalysisRepository.GetByAnalysisIdAsync(analysisId.ToString(), cancellationToken);
            if (analysisEntity != null)
            {
                return analysisEntity.Content;
            }
            
            return null;
        }

        public async Task<bool> CanAnalyzeRequirementsAsync(
            int projectId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if requirements analysis already exists for this project
                var existingAnalysis = await _requirementsAnalysisRepository.GetByProjectIdAsync(projectId, cancellationToken);
                return existingAnalysis == null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if requirements can be analyzed for project {ProjectId}", projectId);
                return false;
            }
        }

        public async Task<string?> GetBusinessContextAsync(Guid analysisId, CancellationToken cancellationToken = default)
        {
            var analysisEntity = await _requirementsAnalysisRepository.GetByAnalysisIdAsync(analysisId.ToString(), cancellationToken);
            if (analysisEntity != null)
            {
                return analysisEntity.Content;
            }

            return null;
        }
        
        public async Task UpdateAnalysisStatusAsync(
            Guid analysisId,
            RequirementsAnalysisStatus status,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("RequirementsAnalysisService: Updating analysis {AnalysisId} to status {Status}", analysisId, status);

            var analysisEntity = await _requirementsAnalysisRepository.GetByAnalysisIdAsync(analysisId.ToString(), cancellationToken);
            if (analysisEntity != null)
            {
                analysisEntity.Status = status;
                await _requirementsAnalysisRepository.UpdateAsync(analysisEntity, cancellationToken);
                _logger.LogInformation("RequirementsAnalysisService: Confirmed analysis {AnalysisId} status updated to {Status}", analysisId, status);
            }
            else
            {
                _logger.LogWarning("RequirementsAnalysisService: Analysis {AnalysisId} not found in database to update status", analysisId);
            }
        }

        public async Task<RequirementsAnalysis?> GetAnalysisByProjectAsync(int projectId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("RequirementsAnalysisService: Getting analysis for project {ProjectId}", projectId);
            
            try
            {
                // Query the repository for analysis by project ID
                var analysisEntity = await _requirementsAnalysisRepository.GetByProjectIdAsync(projectId, cancellationToken);
                
                _logger.LogDebug("RequirementsAnalysisService: Found analysis for project {ProjectId}: {Found}", 
                    projectId, analysisEntity != null ? "Yes" : "No");
                
                return analysisEntity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RequirementsAnalysisService: Error getting analysis for project {ProjectId}", projectId);
                return null;
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
