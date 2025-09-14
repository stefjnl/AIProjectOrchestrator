namespace AIProjectOrchestrator.Domain.Configuration
{
    public class AIProviderCredentials
    {
        public const string SectionName = "AIProviders";

        public ClaudeCredentials Claude { get; set; } = new();
        public LMStudioCredentials LMStudio { get; set; } = new();
        public OpenRouterCredentials OpenRouter { get; set; } = new();
        public NanoGptCredentials NanoGpt { get; set; } = new();
        public AlibabaCloudCredentials AlibabaCloud { get; set; } = new();
    }

    public class ClaudeCredentials
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api.anthropic.com";
        public int TimeoutSeconds { get; set; } = 30;
        public int MaxRetries { get; set; } = 3;
        public string DefaultModel { get; set; } = "claude-3-sonnet-20240229";
    }

    public class LMStudioCredentials
    {
        public string BaseUrl { get; set; } = "http://100.74.43.85:1234";
        public int TimeoutSeconds { get; set; } = 60;
        public int MaxRetries { get; set; } = 2;
        public string DefaultModel { get; set; } = "qwen-coder";
    }

    public class OpenRouterCredentials
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1";
        public int TimeoutSeconds { get; set; } = 30;
        public int MaxRetries { get; set; } = 3;
        public string DefaultModel { get; set; } = "moonshotai/kimi-k2-0905";
    }

    public class NanoGptCredentials
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api.nanogpt.com";
        public int TimeoutSeconds { get; set; } = 30;
        public int MaxRetries { get; set; } = 3;
        public string DefaultModel { get; set; } = "moonshotai/Kimi-K2-Instruct-0905";
    }

    public class AlibabaCloudCredentials
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://dashscope-intl.aliyuncs.com/compatible-mode/v1";
        public int TimeoutSeconds { get; set; } = 30;
        public int MaxRetries { get; set; } = 3;
        public string DefaultModel { get; set; } = "qwen-plus";
    }
}