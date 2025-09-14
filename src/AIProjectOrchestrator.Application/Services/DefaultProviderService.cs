using AIProjectOrchestrator.Application.Interfaces;

namespace AIProjectOrchestrator.Application.Services;

/// <summary>
/// Service for managing the default AI provider configuration.
/// </summary>
public class DefaultProviderService : IDefaultProviderService
{
    private string? _defaultProvider;

    /// <summary>
    /// Sets the default AI provider.
    /// </summary>
    /// <param name="provider">The provider name to set as default</param>
    public Task SetDefaultProviderAsync(string provider)
    {
        _defaultProvider = provider;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the current default AI provider.
    /// </summary>
    /// <returns>The current default provider name, or null if not set</returns>
    public Task<string?> GetDefaultProviderAsync()
    {
        return Task.FromResult(_defaultProvider);
    }
}