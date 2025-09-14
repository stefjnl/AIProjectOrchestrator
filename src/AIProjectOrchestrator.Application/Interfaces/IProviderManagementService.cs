namespace AIProjectOrchestrator.Application.Interfaces
{
    public interface IProviderManagementService
    {
        Task<IEnumerable<string>> GetAvailableProvidersAsync();
        Task<object> GetProviderHealthAsync(string name);
        Task<bool> IsValidProviderAsync(string provider);
    }
}
