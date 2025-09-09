namespace AIProjectOrchestrator.Domain.Services;

/// <summary>
/// Service for configuring AI model parameters and provider routing.
/// Provides centralized configuration for mapping model names to actual AI provider models and endpoints.
/// </summary>
public interface IAIModelConfigurationService
{
    /// <summary>
    /// Gets the actual model name to use for AI requests based on the selected model type.
    /// </summary>
    /// <param name="modelType">The model type selected by the user (e.g., "claude", "qwen3-coder", "deepseek")</param>
    /// <returns>The actual model name to use for AI API calls</returns>
    string GetModelName(string modelType);

    /// <summary>
    /// Gets the AI provider name to use for the selected model type.
    /// </summary>
    /// <param name="modelType">The model type selected by the user (e.g., "claude", "qwen3-coder", "deepseek")</param>
    /// <returns>The AI provider name (e.g., "OpenRouter", "LMStudio")</returns>
    string GetProviderName(string modelType);

    /// <summary>
    /// Validates if the specified model type is supported.
    /// </summary>
    /// <param name="modelType">The model type to validate</param>
    /// <returns>True if the model type is supported, false otherwise</returns>
    bool IsModelSupported(string modelType);

    /// <summary>
    /// Gets the maximum context size (in tokens) for the specified model type.
    /// </summary>
    /// <param name="modelType">The model type</param>
    /// <returns>The maximum context size in tokens</returns>
    int GetMaxContextSize(string modelType);
}