namespace AIProjectOrchestrator.Infrastructure.AI.Providers
{
    /// <summary>
    /// AI provider specifically configured for Project Planning operations.
    /// Inherits from IAIProvider but provides compile-time safety and clarity.
    /// </summary>
    public interface IPlanningAIProvider : IAIProvider
    {
        // No additional methods - this interface exists for:
        // 1. Compile-time type safety
        // 2. Clear semantic meaning in DI registration
        // 3. Future extensibility if Project Planning needs specific methods
    }
}