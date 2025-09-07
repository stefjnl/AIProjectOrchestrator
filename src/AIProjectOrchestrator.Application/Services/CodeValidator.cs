using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AIProjectOrchestrator.Domain.Models.Code;
using AIProjectOrchestrator.Domain.Services;

namespace AIProjectOrchestrator.Application.Services;

public class CodeValidator : ICodeValidator
{
    private readonly ILogger<CodeValidator> _logger;

    public CodeValidator(ILogger<CodeValidator> logger)
    {
        _logger = logger;
    }

    public async Task<bool> ValidateGeneratedCodeAsync(List<CodeArtifact> artifacts, CancellationToken cancellationToken = default)
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

    public async Task<List<string>> ValidateCSharpSyntaxAsync(string codeContent, CancellationToken cancellationToken = default)
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

    private class TestCoverageInfo
    {
        public List<string> MissingTests { get; set; } = new List<string>();
        public double EstimatedCoveragePercentage { get; set; } = 0.0;
    }
}