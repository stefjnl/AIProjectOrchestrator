using System.Collections.Generic;

namespace AIProjectOrchestrator.Infrastructure.Configuration
{
    /// <summary>
    /// Configuration for AI providers with operation-specific settings.
    /// Each operation (RequirementsAnalysis, ProjectPlanning, etc.) can have different AI configurations.
    /// </summary>
    public class AIOperationSettings
    {
        /// <summary>
        /// Operation-specific AI provider configurations.
        /// Key: Operation type (RequirementsAnalysis, ProjectPlanning, StoryGeneration, etc.)
        /// Value: Configuration for that specific operation
        /// </summary>
        public Dictionary<string, AIOperationConfig> Operations { get; set; } = new();
    }

    /// <summary>
    /// Configuration for a specific AI operation.
    /// </summary>
    public class AIOperationConfig
    {
        /// <summary>
        /// AI provider name (NanoGpt, OpenRouter, LMStudio, etc.)
        /// </summary>
        public string ProviderName { get; set; } = "NanoGpt";

        /// <summary>
        /// AI model to use for this operation
        /// </summary>
        public string Model { get; set; } = "moonshotai/Kimi-K2-Instruct-0905";

        /// <summary>
        /// Maximum tokens for AI responses in this operation
        /// </summary>
        public int MaxTokens { get; set; } = 2000;

        /// <summary>
        /// Temperature (creativity) setting for this operation
        /// </summary>
        public float Temperature { get; set; } = 0.7f;

        /// <summary>
        /// Request timeout in seconds
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;
    }
}