namespace AIProjectOrchestrator.Infrastructure.AI.Providers
{
    /// <summary>
    /// AI provider specifically configured for Prompt Generation operations.
    /// Inherits from IAIProvider but provides compile-time safety and clarity.
    /// </summary>
    public interface IPromptGenerationAIProvider : IAIProvider
    {
        // No additional methods - this interface exists for:
        // 1. Compile-time type safety
        // 2. Clear semantic meaning in DI registration
        // 3. Future extensibility if Prompt Generation needs specific methods
    }
}