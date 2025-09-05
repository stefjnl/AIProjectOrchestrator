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
using AIProjectOrchestrator.Domain.Models;
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

            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                var allFiles = new List<CodeArtifact>();
                if (result.GeneratedFiles != null)
                    allFiles.AddRange(result.GeneratedFiles);
                if (result.TestFiles != null)
                    allFiles.AddRange(result.TestFiles);

                // Create the required folder structure
                foreach (var file in allFiles)
                {
                    // Create folder structure matching the required pattern
                    string entryPath;
                    if (file.RelativePath.StartsWith("Controllers/"))
                    {
                        entryPath = $"Generated Code Package/API/Controllers/{file.FileName}";
                    }
                    else if (file.RelativePath.StartsWith("Services/"))
                    {
                        if (file.RelativePath.StartsWith("Services/Interfaces/"))
                        {
                            entryPath = $"Generated Code Package/Application/Interfaces/{file.FileName}";
                        }
                        else
                        {
                            entryPath = $"Generated Code Package/Application/Services/{file.FileName}";
                        }
                    }
                    else if (file.RelativePath.StartsWith("Models/"))
                    {
                        entryPath = $"Generated Code Package/Domain/Models/{file.FileName}";
                    }
                    else if (file.RelativePath.StartsWith("Tests/"))
                    {
                        entryPath = $"Generated Code Package/Tests/{file.FileName}";
                    }
                    else
                    {
                        entryPath = $"Generated Code Package/Infrastructure/{file.FileName}";
                    }

                    var entry = archive.CreateEntry(entryPath);
                    using var entryStream = entry.Open();
                    using var writer = new StreamWriter(entryStream);
                    await writer.WriteAsync(file.Content);
                }

                // Add README with implementation guide
                var readmeEntry = archive.CreateEntry("Generated Code Package/README.md");
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

            // Get the planning ID from the story generation
            var planningId = await _storyGenerationService.GetPlanningIdAsync(storyGenerationId, cancellationToken);
            if (!planningId.HasValue)
            {
                _logger.LogWarning("Code generation failed: Planning ID not found for story generation {StoryGenerationId}",
                    storyGenerationId);
                throw new InvalidOperationException("Planning ID not found");
            }

            // Check that project planning is approved
            var planningStatus = await _projectPlanningService.GetPlanningStatusAsync(planningId.Value, cancellationToken);
            if (planningStatus != AIProjectOrchestrator.Domain.Models.ProjectPlanningStatus.Approved)
            {
                _logger.LogWarning("Code generation failed: Project planning {PlanningId} is not approved",
                    planningId.Value);
                throw new InvalidOperationException("Project planning is not approved");
            }

            // Get the requirements analysis ID from the planning
            var requirementsAnalysisId = await _projectPlanningService.GetRequirementsAnalysisIdAsync(
                planningId.Value, cancellationToken);
            if (!requirementsAnalysisId.HasValue)
            {
                _logger.LogWarning("Code generation failed: Requirements analysis ID not found for planning {PlanningId}",
                    planningId.Value);
                throw new InvalidOperationException("Requirements analysis ID not found");
            }

            // Check that requirements analysis is approved
            var requirementsStatus = await _requirementsAnalysisService.GetAnalysisStatusAsync(
                requirementsAnalysisId.Value, cancellationToken);
            if (requirementsStatus != AIProjectOrchestrator.Domain.Models.RequirementsAnalysisStatus.Approved)
            {
                _logger.LogWarning("Code generation failed: Requirements analysis {RequirementsAnalysisId} is not approved",
                    requirementsAnalysisId.Value);
                throw new InvalidOperationException("Requirements analysis is not approved");
            }
        }

        private async Task<ComprehensiveContext> RetrieveComprehensiveContextAsync(Guid storyGenerationId, CancellationToken cancellationToken)
        {
            // Get user stories
            var stories = await _storyGenerationService.GetApprovedStoriesAsync(storyGenerationId, cancellationToken) ?? new List<UserStory>();

            // Get the planning ID from the story generation
            var planningId = await _storyGenerationService.GetPlanningIdAsync(storyGenerationId, cancellationToken);

            string technicalContext = string.Empty;
            string businessContext = string.Empty;

            if (planningId.HasValue)
            {
                // Get planning context (architectural decisions, technical constraints)
                technicalContext = await _projectPlanningService.GetTechnicalContextAsync(planningId.Value, cancellationToken) ?? string.Empty;

                // Get the requirements analysis ID from the planning
                var requirementsAnalysisId = await _projectPlanningService.GetRequirementsAnalysisIdAsync(
                    planningId.Value, cancellationToken);

                if (requirementsAnalysisId.HasValue)
                {
                    // Get requirements context (business rules)
                    businessContext = await _requirementsAnalysisService.GetBusinessContextAsync(
                        requirementsAnalysisId.Value, cancellationToken) ?? string.Empty;
                }
            }

            // Monitor total context size
            var totalContextSize = Encoding.UTF8.GetByteCount(
                string.Join("", stories.Select(s => s.Title + s.Description)) + 
                technicalContext + 
                businessContext);
                
            _logger.LogInformation("Comprehensive context size: {TokenCount} bytes", totalContextSize);

            // Apply token optimization if context is too large
            if (totalContextSize > 150000) // Roughly 37.5K tokens
            {
                _logger.LogWarning("Context size is large ({TokenCount} bytes), applying optimization", totalContextSize);
                stories = OptimizeStoriesContext(stories);
                technicalContext = OptimizeTechnicalContext(technicalContext);
                businessContext = OptimizeBusinessContext(businessContext);
                
                // Recalculate size after optimization
                totalContextSize = Encoding.UTF8.GetByteCount(
                    string.Join("", stories.Select(s => s.Title + s.Description)) + 
                    technicalContext + 
                    businessContext);
                    
                _logger.LogInformation("Context size after optimization: {TokenCount} bytes", totalContextSize);
            }

            return new ComprehensiveContext
            {
                Stories = stories,
                TechnicalContext = technicalContext,
                BusinessContext = businessContext,
                EstimatedTokens = totalContextSize / 4 // Rough approximation of tokens
            };
        }

        private List<UserStory> OptimizeStoriesContext(List<UserStory> stories)
        {
            // Filter and prioritize stories based on relevance
            // For now, we'll just truncate descriptions if they're too long
            var optimizedStories = new List<UserStory>();
            
            foreach (var story in stories)
            {
                var optimizedStory = new UserStory
                {
                    Title = story.Title,
                    Description = story.Description.Length > 500 ? story.Description.Substring(0, 500) + "..." : story.Description,
                    AcceptanceCriteria = story.AcceptanceCriteria.Take(5).ToList(), // Limit to 5 criteria
                    Priority = story.Priority,
                    EstimatedComplexity = story.EstimatedComplexity
                };
                
                optimizedStories.Add(optimizedStory);
            }
            
            return optimizedStories;
        }

        private string OptimizeTechnicalContext(string technicalContext)
        {
            // Compress technical context by removing redundant information
            if (string.IsNullOrEmpty(technicalContext))
                return technicalContext;
                
            // For now, we'll just truncate if it's too long
            if (technicalContext.Length > 2000)
            {
                return technicalContext.Substring(0, 2000) + "...";
            }
            
            return technicalContext;
        }

        private string OptimizeBusinessContext(string businessContext)
        {
            // Compress business context by removing redundant information
            if (string.IsNullOrEmpty(businessContext))
                return businessContext;
                
            // For now, we'll just truncate if it's too long
            if (businessContext.Length > 2000)
            {
                return businessContext.Substring(0, 2000) + "...";
            }
            
            return businessContext;
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

            // Check model health status
            var modelHealth = await CheckModelHealthAsync(cancellationToken);

            // Route based on story characteristics
            if (complexity.HasArchitecturalDecisions && availableModels.Contains("claude") && modelHealth["claude"])
                return "claude";
                
            if (complexity.IsCRUDHeavy && availableModels.Contains("qwen3-coder") && modelHealth["qwen3-coder"])
                return "qwen3-coder";
                
            if (complexity.IsHighComplexity && availableModels.Contains("deepseek") && modelHealth["deepseek"])
                return "deepseek";

            // If we need an alternative implementation for validation
            if (stories.Any(s => s.Description.Contains("validate", StringComparison.OrdinalIgnoreCase) || 
                               s.Description.Contains("compare", StringComparison.OrdinalIgnoreCase)) &&
                availableModels.Contains("deepseek") && modelHealth["deepseek"])
                return "deepseek";

            // Prefer Claude as the default model for general cases if it's healthy
            if (availableModels.Contains("claude") && modelHealth["claude"])
                return "claude";
                
            // Fallback to other models based on health status
            if (availableModels.Contains("qwen3-coder") && modelHealth["qwen3-coder"])
                return "qwen3-coder";
                
            if (availableModels.Contains("deepseek") && modelHealth["deepseek"])
                return "deepseek";

            // Fallback to first available healthy model
            foreach (var model in availableModels)
            {
                if (modelHealth.ContainsKey(model) && modelHealth[model])
                    return model;
            }

            // Final fallback to Claude if no healthy models found
            return "claude";
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

        private async Task<bool> ValidateGeneratedCodeAsync(List<CodeArtifact> artifacts, CancellationToken cancellationToken)
        {
            var allValid = true;
            
            foreach (var artifact in artifacts)
            {
                try
                {
                    // Simple validation - check if content is not empty
                    artifact.CompilationValid = !string.IsNullOrWhiteSpace(artifact.Content);
                    artifact.ValidationErrors = artifact.CompilationValid ? new List<string>() : new List<string> { "Content is empty or whitespace" };

                    // Log validation results
                    if (!artifact.CompilationValid)
                    {
                        _logger.LogWarning("Validation failed for {FileName}: Content is empty", artifact.FileName);
                        allValid = false;
                        continue;
                    }

                    // Perform basic syntax validation for C# files
                    if (artifact.FileName.EndsWith(".cs"))
                    {
                        var syntaxErrors = await ValidateCSharpSyntaxAsync(artifact.Content, cancellationToken);
                        if (syntaxErrors.Any())
                        {
                            artifact.ValidationErrors.AddRange(syntaxErrors);
                            artifact.CompilationValid = false;
                            allValid = false;
                            _logger.LogWarning("Syntax validation failed for {FileName}: {ErrorCount} errors found", 
                                artifact.FileName, syntaxErrors.Count);
                        }
                    }

                    // Check for test coverage indicators in test files
                    if (artifact.FileType == "Test")
                    {
                        var coverageInfo = AnalyzeTestCoverage(artifact.Content);
                        if (coverageInfo.MissingTests.Any())
                        {
                            artifact.ValidationErrors.Add($"Potential missing tests: {string.Join(", ", coverageInfo.MissingTests)}");
                            _logger.LogInformation("Test coverage analysis for {FileName}: {MissingCount} potential gaps identified", 
                                artifact.FileName, coverageInfo.MissingTests.Count);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error validating {FileName}", artifact.FileName);
                    artifact.CompilationValid = false;
                    artifact.ValidationErrors.Add($"Validation error: {ex.Message}");
                    allValid = false;
                }
            }

            return allValid;
        }

        private async Task<List<string>> ValidateCSharpSyntaxAsync(string codeContent, CancellationToken cancellationToken)
        {
            // In a real implementation, this would use Roslyn or similar to validate C# syntax
            // For now, we'll do basic checks
            var errors = new List<string>();
            
            // Check for basic C# structure
            if (!codeContent.Contains("using System") && !codeContent.Contains("namespace "))
            {
                errors.Add("Missing basic C# structure (using statements or namespace declaration)");
            }
            
            // Check for balanced braces
            var openBraces = codeContent.Count(c => c == '{');
            var closeBraces = codeContent.Count(c => c == '}');
            if (openBraces != closeBraces)
            {
                errors.Add($"Unbalanced braces: {openBraces} opening, {closeBraces} closing");
            }
            
            // Check for basic class structure
            if (codeContent.Contains("class ") && !codeContent.Contains("public") && !codeContent.Contains("private"))
            {
                // This is a very basic check - in a real implementation we'd use proper parsing
            }
            
            return errors;
        }

        private TestCoverageInfo AnalyzeTestCoverage(string testContent)
        {
            // In a real implementation, this would analyze the test content to determine coverage
            // For now, we'll just return a basic structure
            var coverageInfo = new TestCoverageInfo();
            
            // Simple heuristic to identify potential missing tests
            var potentialMissing = new List<string>();
            
            if (!testContent.Contains("TestMethod") && !testContent.Contains("Fact") && !testContent.Contains("Theory"))
            {
                potentialMissing.Add("No test methods identified");
            }
            
            if (!testContent.Contains("Assert"))
            {
                potentialMissing.Add("No assertions found");
            }
            
            coverageInfo.MissingTests = potentialMissing;
            return coverageInfo;
        }

        private List<CodeArtifact> OrganizeGeneratedFiles(List<CodeArtifact> files)
        {
            foreach (var file in files)
            {
                // Organize by Clean Architecture structure matching the required pattern
                if (file.FileName.EndsWith("Controller.cs"))
                    file.RelativePath = "Controllers/";
                else if (file.FileName.EndsWith("Service.cs") && !file.FileName.StartsWith("I"))
                    file.RelativePath = "Services/";
                else if (file.FileName.StartsWith("I") && file.FileName.EndsWith("Service.cs"))
                    file.RelativePath = "Services/Interfaces/";
                else if (file.FileName.EndsWith("Tests.cs"))
                    file.RelativePath = "Tests/";
                else if (file.FileName.Contains("Model") || file.FileName.Contains("Request") || file.FileName.Contains("Response"))
                    file.RelativePath = "Models/";
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
                s.Description.Contains("pattern", StringComparison.OrdinalIgnoreCase) ||
                s.Description.Contains("framework", StringComparison.OrdinalIgnoreCase) ||
                s.Description.Contains("integration", StringComparison.OrdinalIgnoreCase));

            var isCrudHeavy = stories.All(s => 
                (s.Description.Contains("create", StringComparison.OrdinalIgnoreCase) ||
                s.Description.Contains("read", StringComparison.OrdinalIgnoreCase) ||
                s.Description.Contains("update", StringComparison.OrdinalIgnoreCase) ||
                s.Description.Contains("delete", StringComparison.OrdinalIgnoreCase) ||
                s.Description.Contains("list", StringComparison.OrdinalIgnoreCase) ||
                s.Description.Contains("get", StringComparison.OrdinalIgnoreCase) ||
                s.Description.Contains("add", StringComparison.OrdinalIgnoreCase) ||
                s.Description.Contains("edit", StringComparison.OrdinalIgnoreCase) ||
                s.Description.Contains("remove", StringComparison.OrdinalIgnoreCase)));

            // Analyze story complexity based on acceptance criteria count and word count
            var totalCriteria = stories.Sum(s => s.AcceptanceCriteria.Count);
            var totalWords = stories.Sum(s => s.Description.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
            
            var isHighComplexity = totalCriteria > stories.Count * 3 || totalWords > stories.Count * 50;
            var isLowComplexity = totalCriteria <= stories.Count * 2 && totalWords <= stories.Count * 20;

            return new StoryComplexity
            {
                HasArchitecturalDecisions = hasArchitecturalDecisions,
                IsCRUDHeavy = isCrudHeavy,
                IsHighComplexity = isHighComplexity,
                IsLowComplexity = isLowComplexity
            };
        }

        private async Task<List<string>> GetAvailableModelsAsync(CancellationToken cancellationToken)
        {
            // In a real implementation, this would check the health of each model provider
            // For now, we'll return a static list
            return new List<string> { "claude", "qwen3-coder", "deepseek" };
        }

        private async Task<Dictionary<string, bool>> CheckModelHealthAsync(CancellationToken cancellationToken)
        {
            // In a real implementation, this would check the actual health of each model provider
            // For now, we'll assume all models are healthy
            var healthStatus = new Dictionary<string, bool>
            {
                { "claude", true },
                { "qwen3-coder", true },
                { "deepseek", true }
            };

            return healthStatus;
        }

        private string GetModelName(string modelName)
        {
            return modelName.ToLower() switch
            {
                "claude" => "qwen/qwen3-coder",
                "qwen3-coder" => "qwen3-coder",
                "deepseek" => "deepseek-coder",
                _ => "qwen/qwen3-coder"
            };
        }

        private string GetProviderName(string modelName)
        {
            return modelName.ToLower() switch
            {
                "claude" => "OpenRouter", // Route Claude requests to OpenRouter
                "qwen3-coder" => "LMStudio",
                "deepseek" => "OpenRouter",
                _ => "OpenRouter"
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
            public bool IsHighComplexity { get; set; }
            public bool IsLowComplexity { get; set; }
        }

        private class TestCoverageInfo
        {
            public List<string> MissingTests { get; set; } = new List<string>();
            public double EstimatedCoveragePercentage { get; set; } = 0.0;
        }
    }
}
