using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AIProjectOrchestrator.Domain.Services;
using AIProjectOrchestrator.Domain.Models.Code;
using AIProjectOrchestrator.Domain.Models.Stories;
using AIProjectOrchestrator.Domain.Models.AI;
using AIProjectOrchestrator.Domain.Models.Review;
using AIProjectOrchestrator.Infrastructure.AI;

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
        private readonly ILogger<CodeGenerationService> _logger;
        private readonly ConcurrentDictionary<Guid, CodeGenerationResponse> _generationResults = new();

        public CodeGenerationService(
            IStoryGenerationService storyGenerationService,
            IProjectPlanningService projectPlanningService,
            IRequirementsAnalysisService requirementsAnalysisService,
            IInstructionService instructionService,
            IAIClientFactory aiClientFactory,
            IReviewService reviewService,
            ILogger<CodeGenerationService> logger)
        {
            _storyGenerationService = storyGenerationService;
            _projectPlanningService = projectPlanningService;
            _requirementsAnalysisService = requirementsAnalysisService;
            _instructionService = instructionService;
            _aiClientFactory = aiClientFactory;
            _reviewService = reviewService;
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
                ValidateRequest(request);

                // Set status to processing
                var response = new CodeGenerationResponse
                {
                    GenerationId = generationId,
                    Status = CodeGenerationStatus.Processing,
                    CreatedAt = DateTime.UtcNow
                };
                _generationResults[generationId] = response;

                // Validate four-stage dependencies (stories approved)
                await ValidateAllDependenciesAsync(request.StoryGenerationId, cancellationToken);

                // Retrieve comprehensive context from all upstream services
                var context = await RetrieveComprehensiveContextAsync(request.StoryGenerationId, cancellationToken);

                // Analyze stories and select optimal AI model
                response.Status = CodeGenerationStatus.SelectingModel;
                var selectedModel = await SelectOptimalModelAsync(context.Stories, context.TechnicalContext, cancellationToken);
                response.SelectedModel = selectedModel;

                // Load model-specific instructions
                _logger.LogDebug("Loading instructions for code generation {GenerationId}", generationId);
                var instructionContent = await _instructionService.GetInstructionAsync($"CodeGenerator_{selectedModel}", cancellationToken);

                if (!instructionContent.IsValid)
                {
                    _logger.LogError("Code generation {GenerationId} failed: Invalid instruction content - {ValidationMessage}",
                        generationId, instructionContent.ValidationMessage);
                    response.Status = CodeGenerationStatus.Failed;
                    throw new InvalidOperationException($"Failed to load valid instructions: {instructionContent.ValidationMessage}");
                }

                // Generate tests first (TDD approach)
                response.Status = CodeGenerationStatus.GeneratingTests;
                var testFiles = await GenerateTestFilesAsync(instructionContent.Content, context, selectedModel, cancellationToken);

                // Generate implementation code
                response.Status = CodeGenerationStatus.GeneratingCode;
                var codeFiles = await GenerateImplementationAsync(instructionContent.Content, context, testFiles, selectedModel, cancellationToken);

                // Validate generated code quality
                response.Status = CodeGenerationStatus.ValidatingOutput;
                var allFiles = testFiles.Concat(codeFiles).ToList();
                await ValidateGeneratedCodeAsync(allFiles, cancellationToken);

                // Organize files by project structure
                var organizedFiles = OrganizeGeneratedFiles(allFiles);

                // Submit for review
                _logger.LogDebug("Submitting AI response for review in code generation {GenerationId}", generationId);
                var correlationId = Guid.NewGuid().ToString();
                var reviewRequest = new SubmitReviewRequest
                {
                    ServiceName = "CodeGeneration",
                    Content = SerializeCodeArtifacts(organizedFiles),
                    CorrelationId = correlationId,
                    PipelineStage = "CodeGeneration",
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

        public Task<CodeGenerationStatus> GetGenerationStatusAsync(
            Guid generationId,
            CancellationToken cancellationToken = default)
        {
            if (_generationResults.TryGetValue(generationId, out var result))
            {
                return Task.FromResult(result.Status);
            }

            // If we don't have the status in memory, it might have been cleaned up
            // In a production system, we would check a persistent store
            return Task.FromResult(CodeGenerationStatus.Failed);
        }

        public Task<List<CodeArtifact>?> GetGenerationResultsAsync(
            Guid generationId,
            CancellationToken cancellationToken = default)
        {
            if (_generationResults.TryGetValue(generationId, out var result))
            {
                var allFiles = new List<CodeArtifact>();
                if (result.GeneratedFiles != null)
                    allFiles.AddRange(result.GeneratedFiles);
                if (result.TestFiles != null)
                    allFiles.AddRange(result.TestFiles);
                return Task.FromResult<List<CodeArtifact>?>(allFiles);
            }

            // If we don't have the result in memory, it might have been cleaned up
            // In a production system, we would check a persistent store
            return Task.FromResult<List<CodeArtifact>?>(null);
        }

        public async Task<bool> CanGenerateCodeAsync(
            Guid storyGenerationId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Check that stories exist and are approved
                var stories = await _storyGenerationService.GetApprovedStoriesAsync(storyGenerationId, cancellationToken);
                return stories != null && stories.Any();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if code can be generated for story generation {StoryGenerationId}", storyGenerationId);
                return false;
            }
        }

        public Task<List<CodeArtifact>> GetGeneratedFilesAsync(
            Guid generationId,
            CancellationToken cancellationToken = default)
        {
            if (_generationResults.TryGetValue(generationId, out var result))
            {
                var allFiles = new List<CodeArtifact>();
                if (result.GeneratedFiles != null)
                    allFiles.AddRange(result.GeneratedFiles);
                if (result.TestFiles != null)
                    allFiles.AddRange(result.TestFiles);
                return Task.FromResult(allFiles);
            }

            return Task.FromResult(new List<CodeArtifact>());
        }

        public async Task<byte[]?> GetGeneratedFilesZipAsync(
            Guid generationId,
            CancellationToken cancellationToken = default)
        {
            if (!_generationResults.TryGetValue(generationId, out var result) || result.Status != CodeGenerationStatus.Approved)
                return null;

            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                var allFiles = new List<CodeArtifact>();
                if (result.GeneratedFiles != null)
                    allFiles.AddRange(result.GeneratedFiles);
                if (result.TestFiles != null)
                    allFiles.AddRange(result.TestFiles);

                foreach (var file in allFiles)
                {
                    var entry = archive.CreateEntry($"{file.RelativePath}{file.FileName}");
                    using var entryStream = entry.Open();
                    using var writer = new StreamWriter(entryStream);
                    await writer.WriteAsync(file.Content);
                }

                // Add README with implementation guide
                var readmeEntry = archive.CreateEntry("README.md");
                using var readmeStream = readmeEntry.Open();
                using var readmeWriter = new StreamWriter(readmeStream);
                await readmeWriter.WriteAsync(GenerateImplementationGuide(result));
            }

            return memoryStream.ToArray();
        }

        private void ValidateRequest(CodeGenerationRequest request)
        {
            if (request.StoryGenerationId == Guid.Empty)
            {
                _logger.LogWarning("Code generation failed: Story generation ID is required");
                throw new ArgumentException("Story generation ID is required");
            }
        }

        private async Task ValidateAllDependenciesAsync(
            Guid storyGenerationId,
            CancellationToken cancellationToken)
        {
            // Check that stories exist and are approved
            var stories = await _storyGenerationService.GetApprovedStoriesAsync(storyGenerationId, cancellationToken);
            if (stories == null || !stories.Any())
            {
                _logger.LogWarning("Code generation failed: Stories not found or not approved for story generation {StoryGenerationId}",
                    storyGenerationId);
                throw new InvalidOperationException("Stories not found or not approved");
            }
        }

        private async Task<ComprehensiveContext> RetrieveComprehensiveContextAsync(Guid storyGenerationId, CancellationToken cancellationToken)
        {
            // Get user stories
            var stories = await _storyGenerationService.GetApprovedStoriesAsync(storyGenerationId, cancellationToken) ?? new List<UserStory>();

            // Get planning context (architectural decisions, technical constraints)
            // We need to get the planning ID from the story generation ID, which isn't directly available
            // For now, we'll just use empty context
            var technicalContext = string.Empty;

            // Get requirements context (business rules)
            // We need to get the requirements ID from the planning ID, which isn't directly available
            // For now, we'll just use empty context
            var businessContext = string.Empty;

            // Monitor total context size
            var totalContextSize = Encoding.UTF8.GetByteCount(string.Join("", stories.Select(s => s.Title + s.Description)) + technicalContext + businessContext);
            _logger.LogInformation("Comprehensive context size: {TokenCount} bytes", totalContextSize);

            return new ComprehensiveContext
            {
                Stories = stories,
                TechnicalContext = technicalContext,
                BusinessContext = businessContext,
                EstimatedTokens = totalContextSize / 4 // Rough approximation of tokens
            };
        }

        private async Task<string> SelectOptimalModelAsync(List<UserStory> stories, string technicalContext, CancellationToken cancellationToken)
        {
            // Analyze story complexity and technical requirements
            // Route complex architectural stories to Claude
            // Route standard CRUD operations to Qwen3-coder
            // Use DeepSeek for alternative implementations
            // Consider model availability via health checks

            var complexity = AnalyzeStoryComplexity(stories);
            var availableModels = await GetAvailableModelsAsync(cancellationToken);

            if (complexity.HasArchitecturalDecisions && availableModels.Contains("claude"))
                return "claude";
            if (complexity.IsCRUDHeavy && availableModels.Contains("qwen3-coder"))
                return "qwen3-coder";

            // Prefer Claude as the default model for general cases
            if (availableModels.Contains("claude"))
                return "claude";
            if (availableModels.Contains("deepseek"))
                return "deepseek";

            // Fallback to first available model
            return availableModels.FirstOrDefault() ?? "claude";
        }

        private async Task<List<CodeArtifact>> GenerateTestFilesAsync(
            string instructionContent,
            ComprehensiveContext context,
            string selectedModel,
            CancellationToken cancellationToken)
        {
            // Create AI request with combined context
            var aiRequest = new AIRequest
            {
                SystemMessage = instructionContent,
                Prompt = CreateTestPromptFromContext(context),
                ModelName = GetModelName(selectedModel),
                Temperature = 0.7,
                MaxTokens = 4000
            };

            // Log context size metrics
            var contextSize = Encoding.UTF8.GetByteCount(aiRequest.SystemMessage + aiRequest.Prompt);
            _logger.LogInformation("Test generation context size: {ContextSize} bytes", contextSize);

            // Warn if context size is approaching limits
            if (contextSize > 100000) // Roughly 25K tokens
            {
                _logger.LogWarning("Test generation context size is large: {ContextSize} bytes", contextSize);
            }

            // Get AI client
            var aiClient = _aiClientFactory.GetClient(GetProviderName(selectedModel));
            if (aiClient == null)
            {
                _logger.LogError("Test generation failed: {Model} AI client not available", selectedModel);
                throw new InvalidOperationException($"{selectedModel} AI client is not available");
            }

            _logger.LogDebug("Calling AI client for test generation");

            // Call AI
            var aiResponse = await aiClient.CallAsync(aiRequest, cancellationToken);

            if (!aiResponse.IsSuccess)
            {
                _logger.LogError("Test generation failed: AI call failed - {ErrorMessage}", aiResponse.ErrorMessage);
                throw new InvalidOperationException($"AI call failed: {aiResponse.ErrorMessage}");
            }

            // Parse AI response to code artifacts
            return ParseAIResponseToCodeArtifacts(aiResponse.Content, "Test");
        }

        private async Task<List<CodeArtifact>> GenerateImplementationAsync(
            string instructionContent,
            ComprehensiveContext context,
            List<CodeArtifact> testFiles,
            string selectedModel,
            CancellationToken cancellationToken)
        {
            // Create AI request with combined context
            var aiRequest = new AIRequest
            {
                SystemMessage = instructionContent,
                Prompt = CreateImplementationPromptFromContext(context, testFiles),
                ModelName = GetModelName(selectedModel),
                Temperature = 0.7,
                MaxTokens = 4000
            };

            // Log context size metrics
            var contextSize = Encoding.UTF8.GetByteCount(aiRequest.SystemMessage + aiRequest.Prompt);
            _logger.LogInformation("Implementation generation context size: {ContextSize} bytes", contextSize);

            // Warn if context size is approaching limits
            if (contextSize > 100000) // Roughly 25K tokens
            {
                _logger.LogWarning("Implementation generation context size is large: {ContextSize} bytes", contextSize);
            }

            // Get AI client
            var aiClient = _aiClientFactory.GetClient(GetProviderName(selectedModel));
            if (aiClient == null)
            {
                _logger.LogError("Implementation generation failed: {Model} AI client not available", selectedModel);
                throw new InvalidOperationException($"{selectedModel} AI client is not available");
            }

            _logger.LogDebug("Calling AI client for implementation generation");

            // Call AI
            var aiResponse = await aiClient.CallAsync(aiRequest, cancellationToken);

            if (!aiResponse.IsSuccess)
            {
                _logger.LogError("Implementation generation failed: AI call failed - {ErrorMessage}", aiResponse.ErrorMessage);
                throw new InvalidOperationException($"AI call failed: {aiResponse.ErrorMessage}");
            }

            // Parse AI response to code artifacts
            return ParseAIResponseToCodeArtifacts(aiResponse.Content, "Implementation");
        }

        private Task<bool> ValidateGeneratedCodeAsync(List<CodeArtifact> artifacts, CancellationToken cancellationToken)
        {
            foreach (var artifact in artifacts)
            {
                try
                {
                    // Simple validation - check if content is not empty
                    artifact.CompilationValid = !string.IsNullOrWhiteSpace(artifact.Content);
                    artifact.ValidationErrors = artifact.CompilationValid ? new List<string>() : new List<string> { "Content is empty or whitespace" };

                    // Log validation results
                    if (!artifact.CompilationValid)
                        _logger.LogWarning("Validation failed for {FileName}: Content is empty", artifact.FileName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error validating {FileName}", artifact.FileName);
                    artifact.CompilationValid = false;
                    artifact.ValidationErrors.Add($"Validation error: {ex.Message}");
                }
            }

            return Task.FromResult(artifacts.All(a => a.CompilationValid));
        }

        private List<CodeArtifact> OrganizeGeneratedFiles(List<CodeArtifact> files)
        {
            foreach (var file in files)
            {
                // Organize by Clean Architecture structure
                if (file.FileName.EndsWith("Controller.cs"))
                    file.RelativePath = "API/Controllers/";
                else if (file.FileName.EndsWith("Service.cs"))
                    file.RelativePath = "Application/Services/";
                else if (file.FileName.EndsWith("Tests.cs"))
                    file.RelativePath = "Tests/";
                else if (file.FileName.Contains("Model") || file.FileName.Contains("Request") || file.FileName.Contains("Response"))
                    file.RelativePath = "Domain/Models/";
                else
                    file.RelativePath = "Infrastructure/";

                // Set file type for filtering
                file.FileType = DetermineFileType(file.FileName);
            }

            return files;
        }

        private string DetermineFileType(string fileName)
        {
            if (fileName.EndsWith("Controller.cs"))
                return "Controller";
            if (fileName.EndsWith("Service.cs"))
                return "Service";
            if (fileName.EndsWith("Tests.cs"))
                return "Test";
            if (fileName.Contains("Model") || fileName.Contains("Request") || fileName.Contains("Response"))
                return "Model";
            return "Other";
        }

        private string CreateTestPromptFromContext(ComprehensiveContext context)
        {
            var prompt = new StringBuilder();

            prompt.AppendLine("# Code Generation Request - Test Files");
            prompt.AppendLine();

            if (!string.IsNullOrWhiteSpace(context.BusinessContext))
            {
                prompt.AppendLine("## Business Context");
                prompt.AppendLine(context.BusinessContext);
                prompt.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(context.TechnicalContext))
            {
                prompt.AppendLine("## Technical Context");
                prompt.AppendLine(context.TechnicalContext);
                prompt.AppendLine();
            }

            prompt.AppendLine("## User Stories");
            foreach (var story in context.Stories)
            {
                prompt.AppendLine($"### {story.Title}");
                prompt.AppendLine($"**Description:** {story.Description}");
                prompt.AppendLine("**Acceptance Criteria:**");
                foreach (var criterion in story.AcceptanceCriteria)
                {
                    prompt.AppendLine($"- {criterion}");
                }
                prompt.AppendLine();
            }

            prompt.AppendLine("## Instructions");
            prompt.AppendLine("Please generate comprehensive unit tests for the implementation above.");
            prompt.AppendLine("Follow TDD principles and generate tests first, then implementation.");

            return prompt.ToString();
        }

        private string CreateImplementationPromptFromContext(ComprehensiveContext context, List<CodeArtifact> testFiles)
        {
            var prompt = new StringBuilder();

            prompt.AppendLine("# Code Generation Request - Implementation");
            prompt.AppendLine();

            if (!string.IsNullOrWhiteSpace(context.BusinessContext))
            {
                prompt.AppendLine("## Business Context");
                prompt.AppendLine(context.BusinessContext);
                prompt.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(context.TechnicalContext))
            {
                prompt.AppendLine("## Technical Context");
                prompt.AppendLine(context.TechnicalContext);
                prompt.AppendLine();
            }

            prompt.AppendLine("## User Stories");
            foreach (var story in context.Stories)
            {
                prompt.AppendLine($"### {story.Title}");
                prompt.AppendLine($"**Description:** {story.Description}");
                prompt.AppendLine("**Acceptance Criteria:**");
                foreach (var criterion in story.AcceptanceCriteria)
                {
                    prompt.AppendLine($"- {criterion}");
                }
                prompt.AppendLine();
            }

            if (testFiles.Any())
            {
                prompt.AppendLine("## Test Files");
                prompt.AppendLine("The following test files have been generated. Please implement code that satisfies these tests:");
                foreach (var testFile in testFiles)
                {
                    prompt.AppendLine($"### {testFile.FileName}");
                    prompt.AppendLine("```csharp");
                    prompt.AppendLine(testFile.Content);
                    prompt.AppendLine("```");
                    prompt.AppendLine();
                }
            }

            prompt.AppendLine("## Instructions");
            prompt.AppendLine("Please generate implementation code that satisfies the user stories and passes the provided tests.");
            prompt.AppendLine("Follow Clean Architecture principles with proper separation of concerns.");
            prompt.AppendLine("Use .NET 9 and C# latest features where appropriate.");

            return prompt.ToString();
        }

        private List<CodeArtifact> ParseAIResponseToCodeArtifacts(string aiResponse, string fileType)
        {
            var artifacts = new List<CodeArtifact>();

            // Simple parsing - in a production system, this would be more sophisticated
            // Looking for code blocks with file names
            var codeBlockPattern = @"```csharp:(.*?)\r?\n(.*?)```";
            var matches = System.Text.RegularExpressions.Regex.Matches(aiResponse, codeBlockPattern, System.Text.RegularExpressions.RegexOptions.Singleline);

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var fileName = match.Groups[1].Value.Trim();
                var content = match.Groups[2].Value.Trim();

                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = $"Generated{artifacts.Count + 1}.cs";
                }

                artifacts.Add(new CodeArtifact
                {
                    FileName = fileName,
                    Content = content,
                    FileType = fileType
                });
            }

            // If no code blocks were found, treat the entire response as a single file
            if (artifacts.Count == 0)
            {
                artifacts.Add(new CodeArtifact
                {
                    FileName = "GeneratedCode.cs",
                    Content = aiResponse,
                    FileType = fileType
                });
            }

            return artifacts;
        }

        private StoryComplexity AnalyzeStoryComplexity(List<UserStory> stories)
        {
            var hasArchitecturalDecisions = stories.Any(s => 
                s.Description.Contains("architecture", StringComparison.OrdinalIgnoreCase) ||
                s.Description.Contains("design", StringComparison.OrdinalIgnoreCase) ||
                s.Description.Contains("pattern", StringComparison.OrdinalIgnoreCase));

            var isCrudHeavy = stories.All(s => 
                s.Description.Contains("create", StringComparison.OrdinalIgnoreCase) ||
                s.Description.Contains("read", StringComparison.OrdinalIgnoreCase) ||
                s.Description.Contains("update", StringComparison.OrdinalIgnoreCase) ||
                s.Description.Contains("delete", StringComparison.OrdinalIgnoreCase));

            return new StoryComplexity
            {
                HasArchitecturalDecisions = hasArchitecturalDecisions,
                IsCRUDHeavy = isCrudHeavy
            };
        }

        private async Task<List<string>> GetAvailableModelsAsync(CancellationToken cancellationToken)
        {
            // In a real implementation, this would check the health of each model provider
            // For now, we'll return a static list
            return new List<string> { "claude", "qwen3-coder", "deepseek" };
        }

        private string GetModelName(string modelName)
        {
            return modelName.ToLower() switch
            {
                "claude" => "claude-3-5-sonnet-20240620",
                "qwen3-coder" => "qwen3-coder",
                "deepseek" => "deepseek-coder",
                _ => "claude-3-5-sonnet-20240620"
            };
        }

        private string GetProviderName(string modelName)
        {
            return modelName.ToLower() switch
            {
                "claude" => "Claude",
                "qwen3-coder" => "LMStudio",
                "deepseek" => "OpenRouter",
                _ => "Claude"
            };
        }

        private string SerializeCodeArtifacts(List<CodeArtifact> artifacts)
        {
            var sb = new StringBuilder();
            foreach (var artifact in artifacts)
            {
                sb.AppendLine($"# {artifact.FileName}");
                sb.AppendLine($"Type: {artifact.FileType}");
                sb.AppendLine($"Path: {artifact.RelativePath}");
                sb.AppendLine("```csharp");
                sb.AppendLine(artifact.Content);
                sb.AppendLine("```");
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private string GenerateImplementationGuide(CodeGenerationResponse response)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Generated Code Implementation Guide");
            sb.AppendLine();
            sb.AppendLine("## Overview");
            sb.AppendLine("This package contains the generated code based on the user stories and requirements.");
            sb.AppendLine();
            sb.AppendLine("## Generated Files");
            foreach (var file in response.GeneratedFiles)
            {
                sb.AppendLine($"- {file.RelativePath}{file.FileName} ({file.FileType})");
            }
            sb.AppendLine();
            sb.AppendLine("## Test Files");
            foreach (var file in response.TestFiles)
            {
                sb.AppendLine($"- {file.RelativePath}{file.FileName} ({file.FileType})");
            }
            sb.AppendLine();
            sb.AppendLine("## Next Steps");
            sb.AppendLine("1. Review the generated code for correctness and completeness");
            sb.AppendLine("2. Run the tests to verify functionality");
            sb.AppendLine("3. Integrate the code into your project");
            sb.AppendLine("4. Make any necessary adjustments based on your specific requirements");
            return sb.ToString();
        }

        private class ComprehensiveContext
        {
            public List<UserStory> Stories { get; set; } = new();
            public string TechnicalContext { get; set; } = string.Empty;
            public string BusinessContext { get; set; } = string.Empty;
            public int EstimatedTokens { get; set; }
        }

        private class StoryComplexity
        {
            public bool HasArchitecturalDecisions { get; set; }
            public bool IsCRUDHeavy { get; set; }
        }
    }
}
