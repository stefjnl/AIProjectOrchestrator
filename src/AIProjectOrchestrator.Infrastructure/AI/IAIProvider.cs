using System.Threading.Tasks;

namespace AIProjectOrchestrator.Infrastructure.AI
{
    /// <summary>
    /// Clean business interface for AI content generation.
    /// Business services use this interface without knowing about AI implementation details.
    /// </summary>
    public interface IAIProvider
    {
        /// <summary>
        /// Generates content using AI based on prompt and optional business context.
        /// </summary>
        /// <param name="prompt">The business prompt to generate content for</param>
        /// <param name="context">Optional business context (instructions, constraints, etc.)</param>
        /// <returns>Generated content as string</returns>
        Task<string> GenerateContentAsync(string prompt, string context = null);
        
        /// <summary>
        /// Checks if the AI provider is available and healthy.
        /// </summary>
        /// <returns>True if provider is available, false otherwise</returns>
        Task<bool> IsAvailableAsync();
        
        /// <summary>
        /// Gets the provider name for logging and identification.
        /// </summary>
        string ProviderName { get; }
    }
}