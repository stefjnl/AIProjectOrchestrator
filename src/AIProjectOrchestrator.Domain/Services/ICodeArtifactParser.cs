using System.Collections.Generic;
using AIProjectOrchestrator.Domain.Models.Code;

namespace AIProjectOrchestrator.Domain.Services;

/// <summary>
/// Service for parsing AI-generated responses into code artifacts.
/// </summary>
public interface ICodeArtifactParser
{
    /// <summary>
    /// Parses AI response text into a list of code artifacts.
    /// Extracts code blocks with optional file names and creates structured artifacts.
    /// </summary>
    /// <param name="aiResponse">The raw AI response containing code blocks</param>
    /// <param name="fileType">The type of file (e.g., "Test", "Implementation")</param>
    /// <param name="defaultFileName">Optional default file name if none is found in the response</param>
    /// <returns>A list of parsed code artifacts</returns>
    List<CodeArtifact> ParseToCodeArtifacts(string aiResponse, string fileType, string? defaultFileName = null);
}
