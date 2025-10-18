using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

public class TestGenerator : ITestGenerator
{
    private readonly ITestGenerationAIProvider _aiProvider;
    private readonly ILogger<TestGenerator> _logger;

    public TestGenerator(ITestGenerationAIProvider aiProvider, ILogger<TestGenerator> logger)
    {
        _aiProvider = aiProvider;
        _logger = logger;
    }

    public async Task<List<CodeArtifact>> GenerateTestFilesAsync(
        string instructionContent,
        AIProjectOrchestrator.Domain.Models.ComprehensiveContext context,
        string selectedModel,
        CancellationToken cancellationToken = default)
    {
        // Create AI request with combined context
        var aiRequest = new AIRequest
        {
            SystemMessage = instructionContent,
            Prompt = CreateTestPromptFromContext(context),
            ModelName = selectedModel,
            Temperature = 0.7,
            MaxTokens = 4000
        };

        // Log context size metrics
        var contextSize = Encoding.UTF8.GetByteCount(aiRequest.SystemMessage + aiRequest.Prompt);
        _logger.LogInformation("Test generation context size: {ContextSize} bytes", contextSize);

        // Warn if context size is approaching limits
        if (contextSize > AIConstants.MaxContextSizeBytes) // Roughly 25K tokens
        {
            _logger.LogWarning("Test generation context size is large: {ContextSize} bytes", contextSize);
        }

        _logger.LogDebug("Calling AI provider for test generation");

        // Call AI using the provider
        var aiResponse = await _aiProvider.GenerateContentAsync(aiRequest.Prompt, aiRequest.SystemMessage);

        // Parse AI response to code artifacts
        return ParseAIResponseToCodeArtifacts(aiResponse, "Test");
    }

    private string CreateTestPromptFromContext(AIProjectOrchestrator.Domain.Models.ComprehensiveContext context)
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

    private List<CodeArtifact> ParseAIResponseToCodeArtifacts(string aiResponse, string fileType)
    {
        var artifacts = new List<CodeArtifact>();

        // Simple parsing - in a production system, this would be more sophisticated
        // Looking for code blocks with file names
        var codeBlockPattern = @"```csharp:(.*?)\r?\n(.*?)```";
        var matches = Regex.Matches(aiResponse, codeBlockPattern, RegexOptions.Singleline);

        foreach (Match match in matches)
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
                FileName = "GeneratedTests.cs",
                Content = aiResponse,
                FileType = fileType
            });
        }

        return artifacts;
    }

}