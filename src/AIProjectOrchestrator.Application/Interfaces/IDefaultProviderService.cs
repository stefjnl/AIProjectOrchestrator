using System.Threading.Tasks;

namespace AIProjectOrchestrator.Application.Interfaces;

/// <summary>
/// Service for managing the default AI provider configuration.
/// </summary>
public interface IDefaultProviderService
{
    /// <summary>
    /// Sets the default AI provider.
    /// </summary>
    /// <param name="provider">The provider name to set as default</param>
    Task SetDefaultProviderAsync(string provider);
    
    /// <summary>
    /// Gets the current default AI provider.
    /// </summary>
    /// <returns>The current default provider name, or null if not set</returns>
    Task<string?> GetDefaultProviderAsync();
}