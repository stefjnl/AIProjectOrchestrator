using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AIProjectOrchestrator.Domain.Services;
using AIProjectOrchestrator.Domain.Models.Code;
using AIProjectOrchestrator.Domain.Models.Review;

namespace AIProjectOrchestrator.Application.Services
{
    /// <summary>
    /// Service for coordinating code generation.
    /// Refactored to follow Single Responsibility Principle (SRP) with reduced dependencies.
    /// Dependencies reduced from 11 to 5 by extracting responsibilities to focused services.
    /// </summary>
    public class CodeGenerationService : ICodeGenerationService
    {
        private readonly IWorkflowDependencyValidator _dependencyValidator;
        private readonly ICodeGenerationOrchestrator _orchestrator;
        private readonly ICodeGenerationStateManager _stateManager;
        private readonly IContextRetriever _contextRetriever;
        private readonly IInstructionService _instructionService;
        private readonly IReviewService _reviewService;
        private readonly IFileOrganizer _fileOrganizer;
        private readonly ILogger<CodeGenerationService> _logger;

        public CodeGenerationService(
            IWorkflowDependencyValidator dependencyValidator,
            ICodeGenerationOrchestrator orchestrator,
            ICodeGenerationStateManager stateManager,
            IContextRetriever contextRetriever,
            IInstructionService instructionService,
            IReviewService reviewService,
            IFileOrganizer fileOrganizer,
            ILogger<CodeGenerationService> logger)
        {
            _dependencyValidator = dependencyValidator;
            _orchestrator = orchestrator;
            _stateManager = stateManager;
            _contextRetriever = contextRetriever;
            _instructionService = instructionService;
            _reviewService = reviewService;
            _fileOrganizer = fileOrganizer;
            _logger = logger;
        }

        public async Task<CodeGenerationResponse> GenerateCodeAsync(
            CodeGenerationRequest request,
            CancellationToken cancellationToken = default)
        {
            var generationId = Guid.NewGuid();
            _logger.LogInformation("Starting code generation {GenerationId} for story generation: {StoryGenerationId}",
                generationId, request.StoryGenerationId);

            try
            {
                // Validate input
                if (request.StoryGenerationId == Guid.Empty)
                {
                    _logger.LogWarning("Code generation failed: Story generation ID is required");
                    throw new ArgumentException("Story generation ID is required");
                }

                // Initialize response and save initial state
                var response = new CodeGenerationResponse
                {
                    GenerationId = generationId,
                    Status = CodeGenerationStatus.Processing,
                    CreatedAt = DateTime.UtcNow
                };
                await _stateManager.SaveStateAsync(generationId, response);

                // Step 1: Validate dependencies using extracted service
                _logger.LogDebug("Validating workflow dependencies for generation {GenerationId}", generationId);
                var validation = await _dependencyValidator.ValidateDependenciesAsync(
                    request.StoryGenerationId,
                    WorkflowStage.CodeGeneration,
                    cancellationToken);

                if (!validation.IsValid)
                {
                    _logger.LogWarning("Code generation {GenerationId} failed dependency validation: {ErrorMessage}",
                        generationId, validation.ErrorMessage);
                    response.Status = CodeGenerationStatus.Failed;
                    await _stateManager.SaveStateAsync(generationId, response);
                    throw new InvalidOperationException(validation.ErrorMessage);
                }

                // Step 2: Retrieve comprehensive context from all upstream services
                _logger.LogDebug("Retrieving comprehensive context for generation {GenerationId}", generationId);
                var comprehensiveContext = await _contextRetriever.RetrieveComprehensiveContextAsync(
                    request.StoryGenerationId, cancellationToken);

                // Step 3: Select AI model and load instructions
                await _stateManager.UpdateStatusAsync(generationId, CodeGenerationStatus.SelectingModel);
                var selectedModel = "qwen3-coder"; // Simplified model selection
                _logger.LogInformation("Code generation {GenerationId}: Selected model: {SelectedModel}",
                    generationId, selectedModel);
                response.SelectedModel = selectedModel;

                string instructionFileName = selectedModel.ToLower() switch
                {
                    "qwen3-coder" => "CodeGenerator_Qwen3Coder",
                    _ => $"CodeGenerator_{selectedModel}"
                };

                var instructionContent = await _instructionService.GetInstructionAsync(instructionFileName, cancellationToken);
                if (!instructionContent.IsValid)
                {
                    _logger.LogError("Code generation {GenerationId} failed: Invalid instruction content - {ValidationMessage}",
                        generationId, instructionContent.ValidationMessage);
                    response.Status = CodeGenerationStatus.Failed;
                    await _stateManager.SaveStateAsync(generationId, response);
                    throw new InvalidOperationException($"Failed to load valid instructions: {instructionContent.ValidationMessage}");
                }

                // Step 4: Orchestrate code generation using extracted orchestrator
                await _stateManager.UpdateStatusAsync(generationId, CodeGenerationStatus.GeneratingTests);
                
                var generationContext = new CodeGenerationContext
                {
                    GenerationId = generationId,
                    StoryGenerationId = request.StoryGenerationId,
                    ComprehensiveContext = comprehensiveContext,
                    SelectedModel = selectedModel,
                    InstructionContent = instructionContent.Content,
                    PlanningId = validation.PlanningId,
                    RequirementsAnalysisId = validation.RequirementsAnalysisId
                };

                _logger.LogDebug("Orchestrating code generation for {GenerationId}", generationId);
                var result = await _orchestrator.OrchestrateGenerationAsync(generationContext, cancellationToken);

                // Step 5: Submit for review
                await _stateManager.UpdateStatusAsync(generationId, CodeGenerationStatus.PendingReview);
                _logger.LogDebug("Submitting AI response for review in code generation {GenerationId}", generationId);
                
                var reviewRequest = new SubmitReviewRequest
                {
                    ServiceName = "CodeGeneration",
                    Content = _fileOrganizer.SerializeCodeArtifacts(result.OrganizedFiles),
                    CorrelationId = Guid.NewGuid().ToString(),
                    PipelineStage = "Implementation",
                    OriginalRequest = null,
                    AIResponse = null,
                    Metadata = new Dictionary<string, object>
                    {
                        { "GenerationId", generationId },
                        { "StoryGenerationId", request.StoryGenerationId }
                    }
                };

                var reviewResponse = await _reviewService.SubmitForReviewAsync(reviewRequest, cancellationToken);

                // Step 6: Update final response
                response.GeneratedFiles = result.GeneratedFiles;
                response.TestFiles = result.TestFiles;
                response.ReviewId = reviewResponse.ReviewId;
                response.Status = CodeGenerationStatus.PendingReview;
                await _stateManager.SaveStateAsync(generationId, response);

                _logger.LogInformation("Code generation {GenerationId} completed successfully. Review ID: {ReviewId}",
                    generationId, reviewResponse.ReviewId);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Code generation {GenerationId} failed with exception", generationId);
                await _stateManager.UpdateStatusAsync(generationId, CodeGenerationStatus.Failed);
                throw;
            }
        }

        public async Task<CodeGenerationStatus> GetStatusAsync(Guid codeGenerationId, CancellationToken cancellationToken = default)
        {
            return await _stateManager.GetStatusAsync(codeGenerationId);
        }

        public async Task<CodeArtifactsResult> GetGeneratedCodeAsync(Guid codeGenerationId, CancellationToken cancellationToken = default)
        {
            var state = await _stateManager.GetStateAsync(codeGenerationId);
            if (state == null)
            {
                _logger.LogWarning("Code generation state not found for {CodeGenerationId}", codeGenerationId);
                return new CodeArtifactsResult
                {
                    GenerationId = codeGenerationId,
                    Artifacts = new List<CodeArtifact>()
                };
            }

            var allFiles = new List<CodeArtifact>();
            if (state.GeneratedFiles != null)
                allFiles.AddRange(state.GeneratedFiles);
            if (state.TestFiles != null)
                allFiles.AddRange(state.TestFiles);

            // Calculate file types distribution
            var fileTypes = new Dictionary<string, int>();
            foreach (var file in allFiles)
            {
                if (fileTypes.ContainsKey(file.FileType))
                    fileTypes[file.FileType]++;
                else
                    fileTypes[file.FileType] = 1;
            }

            // Calculate total size
            long totalSize = 0;
            foreach (var file in allFiles)
            {
                totalSize += Encoding.UTF8.GetByteCount(file.Content);
            }

            return new CodeArtifactsResult
            {
                GenerationId = codeGenerationId,
                Artifacts = allFiles,
                GeneratedAt = state.CreatedAt,
                PackageName = $"GeneratedCode_{codeGenerationId}",
                TotalSizeBytes = totalSize,
                FileCount = allFiles.Count,
                FileTypes = fileTypes
            };
        }

        public async Task<bool> CanGenerateCodeAsync(
            Guid storyGenerationId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate dependencies to check if stories are approved
                var validation = await _dependencyValidator.ValidateDependenciesAsync(
                    storyGenerationId,
                    WorkflowStage.CodeGeneration,
                    cancellationToken);
                
                return validation.IsValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if code can be generated for story generation {StoryGenerationId}",
                    storyGenerationId);
                return false;
            }
        }

        public async Task<byte[]?> GetGeneratedFilesZipAsync(
            Guid generationId,
            CancellationToken cancellationToken = default)
        {
            var state = await _stateManager.GetStateAsync(generationId);
            if (state == null || state.Status != CodeGenerationStatus.Approved)
            {
                _logger.LogWarning("Cannot get ZIP for generation {GenerationId} - state not found or not approved",
                    generationId);
                return null;
            }

            return await _fileOrganizer.GetGeneratedFilesZipAsync(
                generationId,
                state.GeneratedFiles ?? new List<CodeArtifact>(),
                state.TestFiles ?? new List<CodeArtifact>(),
                cancellationToken);
        }
    }
}
