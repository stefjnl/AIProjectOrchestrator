namespace AIProjectOrchestrator.Infrastructure.AI.Providers
{
    /// <summary>
    /// AI provider specifically configured for Test Generation operations.
    /// Inherits from IAIProvider but provides compile-time safety and clarity.
    /// </summary>
    public interface ITestGenerationAIProvider : IAIProvider
    {
        // No additional methods - this interface exists for:
        // 1. Compile-time type safety
        // 2. Clear semantic meaning in DI registration
        // 3. Future extensibility if Test Generation needs specific methods
    }
}