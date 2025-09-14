using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Interfaces;
using AIProjectOrchestrator.Domain.Models;
using AIProjectOrchestrator.Domain.Models.AI;
using AIProjectOrchestrator.Domain.Models.Stories;
using AIProjectOrchestrator.Domain.Services;
using Microsoft.Extensions.Logging;

namespace AIProjectOrchestrator.Application.Services.Builders
{
    public class StoryContextBuilder : IStoryContextBuilder
    {
        private readonly IProjectPlanningService _projectPlanningService;
        private readonly IRequirementsAnalysisService _requirementsAnalysisService;
        private readonly IInstructionService _instructionService;
        private readonly ILogger<StoryContextBuilder> _logger;

        public StoryContextBuilder(
            IProjectPlanningService projectPlanningService,
            IRequirementsAnalysisService requirementsAnalysisService,
            IInstructionService instructionService,
            ILogger<StoryContextBuilder> logger)
        {
            _projectPlanningService = projectPlanningService;
            _requirementsAnalysisService = requirementsAnalysisService;
            _instructionService = instructionService;
            _logger = logger;
        }

        public async Task<AIRequest> BuildAsync(Guid planningId, StoryGenerationRequest request, Guid generationId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Loading instructions for story generation {GenerationId}", generationId);

            // Retrieve context from both services
            var planningContent = await _projectPlanningService.GetPlanningResultContentAsync(
                planningId, cancellationToken);

            if (string.IsNullOrEmpty(planningContent))
            {
                _logger.LogError("Story context build {GenerationId} failed: Planning content not found", generationId);
                throw new InvalidOperationException("Planning content not found");
            }

            // Get requirements analysis ID and content
            var requirementsAnalysisId = await _projectPlanningService.GetRequirementsAnalysisIdAsync(
                planningId, cancellationToken);

            var requirementsContent = string.Empty;
            if (requirementsAnalysisId.HasValue)
            {
                requirementsContent = await _requirementsAnalysisService.GetAnalysisResultContentAsync(
                    requirementsAnalysisId.Value, cancellationToken) ?? string.Empty;
            }

            // Load instructions
            var instructionContent = await _instructionService.GetInstructionAsync("StoryGenerator", cancellationToken);

            if (!instructionContent.IsValid)
            {
                _logger.LogError("Story context build {GenerationId} failed: Invalid instruction content - {ValidationMessage}",
                    generationId, instructionContent.ValidationMessage);
                throw new InvalidOperationException($"Failed to load valid instructions: {instructionContent.ValidationMessage}");
            }

            // Create prompt from context
            var prompt = CreatePromptFromContext(planningContent, requirementsContent, request);

            // Create AI request with combined context
            var aiRequest = new AIRequest
            {
                SystemMessage = instructionContent.Content,
                Prompt = prompt,
                ModelName = string.Empty, // Will be set by the provider
                Temperature = 0.7, // Default value, will be overridden by provider
                MaxTokens = 1000  // Default value, will be overridden by provider
            };

            // Log context size metrics
            var contextSize = Encoding.UTF8.GetByteCount(aiRequest.SystemMessage + aiRequest.Prompt);
            _logger.LogInformation("Story generation {GenerationId} context size: {ContextSize} bytes", generationId, contextSize);

            // Warn if context size is approaching limits
            if (contextSize > 100000) // Roughly 25K tokens
            {
                _logger.LogWarning("Story generation {GenerationId} context size is large: {ContextSize} bytes", generationId, contextSize);
            }

            _logger.LogDebug("Story context built successfully for generation {GenerationId}", generationId);

            return aiRequest;
        }

        private string CreatePromptFromContext(
            string planningContent,
            string requirementsContent,
            StoryGenerationRequest request)
        {
            var prompt = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(requirementsContent))
            {
                prompt.AppendLine("# Requirements Analysis Content");
                prompt.AppendLine(requirementsContent);
                prompt.AppendLine();
            }

            prompt.AppendLine("# Project Planning Content");
            prompt.AppendLine(planningContent);
            prompt.AppendLine();

            if (!string.IsNullOrWhiteSpace(request.StoryPreferences))
            {
                prompt.AppendLine("## Story Preferences");
                prompt.AppendLine(request.StoryPreferences);
                prompt.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(request.ComplexityLevels))
            {
                prompt.AppendLine("## Complexity Levels");
                prompt.AppendLine(request.ComplexityLevels);
                prompt.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(request.AdditionalGuidance))
            {
                prompt.AppendLine("## Additional Guidance");
                prompt.AppendLine(request.AdditionalGuidance);
                prompt.AppendLine();
            }

            prompt.AppendLine("## Instructions");
            prompt.AppendLine("Please generate 5-15 user stories based on the requirements analysis and project planning content above.");
            prompt.AppendLine("Each story should include:");
            prompt.AppendLine("- A clear title");
            prompt.AppendLine("- A detailed description following the 'As a [role], I want [goal] so that [benefit]' format");
            prompt.AppendLine("- Specific acceptance criteria as bullet points");
            prompt.AppendLine("- A priority level (High, Medium, Low)");
            prompt.AppendLine("- An estimated complexity level if applicable");

            return prompt.ToString();
        }
    }
}
