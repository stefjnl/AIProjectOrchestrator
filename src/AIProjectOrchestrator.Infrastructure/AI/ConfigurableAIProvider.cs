using System;
using System.Net.Http;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Models.AI;
using AIProjectOrchestrator.Domain.Services;
using AIProjectOrchestrator.Domain.Configuration;
using AIProjectOrchestrator.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace AIProjectOrchestrator.Infrastructure.AI
{
    /// <summary>
    /// Base implementation of IAIProvider with operation-specific configuration.
    /// Each derived class represents a specific business operation with its own AI configuration.
    /// </summary>
    public abstract class ConfigurableAIProvider : IAIProvider
    {
        /// <inheritdoc />
        public async Task<IEnumerable<string>> GetModelsAsync()
        {
            var options = GetAIOperationConfig();

            // Use DI-configured, provider-specific HttpClient
            var httpClient = GetProviderHttpClient(options.ProviderName);
            if (httpClient == null)
            {
                _logger.LogWarning("Provider HttpClient is not available for operation '{Operation}'", _operationType);
                return new List<string>();
            }

            // Create AI client with proper configuration
            var client = CreateAIClient(options.ProviderName, httpClient);
            if (client == null)
            {
                _logger.LogWarning("AI client '{Provider}' is not available for operation '{Operation}'",
                    options.ProviderName, _operationType);
                return new List<string>();
            }

            try
            {
                var models = await client.GetModelsAsync();
                _logger.LogDebug("Retrieved {Count} models for provider '{Provider}' in operation '{Operation}'",
                    models?.Count() ?? 0, options.ProviderName, _operationType);
                return models ?? new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve models for AI provider '{Provider}' in operation '{Operation}'",
                    options.ProviderName, _operationType);
                return new List<string>();
            }
        }

        private readonly string _operationType;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOptions<AIProjectOrchestrator.Infrastructure.Configuration.AIOperationSettings> _settings;
        private readonly ILogger<ConfigurableAIProvider> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IServiceProvider? _serviceProvider;

        /// <summary>
        /// Creates a new instance of ConfigurableAIProvider with specific operation configuration.
        /// </summary>
        /// <param name="operationType">The operation type for configuration lookup</param>
        /// <param name="httpClientFactory">Factory for creating HTTP clients with Docker SSL support</param>
        /// <param name="settings">Operation-specific configuration</param>
        /// <param name="logger">Logger for diagnostics</param>
        /// <param name="loggerFactory">Logger factory for creating operation-specific loggers</param>
        /// <param name="serviceProvider">Service provider for dependency resolution</param>
        protected ConfigurableAIProvider(string operationType, IHttpClientFactory httpClientFactory,
            IOptions<AIProjectOrchestrator.Infrastructure.Configuration.AIOperationSettings> settings, ILogger<ConfigurableAIProvider> logger,
            ILoggerFactory loggerFactory, IServiceProvider? serviceProvider = null)
        {
            _operationType = operationType ?? throw new ArgumentNullException(nameof(operationType));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _serviceProvider = serviceProvider!;

            // Add detailed logging to diagnose configuration issues
            _logger.LogInformation("--- ConfigurableAIProvider Constructor Debug ---");
            _logger.LogInformation("Operation Type: {OperationType}", _operationType);
            if (_settings.Value == null)
            {
                _logger.LogWarning("AIOperationSettings.Value is NULL");
            }
            else if (_settings.Value.Operations == null)
            {
                _logger.LogWarning("AIOperationSettings.Value.Operations is NULL");
            }
            else
            {
                _logger.LogInformation("Available Operations: {Operations}", string.Join(", ", _settings.Value.Operations.Keys));
                if (_settings.Value.Operations.TryGetValue(_operationType, out var config))
                {
                    _logger.LogInformation("Configuration for '{OperationType}': Provider={Provider}, Model={Model}, MaxTokens={MaxTokens}",
                        _operationType, config.ProviderName, config.Model, config.MaxTokens);
                }
                else
                {
                    _logger.LogWarning("No configuration found for operation type: {OperationType}", _operationType);
                }
            }
            _logger.LogInformation("--- End Constructor Debug ---");
        }

        /// <inheritdoc />
        public string ProviderName
        {
            get
            {
                // Return the configuration-based provider name directly
                // This avoids blocking async calls which can cause deadlocks
                // If runtime provider override is needed in the future, consider:
                // 1. Making ProviderName async in the interface (breaking change)
                // 2. Caching the provider name during construction
                // 3. Using a separate method GetProviderNameAsync()
                var configProvider = GetAIOperationConfig().ProviderName;
                
                _logger.LogDebug("Provider for operation '{Operation}': {Provider}",
                    _operationType, configProvider);
                
                return configProvider;
            }
        }

        /// <inheritdoc />
        public async Task<string> GenerateContentAsync(string prompt, string? context = null)
        {
            _logger.LogDebug("Generating content for operation '{Operation}' with prompt length {PromptLength}",
                _operationType, prompt?.Length ?? 0);

            var options = GetAIOperationConfig();

#pragma warning disable CS8601
            var aiRequest = new AIRequest
            {
                Prompt = prompt,
                SystemMessage = context ?? string.Empty, // Business context, not AI system message
                ModelName = options.Model,
                MaxTokens = options.MaxTokens,
                Temperature = options.Temperature
            };
#pragma warning restore CS8601

            // Use DI-configured, provider-specific HttpClient
            var httpClient = GetProviderHttpClient(options.ProviderName);
            if (httpClient == null)
            {
                _logger.LogError("Provider HttpClient is not available for operation '{Operation}'", _operationType);
                throw new InvalidOperationException($"Provider HttpClient is not available for operation '{_operationType}'");
            }

            // Create AI client with proper configuration
            var client = CreateAIClient(options.ProviderName, httpClient);
            if (client == null)
            {
                _logger.LogError("AI client '{Provider}' is not available for operation '{Operation}'",
                    options.ProviderName, _operationType);
                throw new InvalidOperationException($"AI client '{options.ProviderName}' is not available for operation '{_operationType}'");
            }

            _logger.LogInformation("Calling AI provider '{Provider}' for operation '{Operation}' with model '{Model}'",
                options.ProviderName, _operationType, options.Model);

            try
            {
                var response = await client.CallAsync(aiRequest);

                if (!response.IsSuccess)
                {
                    _logger.LogError("AI call failed for operation '{Operation}': {ErrorMessage}",
                        _operationType, response.ErrorMessage);
                    throw new InvalidOperationException($"AI call failed: {response.ErrorMessage}");
                }

                _logger.LogInformation("AI call successful for operation '{Operation}', response length: {ResponseLength}",
                    _operationType, response.Content?.Length ?? 0);

                return response.Content ?? string.Empty;
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP request failed for operation '{Operation}' with provider '{Provider}' and model '{Model}'",
                    _operationType, options.ProviderName, options.Model);
                throw new InvalidOperationException($"AI HTTP request failed: {httpEx.Message}", httpEx);
            }
            catch (TaskCanceledException timeoutEx)
            {
                _logger.LogError(timeoutEx, "AI request timed out for operation '{Operation}' with provider '{Provider}'",
                    _operationType, options.ProviderName);
                throw new InvalidOperationException($"AI request timed out after {options.TimeoutSeconds} seconds", timeoutEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in AI call for operation '{Operation}' with provider '{Provider}'",
                    _operationType, options.ProviderName);
                throw new InvalidOperationException($"Unexpected AI call error: {ex.Message}", ex);
            }
        }

        /// <inheritdoc />
        public async Task<bool> IsAvailableAsync()
        {
            var options = GetAIOperationConfig();

            try
            {
                // Use DI-configured, provider-specific HttpClient
                var httpClient = GetProviderHttpClient(options.ProviderName);
                if (httpClient == null)
                {
                    _logger.LogWarning("Provider HttpClient is not available for operation '{Operation}'", _operationType);
                    return false;
                }

                // Create AI client with proper configuration
                var client = CreateAIClient(options.ProviderName, httpClient);
                if (client == null)
                {
                    _logger.LogWarning("AI client '{Provider}' is not available for operation '{Operation}'",
                        options.ProviderName, _operationType);
                    return false;
                }

                var isHealthy = await client.IsHealthyAsync();
                _logger.LogDebug("AI provider '{Provider}' availability for operation '{Operation}': {IsAvailable}",
                    options.ProviderName, _operationType, isHealthy);
                return isHealthy;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed for AI provider '{Provider}' in operation '{Operation}'",
                    options.ProviderName, _operationType);
                return false;
            }
        }

        /// <summary>
        /// Gets a DI-configured HttpClient for the specified provider.
        /// </summary>
        /// <param name="providerName">Provider identifier</param>
        /// <returns>Named HttpClient or null if unsupported</returns>
        private HttpClient? GetProviderHttpClient(string providerName)
        {
            switch (providerName.ToLowerInvariant())
            {
                case "nanogpt":
                    return _httpClientFactory.CreateClient(nameof(NanoGptClient));
                case "openrouter":
                    return _httpClientFactory.CreateClient(nameof(OpenRouterClient));
                case "claude":
                    return _httpClientFactory.CreateClient(nameof(ClaudeClient));
                case "lmstudio":
                    return _httpClientFactory.CreateClient(nameof(LMStudioClient));
                default:
                    _logger.LogWarning("Unsupported provider for HttpClient creation: {ProviderName}", providerName);
                    return null;
            }
        }


        /// <summary>
        /// Creates an AI client instance with proper Docker SSL configuration.
        /// </summary>
        /// <param name="providerName">Name of the AI provider</param>
        /// <param name="httpClient">HTTP client with Docker SSL support</param>
        /// <returns>AI client instance or null if provider not supported</returns>
        private IAIClient? CreateAIClient(string providerName, HttpClient httpClient)
        {
            // Get the domain configuration service from DI container to get proper settings
            AIProviderConfigurationService configurationService;
            if (_serviceProvider != null)
            {
                var domainSettings = _serviceProvider.GetRequiredService<IOptions<AIProjectOrchestrator.Domain.Configuration.AIProviderCredentials>>();
                configurationService = new AIProviderConfigurationService(domainSettings);
            }
            else
            {
                var domainSettings = new Microsoft.Extensions.Options.OptionsWrapper<AIProjectOrchestrator.Domain.Configuration.AIProviderCredentials>(
                    new AIProjectOrchestrator.Domain.Configuration.AIProviderCredentials());
                configurationService = new AIProviderConfigurationService(domainSettings);
            }
            _logger.LogInformation("Created AIProviderConfigurationService for operation {Operation}", _operationType);

            // Create loggers using the logger factory approach (will be replaced in refactoring 3)
            var loggerFactory = _loggerFactory;

            switch (providerName.ToLowerInvariant())
            {
                case "nanogpt":
                    _logger.LogDebug("Creating NanoGptClient for operation {Operation}", _operationType);
                    return new NanoGptClient(httpClient,
                        loggerFactory.CreateLogger<NanoGptClient>(),
                        configurationService);
                case "openrouter":
                    _logger.LogInformation("Creating OpenRouterClient for operation {Operation}", _operationType);
                    return new OpenRouterClient(httpClient,
                        loggerFactory.CreateLogger<OpenRouterClient>(),
                        configurationService);
                case "claude":
                    _logger.LogDebug("Creating ClaudeClient for operation {Operation}", _operationType);
                    return new ClaudeClient(httpClient,
                        loggerFactory.CreateLogger<ClaudeClient>(),
                        configurationService);
                case "lmstudio":
                    _logger.LogDebug("Creating LMStudioClient for operation {Operation}", _operationType);
                    return new LMStudioClient(httpClient,
                        loggerFactory.CreateLogger<LMStudioClient>(),
                        configurationService);
                default:
                    _logger.LogError("Unsupported AI provider: {ProviderName}", providerName);
                    return null!;
            }
        }

        /// <summary>
        /// Gets the configuration for the specific operation type.
        /// </summary>
        /// <returns>Operation-specific configuration</returns>
        private AIOperationConfig GetAIOperationConfig()
        {
            _logger.LogInformation("=== GetOperationConfig Debug Info ===");
            _logger.LogInformation("Looking for operation type '{Operation}' in configuration", _operationType);
            _logger.LogInformation("Available operations: {Operations}", string.Join(", ", _settings.Value.Operations.Keys));

            if (_settings.Value.Operations == null)
            {
                _logger.LogError("Operations configuration is null");
                throw new InvalidOperationException("Operations configuration is null");
            }

            if (!_settings.Value.Operations.ContainsKey(_operationType))
            {
                _logger.LogError("Operation type '{Operation}' not found in configuration. Available operations: {AvailableOperations}",
                    _operationType, string.Join(", ", _settings.Value.Operations.Keys));
                throw new InvalidOperationException($"Operation type '{_operationType}' not found in AI provider configuration");
            }

            var config = _settings.Value.Operations[_operationType];
            _logger.LogDebug("Found configuration for operation '{Operation}': Provider={Provider}, Model={Model}",
                _operationType, config.ProviderName, config.Model);

            return config;
        }
    }
}