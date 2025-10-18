using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AIProjectOrchestrator.Domain.Models;
using AIProjectOrchestrator.Domain.Models.Code;
using AIProjectOrchestrator.Domain.Models.AI;
using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Services;
using AIProjectOrchestrator.Domain.Common;
using AIProjectOrchestrator.Infrastructure.AI.Providers;

namespace AIProjectOrchestrator.Application.Services;

public class ImplementationGenerator : IImplementationGenerator
{
    private readonly IImplementationGenerationAIProvider _aiProvider;
    private readonly ICodeArtifactParser _codeArtifactParser;
    private readonly ILogger<ImplementationGenerator> _logger;

    public ImplementationGenerator(
        IImplementationGenerationAIProvider aiProvider,
        ICodeArtifactParser codeArtifactParser,
        ILogger<ImplementationGenerator> logger)
    {
        _aiProvider = aiProvider;
        _codeArtifactParser = codeArtifactParser;
        _logger = logger;
    }

    public async Task<List<CodeArtifact>> GenerateImplementationAsync(
        string instructionContent,
        ComprehensiveContext context,
        List<CodeArtifact> testFiles,
        string selectedModel,
        CancellationToken cancellationToken = default)
    {
        // Create AI request with combined context
        var aiRequest = new AIRequest
        {
            SystemMessage = instructionContent,
            Prompt = CreateImplementationPromptFromContext(context, testFiles),
            ModelName = selectedModel,
            Temperature = 0.7,
            MaxTokens = 4000
        };

        // Log context size metrics
        var contextSize = Encoding.UTF8.GetByteCount(aiRequest.SystemMessage + aiRequest.Prompt);
        _logger.LogInformation("Implementation generation context size: {ContextSize} bytes", contextSize);

        // Warn if context size is approaching limits
        if (contextSize > AIConstants.MaxContextSizeBytes) // Roughly 25K tokens
        {
            _logger.LogWarning("Implementation generation context size is large: {ContextSize} bytes", contextSize);
        }

        _logger.LogDebug("Calling AI provider for implementation generation");

        // Call AI using the provider
        var aiResponse = await _aiProvider.GenerateContentAsync(aiRequest.Prompt, aiRequest.SystemMessage).ConfigureAwait(false);

        // Parse AI response to code artifacts
        return _codeArtifactParser.ParseToCodeArtifacts(aiResponse, "Implementation", "GeneratedImplementation.cs");
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
}