using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AIProjectOrchestrator.Infrastructure.AI
{
    /// <summary>
    /// Infrastructure layer implementation of IProviderConfigurationService that provides
    /// runtime provider configuration by accessing the Application layer service.
    /// </summary>
    public class ProviderConfigurationService : IProviderConfigurationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ProviderConfigurationService> _logger;

        /// <summary>
        /// Creates a new ProviderConfigurationService.
        /// </summary>
        /// <param name="serviceProvider">Service provider for resolving Application layer services</param>
        /// <param name="logger">Logger for diagnostics</param>
        public ProviderConfigurationService(IServiceProvider serviceProvider, ILogger<ProviderConfigurationService> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<string?> GetDefaultProviderAsync()
        {
            try
            {
                _logger.LogDebug("ProviderConfigurationService: Attempting to get default provider using reflection");
                
                // Resolve the Application layer service to get the current default provider
                using var scope = _serviceProvider.CreateScope();
                
                // Use reflection to avoid direct reference to Application layer
                var defaultProviderServiceType = Type.GetType("AIProjectOrchestrator.Application.Interfaces.IDefaultProviderService, AIProjectOrchestrator.Application");
                if (defaultProviderServiceType == null)
                {
                    _logger.LogWarning("ProviderConfigurationService: Could not find IDefaultProviderService type");
                    return null;
                }

                var defaultProviderService = scope.ServiceProvider.GetService(defaultProviderServiceType);
                if (defaultProviderService == null)
                {
                    _logger.LogWarning("ProviderConfigurationService: Could not resolve IDefaultProviderService from DI container");
                    return null;
                }

                // Use reflection to call the method
                var method = defaultProviderServiceType.GetMethod("GetDefaultProviderAsync");
                if (method == null)
                {
                    _logger.LogWarning("ProviderConfigurationService: Could not find GetDefaultProviderAsync method");
                    return null;
                }

                var invokeResult = method.Invoke(defaultProviderService, null);
                if (invokeResult is Task<string?> resultTask)
                {
                    var result = await resultTask;
                    _logger.LogDebug("ProviderConfigurationService: Successfully retrieved default provider: {Provider}", result ?? "null");
                    return result;
                }
                else
                {
                    _logger.LogWarning("ProviderConfigurationService: Invoke result was not of expected type Task<string?>");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ProviderConfigurationService: Error getting default provider");
                return null;
            }
        }
    }
}