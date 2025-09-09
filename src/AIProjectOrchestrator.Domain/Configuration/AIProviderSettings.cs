namespace AIProjectOrchestrator.Domain.Configuration
{
    public class AIProviderSettings
    {
        public const string SectionName = "AIProviders";
        
        public ClaudeSettings Claude { get; set; } = new();
        public LMStudioSettings LMStudio { get; set; } = new();
        public OpenRouterSettings OpenRouter { get; set; } = new();
        public NanoGptSettings NanoGpt { get; set; } = new();
    }

    public class ClaudeSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api.anthropic.com";
        public int TimeoutSeconds { get; set; } = 30;
        public int MaxRetries { get; set; } = 3;
        public string DefaultModel { get; set; } = "claude-3-sonnet-20240229";
    }

    public class LMStudioSettings
    {
        public string BaseUrl { get; set; } = "http://100.74.43.85:1234";
        public int TimeoutSeconds { get; set; } = 60;
        public int MaxRetries { get; set; } = 2;
        public string DefaultModel { get; set; } = "qwen-coder";
    }

    public class OpenRouterSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1";
        public int TimeoutSeconds { get; set; } = 30;
        public int MaxRetries { get; set; } = 3;
        public string DefaultModel { get; set; } = "qwen/qwen3-coder";
    }
}

public class NanoGptSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://nano-gpt.com/api/v1"; // TODO: Update with actual base URL
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
    public string DefaultModel { get; set; } = "nano-3"; // TODO: Update with actual default model
}