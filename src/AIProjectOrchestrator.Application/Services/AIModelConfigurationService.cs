using Microsoft.Extensions.Logging;
using AIProjectOrchestrator.Domain.Services;

namespace AIProjectOrchestrator.Application.Services;

/// <summary>
/// Service for configuring AI model parameters and provider routing.
/// Provides centralized configuration for mapping model names to actual AI provider models and endpoints.
/// </summary>
public class AIModelConfigurationService : IAIModelConfigurationService
{
    private readonly ILogger<AIModelConfigurationService> _logger;

    public AIModelConfigurationService(ILogger<AIModelConfigurationService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public string GetModelName(string modelType)
    {
        if (string.IsNullOrWhiteSpace(modelType))
        {
            _logger.LogWarning("Model type is null or empty, defaulting to qwen/qwen3-coder");
            return "qwen/qwen3-coder";
        }

        var modelName = modelType.ToLower() switch
        {
            "claude" => "qwen/qwen3-coder",
            "qwen3-coder" => "qwen/qwen3-coder",
            "deepseek" => "qwen/qwen3-coder", // Use Qwen for all models
            _ => "qwen/qwen3-coder"
        };

        _logger.LogDebug("Mapped model type '{ModelType}' to model name '{ModelName}'", modelType, modelName);
        return modelName;
    }

    /// <inheritdoc />
    public string GetProviderName(string modelType)
    {
        if (string.IsNullOrWhiteSpace(modelType))
        {
            _logger.LogWarning("Model type is null or empty, defaulting to OpenRouter");
            return "OpenRouter";
        }

        var providerName = modelType.ToLower() switch
        {
            "claude" => "OpenRouter", // Route Claude requests to OpenRouter
            "qwen3-coder" => "LMStudio",
            "deepseek" => "OpenRouter",
            _ => "OpenRouter"
        };

        _logger.LogDebug("Mapped model type '{ModelType}' to provider '{ProviderName}'", modelType, providerName);
        return providerName;
    }

    /// <inheritdoc />
    public bool IsModelSupported(string modelType)
    {
        if (string.IsNullOrWhiteSpace(modelType))
        {
            return false;
        }

        var supportedModels = new[] { "claude", "qwen3-coder", "deepseek" };
        return supportedModels.Contains(modelType.ToLower());
    }

    /// <inheritdoc />
    public int GetMaxContextSize(string modelType)
    {
        // Return context sizes based on model capabilities
        return modelType?.ToLower() switch
        {
            "claude" => 200000, // Claude models typically support 200K tokens
            "qwen3-coder" => 128000, // Qwen models typically support 128K tokens
            "deepseek" => 128000, // DeepSeek models typically support 128K tokens
            _ => 128000 // Default to 128K tokens for unknown models
        };
    }
}