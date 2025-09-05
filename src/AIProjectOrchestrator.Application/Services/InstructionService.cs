using AIProjectOrchestrator.Domain.Models;
using AIProjectOrchestrator.Domain.Services;
using AIProjectOrchestrator.Domain.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AIProjectOrchestrator.Application.Services
{
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
            
            var baseDirectory = AppContext.BaseDirectory;
            var currentDirectory = Directory.GetCurrentDirectory();

            // First, try the path relative to AppContext.BaseDirectory
            _fullInstructionsPath = Path.Combine(baseDirectory, _settings.InstructionsPath);
            _logger.LogInformation("Attempting to find instructions at AppContext.BaseDirectory path: {Path}", _fullInstructionsPath);

            // If not found, try relative to Current Directory
            if (!Directory.Exists(_fullInstructionsPath))
            {
                var currentDirPath = Path.Combine(currentDirectory, _settings.InstructionsPath);
                _logger.LogInformation("AppContext.BaseDirectory path not found. Attempting Current Directory path: {Path}", currentDirPath);
                if (Directory.Exists(currentDirPath))
                {
                    _fullInstructionsPath = currentDirPath;
                }
            }
            
            // Log the contents of the directory for debugging
            if (Directory.Exists(_fullInstructionsPath))
            {
                var files = Directory.GetFiles(_fullInstructionsPath);
                _logger.LogInformation("Found {Count} files in instructions directory: {Files}", files.Length, string.Join(", ", files.Select(Path.GetFileName)));
            }
            else
            {
                _logger.LogWarning("Instructions directory not found at final path: {Path}", _fullInstructionsPath);
                
                // Try to find the directory in parent directories for fallback (original logic)
                var tempPath = baseDirectory;
                for (int i = 0; i < 5; i++)
                {
                    var parentDir = Directory.GetParent(tempPath)?.FullName;
                    if (string.IsNullOrEmpty(parentDir))
                        break;
                        
                    var possiblePath = Path.Combine(parentDir, _settings.InstructionsPath);
                    if (Directory.Exists(possiblePath))
                    {
                        _fullInstructionsPath = possiblePath;
                        _logger.LogInformation("Found instructions directory at fallback parent path: {Path}", _fullInstructionsPath);
                        break;
                    }
                    tempPath = parentDir;
                }
            }
        }

        public async Task<InstructionContent> GetInstructionAsync(string serviceName, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting instruction for service: {ServiceName}", serviceName);

            // Convert service name to file name
            var fileName = GetInstructionFileName(serviceName);
            var filePath = Path.Combine(_fullInstructionsPath, fileName);

            _logger.LogInformation("Looking for instruction file at: {FilePath}", filePath);

            // Check if file exists
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Instruction file not found: {FilePath}", filePath);
                
                // Log directory contents for debugging
                if (Directory.Exists(_fullInstructionsPath))
                {
                    var files = Directory.GetFiles(_fullInstructionsPath);
                    _logger.LogInformation("Available files in directory: {Files}", string.Join(", ", files.Select(Path.GetFileName)));
                }
                
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

            var fileName = serviceName
                .Replace("Analysis", "Analyst")
                .Replace("Planning", "Planner")
                .Replace("Generation", "Generator");

            // Always add .md extension if not already present
            if (!fileName.EndsWith(".md"))
            {
                fileName += ".md";
            }

            return fileName;
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
}
