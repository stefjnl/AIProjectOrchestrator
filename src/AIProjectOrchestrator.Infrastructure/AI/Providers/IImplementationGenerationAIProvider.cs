namespace AIProjectOrchestrator.Infrastructure.AI.Providers
{
    /// <summary>
    /// AI provider specifically configured for Implementation Generation operations.
    /// Inherits from IAIProvider but provides compile-time safety and clarity.
    /// </summary>
    public interface IImplementationGenerationAIProvider : IAIProvider
    {
        // No additional methods - this interface exists for:
        // 1. Compile-time type safety
        // 2. Clear semantic meaning in DI registration
        // 3. Future extensibility if Implementation Generation needs specific methods
    }
}