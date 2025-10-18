using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using AIProjectOrchestrator.Domain.Models.Code;
using AIProjectOrchestrator.Domain.Services;

namespace AIProjectOrchestrator.Application.Services;

/// <summary>
/// Service for parsing AI-generated responses into code artifacts.
/// Extracts code blocks from markdown-formatted responses.
/// </summary>
public class CodeArtifactParser : ICodeArtifactParser
{
    private readonly ILogger<CodeArtifactParser> _logger;
    private const string CodeBlockPattern = @"```csharp:(.*?)\r?\n(.*?)```";

    public CodeArtifactParser(ILogger<CodeArtifactParser> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Parses AI response text into a list of code artifacts.
    /// Looks for markdown code blocks in the format: ```csharp:FileName.cs
    /// </summary>
    /// <param name="aiResponse">The raw AI response containing code blocks</param>
    /// <param name="fileType">The type of file (e.g., "Test", "Implementation")</param>
    /// <param name="defaultFileName">Optional default file name if none is found in the response</param>
    /// <returns>A list of parsed code artifacts</returns>
    public List<CodeArtifact> ParseToCodeArtifacts(string aiResponse, string fileType, string? defaultFileName = null)
    {
        if (string.IsNullOrWhiteSpace(aiResponse))
        {
            _logger.LogWarning("Received empty or null AI response for parsing");
            return new List<CodeArtifact>();
        }

        var artifacts = new List<CodeArtifact>();

        // Look for code blocks with file names in format: ```csharp:FileName.cs
        var matches = Regex.Matches(aiResponse, CodeBlockPattern, RegexOptions.Singleline);

        _logger.LogDebug("Found {MatchCount} code blocks in AI response", matches.Count);

        foreach (Match match in matches)
        {
            var fileName = match.Groups[1].Value.Trim();
            var content = match.Groups[2].Value.Trim();

            if (string.IsNullOrEmpty(fileName))
            {
                fileName = $"Generated{artifacts.Count + 1}.cs";
                _logger.LogDebug("No filename specified in code block, using default: {FileName}", fileName);
            }

            artifacts.Add(new CodeArtifact
            {
                FileName = fileName,
                Content = content,
                FileType = fileType
            });

            _logger.LogDebug("Parsed code artifact: {FileName} ({ContentLength} chars)", fileName, content.Length);
        }

        // If no code blocks were found, treat the entire response as a single file
        if (artifacts.Count == 0)
        {
            var fallbackFileName = defaultFileName ?? $"Generated{fileType}.cs";
            _logger.LogInformation("No code blocks found, treating entire response as single file: {FileName}", fallbackFileName);

            artifacts.Add(new CodeArtifact
            {
                FileName = fallbackFileName,
                Content = aiResponse,
                FileType = fileType
            });
        }

        _logger.LogInformation("Parsed {ArtifactCount} code artifact(s) from AI response", artifacts.Count);
        return artifacts;
    }
}
