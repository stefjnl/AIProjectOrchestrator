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
                "Claude" => _settings.Value.Claude as T ?? throw new InvalidCastException($"Cannot convert ClaudeSettings to {typeof(T).Name}"),
                "LMStudio" => _settings.Value.LMStudio as T ?? throw new InvalidCastException($"Cannot convert LMStudioSettings to {typeof(T).Name}"),
                "OpenRouter" => _settings.Value.OpenRouter as T ?? throw new InvalidCastException($"Cannot convert OpenRouterSettings to {typeof(T).Name}"),
                "NanoGpt" => _settings.Value.NanoGpt as T ?? throw new InvalidCastException($"Cannot convert NanoGptSettings to {typeof(T).Name}"),
                "AlibabaCloud" => _settings.Value.AlibabaCloud as T ?? throw new InvalidCastException($"Cannot convert AlibabaCloudSettings to {typeof(T).Name}"),
                _ => throw new ArgumentException($"Invalid provider name: {providerName}")
            };
        }

        public object GetProviderSettings(string providerName)
        {
            return providerName switch
            {
                "Claude" => _settings.Value.Claude,
                "LMStudio" => _settings.Value.LMStudio,
                "OpenRouter" => _settings.Value.OpenRouter,
                "NanoGpt" => _settings.Value.NanoGpt,
                "AlibabaCloud" => _settings.Value.AlibabaCloud,
                _ => throw new ArgumentException($"Invalid provider name: {providerName}")
            };
        }

        public IEnumerable<string> GetProviderNames()
        {
            return new List<string> { "Claude", "LMStudio", "OpenRouter", "NanoGpt", "AlibabaCloud" };
        }
    }
}