using System;
using System.Collections.Generic;
using AIProjectOrchestrator.Domain.Configuration;
using Microsoft.Extensions.Options;

namespace AIProjectOrchestrator.Domain.Services
{
    public class AIProviderConfigurationService
    {
        private readonly IOptions<AIProviderCredentials> _settings;

        public AIProviderConfigurationService(IOptions<AIProviderCredentials> settings)
        {
            _settings = settings;
        }

        public T GetProviderSettings<T>(string providerName) where T : class
        {
            return providerName switch
            {
                ProviderNames.Claude => _settings.Value.Claude as T ?? throw new InvalidCastException($"Cannot convert ClaudeSettings to {typeof(T).Name}"),
                ProviderNames.LMStudio => _settings.Value.LMStudio as T ?? throw new InvalidCastException($"Cannot convert LMStudioSettings to {typeof(T).Name}"),
                ProviderNames.OpenRouter => _settings.Value.OpenRouter as T ?? throw new InvalidCastException($"Cannot convert OpenRouterSettings to {typeof(T).Name}"),
                ProviderNames.NanoGpt => _settings.Value.NanoGpt as T ?? throw new InvalidCastException($"Cannot convert NanoGptSettings to {typeof(T).Name}"),
                ProviderNames.AlibabaCloud => _settings.Value.AlibabaCloud as T ?? throw new InvalidCastException($"Cannot convert AlibabaCloudSettings to {typeof(T).Name}"),
                _ => throw new ArgumentException($"Invalid provider name: {providerName}")
            };
        }

        public object GetProviderSettings(string providerName)
        {
            return providerName switch
            {
                ProviderNames.Claude => _settings.Value.Claude,
                ProviderNames.LMStudio => _settings.Value.LMStudio,
                ProviderNames.OpenRouter => _settings.Value.OpenRouter,
                ProviderNames.NanoGpt => _settings.Value.NanoGpt,
                ProviderNames.AlibabaCloud => _settings.Value.AlibabaCloud,
                _ => throw new ArgumentException($"Invalid provider name: {providerName}")
            };
        }

        public IEnumerable<string> GetProviderNames()
        {
            return new List<string>
            {
                ProviderNames.Claude,
                ProviderNames.LMStudio,
                ProviderNames.OpenRouter,
                ProviderNames.NanoGpt,
                ProviderNames.AlibabaCloud
            };
        }
    }
}