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
using AIProjectOrchestrator.Infrastructure.AI;

namespace AIProjectOrchestrator.Application.Services;

public class ImplementationGenerator : IImplementationGenerator
{
    private readonly IAIClientFactory _aiClientFactory;
    private readonly ILogger<ImplementationGenerator> _logger;

    public ImplementationGenerator(IAIClientFactory aiClientFactory, ILogger<ImplementationGenerator> logger)
    {
        _aiClientFactory = aiClientFactory;
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
                FileName = "GeneratedImplementation.cs",
                Content = aiResponse,
                FileType = fileType
            });
        }

        return artifacts;
    }

    private string GetModelName(string modelName)
    {
        return modelName.ToLower() switch
        {
            "claude" => "qwen/qwen3-coder",
            "qwen3-coder" => "qwen/qwen3-coder",
            "deepseek" => "qwen/qwen3-coder", // Use Qwen for all models
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
}