namespace AIProjectOrchestrator.Domain.Configuration
{
    /// <summary>
    /// Centralized provider names to eliminate magic strings scattered across the codebase.
    /// Use these constants in switches, configuration services, factories, and UI bindings.
    /// </summary>
    public static class ProviderNames
    {
        public const string NanoGpt = "NanoGpt";
        public const string OpenRouter = "OpenRouter";
        public const string Claude = "Claude";
        public const string LMStudio = "LMStudio";
        public const string AlibabaCloud = "AlibabaCloud";
    }
}