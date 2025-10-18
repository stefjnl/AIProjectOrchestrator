using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Models.Code;
using AIProjectOrchestrator.Domain.Services;
using Microsoft.Extensions.Logging;

namespace AIProjectOrchestrator.Application.Services
{
    /// <summary>
    /// Implementation of code generation orchestration.
    /// Follows Single Responsibility Principle - focuses only on orchestrating the generation workflow.
    /// </summary>
    public class CodeGenerationOrchestrator : ICodeGenerationOrchestrator
    {
        private readonly ITestGenerator _testGenerator;
        private readonly IImplementationGenerator _implementationGenerator;
        private readonly ICodeValidator _codeValidator;
        private readonly IFileOrganizer _fileOrganizer;
        private readonly ILogger<CodeGenerationOrchestrator> _logger;

        public CodeGenerationOrchestrator(
            ITestGenerator testGenerator,
            IImplementationGenerator implementationGenerator,
            ICodeValidator codeValidator,
            IFileOrganizer fileOrganizer,
            ILogger<CodeGenerationOrchestrator> logger)
        {
            _testGenerator = testGenerator;
            _implementationGenerator = implementationGenerator;
            _codeValidator = codeValidator;
            _fileOrganizer = fileOrganizer;
            _logger = logger;
        }

        public async Task<CodeGenerationResult> OrchestrateGenerationAsync(
            CodeGenerationContext context,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting code generation orchestration for generation {GenerationId}",
                context.GenerationId);

            try
            {
                // Step 1: Generate tests first (TDD approach)
                _logger.LogDebug("Generating test files for generation {GenerationId}", context.GenerationId);
                var testFiles = await _testGenerator.GenerateTestFilesAsync(
                    context.InstructionContent,
                    context.ComprehensiveContext,
                    context.SelectedModel,
                    cancellationToken);

                _logger.LogInformation("Generated {TestFileCount} test files for generation {GenerationId}",
                    testFiles.Count, context.GenerationId);

                // Step 2: Generate implementation code
                _logger.LogDebug("Generating implementation code for generation {GenerationId}", context.GenerationId);
                var codeFiles = await _implementationGenerator.GenerateImplementationAsync(
                    context.InstructionContent,
                    context.ComprehensiveContext,
                    testFiles,
                    context.SelectedModel,
                    cancellationToken);

                _logger.LogInformation("Generated {CodeFileCount} implementation files for generation {GenerationId}",
                    codeFiles.Count, context.GenerationId);

                // Step 3: Validate generated code quality
                _logger.LogDebug("Validating generated code for generation {GenerationId}", context.GenerationId);
                var allFiles = testFiles.Concat(codeFiles).ToList();
                await _codeValidator.ValidateGeneratedCodeAsync(allFiles, cancellationToken);

                _logger.LogInformation("Code validation passed for generation {GenerationId}", context.GenerationId);

                // Step 4: Organize files by project structure
                _logger.LogDebug("Organizing files for generation {GenerationId}", context.GenerationId);
                var organizedFiles = _fileOrganizer.OrganizeGeneratedFiles(allFiles);

                _logger.LogInformation("Code generation orchestration completed successfully for generation {GenerationId}. " +
                    "Total files: {TotalFiles} (Tests: {TestCount}, Implementation: {CodeCount})",
                    context.GenerationId, organizedFiles.Count, testFiles.Count, codeFiles.Count);

                return new CodeGenerationResult
                {
                    GenerationId = context.GenerationId,
                    StoryGenerationId = context.StoryGenerationId,
                    GeneratedFiles = codeFiles,
                    TestFiles = testFiles,
                    OrganizedFiles = organizedFiles,
                    SelectedModel = context.SelectedModel,
                    CreatedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Code generation orchestration failed for generation {GenerationId}",
                    context.GenerationId);
                throw;
            }
        }
    }
}
