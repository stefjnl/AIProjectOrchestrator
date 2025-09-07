using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AIProjectOrchestrator.Domain.Models;
using AIProjectOrchestrator.Domain.Services;
using AIProjectOrchestrator.Domain.Models.Code;
using AIProjectOrchestrator.Domain.Models.Stories;
using AIProjectOrchestrator.Domain.Models.AI;
using AIProjectOrchestrator.Domain.Models.Review;
using AIProjectOrchestrator.Infrastructure.AI;
using AIProjectOrchestrator.Domain.Entities;

namespace AIProjectOrchestrator.Application.Services
{
    public class CodeGenerationService : ICodeGenerationService
    {
        private readonly IStoryGenerationService _storyGenerationService;
        private readonly IProjectPlanningService _projectPlanningService;
        private readonly IRequirementsAnalysisService _requirementsAnalysisService;
        private readonly IInstructionService _instructionService;
        private readonly IAIClientFactory _aiClientFactory;
        private readonly IReviewService _reviewService;
        private readonly ITestGenerator _testGenerator;
        private readonly IImplementationGenerator _implementationGenerator;
        private readonly ICodeValidator _codeValidator;
        private readonly IContextRetriever _contextRetriever;
        private readonly IFileOrganizer _fileOrganizer;
        private readonly ILogger<CodeGenerationService> _logger;
        private readonly ConcurrentDictionary<Guid, CodeGenerationResponse> _generationResults = new();

        public CodeGenerationService(
            IStoryGenerationService storyGenerationService,
            IProjectPlanningService projectPlanningService,
            IRequirementsAnalysisService requirementsAnalysisService,
            IInstructionService instructionService,
            IAIClientFactory aiClientFactory,
            IReviewService reviewService,
            ITestGenerator testGenerator,
            IImplementationGenerator implementationGenerator,
            ICodeValidator codeValidator,
            IContextRetriever contextRetriever,
            IFileOrganizer fileOrganizer,
            ILogger<CodeGenerationService> logger)
        {
            _storyGenerationService = storyGenerationService;
            _projectPlanningService = projectPlanningService;
            _requirementsAnalysisService = requirementsAnalysisService;
            _instructionService = instructionService;
            _aiClientFactory = aiClientFactory;
            _reviewService = reviewService;
            _testGenerator = testGenerator;
            _implementationGenerator = implementationGenerator;
            _codeValidator = codeValidator;
            _contextRetriever = contextRetriever;
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

                // Set status to processing
                var response = new CodeGenerationResponse
                {
                    GenerationId = generationId,
                    Status = CodeGenerationStatus.Processing,
                    CreatedAt = DateTime.UtcNow
                };
                _generationResults[generationId] = response;

                // Validate four-stage dependencies (stories approved)
                // This validation logic will be extracted to a separate service in future iterations
                var stories = await _storyGenerationService.GetApprovedStoriesAsync(request.StoryGenerationId, cancellationToken);
                if (stories == null || !stories.Any())
                {
                    _logger.LogWarning("Code generation failed: Stories not found or not approved for story generation {StoryGenerationId}",
                        request.StoryGenerationId);
                    throw new InvalidOperationException("Stories not found or not approved");
                }

                var planningId = await _storyGenerationService.GetPlanningIdAsync(request.StoryGenerationId, cancellationToken);
                if (!planningId.HasValue)
                {
                    _logger.LogWarning("Code generation failed: Planning ID not found for story generation {StoryGenerationId}",
                        request.StoryGenerationId);
                    throw new InvalidOperationException("Planning ID not found");
                }

                var planningStatus = await _projectPlanningService.GetPlanningStatusAsync(planningId.Value, cancellationToken);
                if (planningStatus != AIProjectOrchestrator.Domain.Models.ProjectPlanningStatus.Approved)
                {
                    _logger.LogWarning("Code generation failed: Project planning {PlanningId} is not approved",
                        planningId.Value);
                    throw new InvalidOperationException("Project planning is not approved");
                }

                var requirementsAnalysisId = await _projectPlanningService.GetRequirementsAnalysisIdAsync(
                    planningId.Value, cancellationToken);
                if (!requirementsAnalysisId.HasValue)
                {
                    _logger.LogWarning("Code generation failed: Requirements analysis ID not found for planning {PlanningId}",
                        planningId.Value);
                    throw new InvalidOperationException("Requirements analysis ID not found");
                }

                var requirementsStatus = await _requirementsAnalysisService.GetAnalysisStatusAsync(
                    requirementsAnalysisId.Value, cancellationToken);
                if (requirementsStatus != AIProjectOrchestrator.Domain.Models.RequirementsAnalysisStatus.Approved)
                {
                    _logger.LogWarning("Code generation failed: Requirements analysis {RequirementsAnalysisId} is not approved",
                        requirementsAnalysisId.Value);
                    throw new InvalidOperationException("Requirements analysis is not approved");
                }

                // Retrieve comprehensive context from all upstream services
                var context = await _contextRetriever.RetrieveComprehensiveContextAsync(request.StoryGenerationId, cancellationToken);

                // Analyze stories and select optimal AI model
                response.Status = CodeGenerationStatus.SelectingModel;
                var selectedModel = "qwen3-coder"; // Simplified model selection - could be enhanced later
                _logger.LogInformation("Code generation {GenerationId}: Selected model: {SelectedModel}", generationId, selectedModel);
                response.SelectedModel = selectedModel;

                // Load model-specific instructions
                _logger.LogDebug("Loading instructions for code generation {GenerationId}", generationId);
                // Map the model name to the correct instruction file name
                string instructionFileName = selectedModel.ToLower() switch
                {
                    "qwen3-coder" => "CodeGenerator_Qwen3Coder",
                    _ => $"CodeGenerator_{selectedModel}"
                };

                // Log the instruction file name for debugging
                _logger.LogInformation("Code generation {GenerationId}: Using instruction file name: {InstructionFileName}, selected model: {SelectedModel}",
                    generationId, instructionFileName, selectedModel);

                // Double-check the mapping
                if (instructionFileName == "CodeGenerator_qwen3-coder")
                {
                    _logger.LogWarning("Code generation {GenerationId}: WARNING - instructionFileName is still using lowercase version!", generationId);
                    instructionFileName = "CodeGenerator_Qwen3Coder";
                    _logger.LogInformation("Code generation {GenerationId}: Corrected instruction file name to: {InstructionFileName}", generationId, instructionFileName);
                }

                var instructionContent = await _instructionService.GetInstructionAsync(instructionFileName, cancellationToken);

                if (!instructionContent.IsValid)
                {
                    _logger.LogError("Code generation {GenerationId} failed: Invalid instruction content - {ValidationMessage}",
                        generationId, instructionContent.ValidationMessage);
                    response.Status = CodeGenerationStatus.Failed;
                    throw new InvalidOperationException($"Failed to load valid instructions: {instructionContent.ValidationMessage}");
                }

                // Generate tests first (TDD approach)
                response.Status = CodeGenerationStatus.GeneratingTests;
                var testFiles = await _testGenerator.GenerateTestFilesAsync(instructionContent.Content, context, selectedModel, cancellationToken);

                // Generate implementation code
                response.Status = CodeGenerationStatus.GeneratingCode;
                var codeFiles = await _implementationGenerator.GenerateImplementationAsync(instructionContent.Content, context, testFiles, selectedModel, cancellationToken);

                // Validate generated code quality
                response.Status = CodeGenerationStatus.ValidatingOutput;
                var allFiles = testFiles.Concat(codeFiles).ToList();
                await _codeValidator.ValidateGeneratedCodeAsync(allFiles, cancellationToken);

                // Organize files by project structure
                var organizedFiles = _fileOrganizer.OrganizeGeneratedFiles(allFiles);

                // Submit for review
                _logger.LogDebug("Submitting AI response for review in code generation {GenerationId}", generationId);
                var correlationId = Guid.NewGuid().ToString();
                var reviewRequest = new SubmitReviewRequest
                {
                    ServiceName = "CodeGeneration",
                    Content = _fileOrganizer.SerializeCodeArtifacts(organizedFiles),
                    CorrelationId = correlationId,
                    PipelineStage = "Implementation",
                    OriginalRequest = null, // Not applicable for code generation
                    AIResponse = null, // Not applicable for code generation
                    Metadata = new Dictionary<string, object>
                    {
                        { "GenerationId", generationId },
                        { "StoryGenerationId", request.StoryGenerationId }
                    }
                };

                var reviewResponse = await _reviewService.SubmitForReviewAsync(reviewRequest, cancellationToken);

                // Update response with results
                response.GeneratedFiles = organizedFiles.Where(f => f.FileType != "Test").ToList() ?? new List<CodeArtifact>();
                response.TestFiles = organizedFiles.Where(f => f.FileType == "Test").ToList() ?? new List<CodeArtifact>();
                response.ReviewId = reviewResponse.ReviewId;
                response.Status = CodeGenerationStatus.PendingReview;

                _logger.LogInformation("Code generation {GenerationId} completed successfully. Review ID: {ReviewId}",
                    generationId, reviewResponse.ReviewId);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Code generation {GenerationId} failed with exception", generationId);
                if (_generationResults.TryGetValue(generationId, out var response))
                {
                    response.Status = CodeGenerationStatus.Failed;
                }
                throw;
            }
        }

        public async Task<CodeGenerationStatus> GetStatusAsync(Guid codeGenerationId, CancellationToken cancellationToken = default)
        {
            if (_generationResults.TryGetValue(codeGenerationId, out var result))
            {
                return result.Status;
            }

            // If we don't have the status in memory, it might have been cleaned up
            // In a production system, we would check a persistent store
            return CodeGenerationStatus.Failed;
        }

        public async Task<CodeArtifactsResult> GetGeneratedCodeAsync(Guid codeGenerationId, CancellationToken cancellationToken = default)
        {
            if (_generationResults.TryGetValue(codeGenerationId, out var result))
            {
                var allFiles = new List<CodeArtifact>();
                if (result.GeneratedFiles != null)
                    allFiles.AddRange(result.GeneratedFiles);
                if (result.TestFiles != null)
                    allFiles.AddRange(result.TestFiles);

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
                    GeneratedAt = result.CreatedAt,
                    PackageName = $"GeneratedCode_{codeGenerationId}",
                    TotalSizeBytes = totalSize,
                    FileCount = allFiles.Count,
                    FileTypes = fileTypes
                };
            }

            // If we don't have the result in memory, it might have been cleaned up
            // In a production system, we would check a persistent store
            return new CodeArtifactsResult
            {
                GenerationId = codeGenerationId,
                Artifacts = new List<CodeArtifact>()
            };
        }

        public async Task<bool> CanGenerateCodeAsync(
            Guid storyGenerationId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Check that stories exist and are approved by checking the status
                var status = await _storyGenerationService.GetGenerationStatusAsync(storyGenerationId, cancellationToken);
                return status == AIProjectOrchestrator.Domain.Models.Stories.StoryGenerationStatus.Approved;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if code can be generated for story generation {StoryGenerationId}", storyGenerationId);
                return false;
            }
        }

        public async Task<byte[]?> GetGeneratedFilesZipAsync(
            Guid generationId,
            CancellationToken cancellationToken = default)
        {
            if (!_generationResults.TryGetValue(generationId, out var result) || result.Status != CodeGenerationStatus.Approved)
                return null;

            return await _fileOrganizer.GetGeneratedFilesZipAsync(generationId, result.GeneratedFiles ?? new List<CodeArtifact>(), result.TestFiles ?? new List<CodeArtifact>(), cancellationToken);
        }
    }
}
