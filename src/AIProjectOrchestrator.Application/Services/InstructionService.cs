using AIProjectOrchestrator.Application.Configuration;
using AIProjectOrchestrator.Domain.Models;
using AIProjectOrchestrator.Domain.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text;

namespace AIProjectOrchestrator.Application.Services;

/// <summary>
///   The InstructionService dynamically loads AI instruction files for different AI sub-agents. It maps service names to markdown instruction files (e.g., RequirementsAnalysisService →
///   RequirementsAnalyst.md), caches them in memory with automatic invalidation based on file modification times, and validates content requirements. This allows AI services to access their specific behavioral instructions
///  without requiring application restarts during development iterations.
/// </summary>
public class InstructionService : IInstructionService
{
    private readonly InstructionSettings _settings;
    private readonly ILogger<InstructionService> _logger;
    private readonly ConcurrentDictionary<string, CachedInstruction> _cache;
    private readonly string _fullInstructionsPath;

    public InstructionService(
        IOptions<InstructionSettings> settings,
        ILogger<InstructionService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _cache = new ConcurrentDictionary<string, CachedInstruction>();
        _fullInstructionsPath = Path.Combine(Directory.GetCurrentDirectory(), _settings.InstructionsPath);
        
        _logger.LogInformation("InstructionService initialized with path: {Path}", _fullInstructionsPath);
    }

    public async Task<InstructionContent> GetInstructionAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting instruction for service: {ServiceName}", serviceName);

        // Convert service name to file name
        var fileName = GetInstructionFileName(serviceName);
        var filePath = Path.Combine(_fullInstructionsPath, fileName);

        // Check if file exists
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Instruction file not found: {FilePath}", filePath);
            return new InstructionContent
            {
                ServiceName = serviceName,
                Content = string.Empty,
                LastModified = DateTime.MinValue,
                IsValid = false,
                ValidationMessage = $"Instruction file not found: {fileName}"
            };
        }

        try
        {
            // Check cache first
            if (_cache.TryGetValue(serviceName, out var cachedInstruction))
            {
                var lastWriteTime = File.GetLastWriteTimeUtc(filePath);
                if (lastWriteTime <= cachedInstruction.LastModified)
                {
                    _logger.LogDebug("Returning cached instruction for service: {ServiceName}", serviceName);
                    return cachedInstruction.Content;
                }
                else
                {
                    _logger.LogDebug("Cache expired for service: {ServiceName}, reloading", serviceName);
                }
            }

            // Load file content
            var content = await File.ReadAllTextAsync(filePath, Encoding.UTF8, cancellationToken);
            var lastModified = File.GetLastWriteTimeUtc(filePath);

            // Validate content
            var isValid = ValidateContent(content, out var validationMessage);

            var instructionContent = new InstructionContent
            {
                ServiceName = serviceName,
                Content = content,
                LastModified = lastModified,
                IsValid = isValid,
                ValidationMessage = validationMessage
            };

            // Cache the instruction
            _cache[serviceName] = new CachedInstruction
            {
                Content = instructionContent,
                LastModified = lastModified
            };

            _logger.LogInformation("Successfully loaded instruction for service: {ServiceName}", serviceName);
            return instructionContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading instruction for service: {ServiceName}", serviceName);
            return new InstructionContent
            {
                ServiceName = serviceName,
                Content = string.Empty,
                LastModified = DateTime.MinValue,
                IsValid = false,
                ValidationMessage = $"Error loading instruction: {ex.Message}"
            };
        }
    }

    public async Task<bool> IsValidInstructionAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        var instruction = await GetInstructionAsync(serviceName, cancellationToken);
        return instruction.IsValid;
    }

    private string GetInstructionFileName(string serviceName)
    {
        // Convert service names to file names using convention:
        // RequirementsAnalysisService → RequirementsAnalyst.md
        // ProjectPlanningService → ProjectPlanner.md 
        // StoryGenerationService → StoryGenerator.md
        
        return serviceName
            .Replace("Service", ".md")
            .Replace("Analysis", "Analyst")
            .Replace("Planning", "Planner")
            .Replace("Generation", "Generator");
    }

    private bool ValidateContent(string content, out string validationMessage)
    {
        validationMessage = string.Empty;

        // Check minimum length
        if (content.Length < _settings.MinimumContentLength)
        {
            validationMessage = $"Content too short. Minimum {_settings.MinimumContentLength} characters required.";
            return false;
        }

        // Check for required sections (case-insensitive)
        var lowerContent = content.ToLowerInvariant();
        foreach (var section in _settings.RequiredSections)
        {
            if (!lowerContent.Contains(section.ToLowerInvariant()))
            {
                validationMessage = $"Missing required section: {section}";
                return false;
            }
        }

        // Check UTF-8 encoding
        try
        {
            Encoding.UTF8.GetBytes(content);
        }
        catch (Exception)
        {
            validationMessage = "Content is not valid UTF-8";
            return false;
        }

        return true;
    }

    private class CachedInstruction
    {
        public InstructionContent Content { get; set; } = new InstructionContent();
        public DateTime LastModified { get; set; }
    }
}