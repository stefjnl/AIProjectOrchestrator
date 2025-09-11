using System;
using Microsoft.Extensions.Logging;

namespace AIProjectOrchestrator.Infrastructure.AI
{
    public class AIClientLogger
    {
        private static readonly ILogger _logger;

        static AIClientLogger()
        {
            // Initialize the logger (depends on your logging setup)
            _logger = LoggerFactory.Create(builder => { builder.AddConsole(); }).CreateLogger("AIClientLogger");
        }

        public static void LogSettings(string providerName, string baseUrl, int apiKeyLength, string defaultModel)
        {
            _logger.LogInformation("{ProviderName} Settings - BaseUrl: {BaseUrl}, ApiKey Length: {ApiKeyLength}, DefaultModel: {DefaultModel}",
                providerName, baseUrl, apiKeyLength, defaultModel);
        }

        public static void LogApiKeyPrefix(string providerName, string apiKeyPrefix)
        {
            _logger.LogInformation("{ProviderName} API Key prefix: {ApiKeyPrefix}", providerName, apiKeyPrefix);
        }

        public static void LogRequestUrl(string providerName, string requestUrl)
        {
            _logger.LogInformation("{ProviderName} Request URL: {RequestUrl}", providerName, requestUrl);
        }

        public static void LogRequestHeaders(string providerName, string headers)
        {
            _logger.LogInformation("{ProviderName} Request Headers: {Headers}", providerName, headers);
        }

        public static void LogRequestContent(string providerName, string requestContent)
        {
            _logger.LogInformation("{ProviderName} Request Content: {RequestContent}", providerName, requestContent);
        }

        public static void LogResponse(string providerName, System.Net.HttpStatusCode statusCode, int contentLength, string contentStart)
        {
            _logger.LogInformation("{ProviderName} API Response - Status: {StatusCode}, Content Length: {ContentLength}, Content Start: {ContentStart}",
                providerName, statusCode, contentLength, contentStart);
        }

        public static void LogRetryAttempt(string providerName, int attempt, System.Net.HttpStatusCode statusCode)
        {
            _logger.LogWarning("Attempt {Attempt} failed with status {StatusCode} for provider {ProviderName}. Retrying...",
                attempt, statusCode, providerName);
        }

        public static void LogNetworkError(string providerName, int attempt, Exception ex)
        {
            _logger.LogWarning(ex, "Attempt {Attempt} failed with network error for provider {ProviderName}. Retrying...",
                attempt, providerName);
        }

        public static void LogException(string providerName, int attempt, Exception ex)
        {
            _logger.LogWarning(ex, "Attempt {Attempt} failed with exception for provider {ProviderName}. Retrying...",
                attempt, providerName);
        }
    }
}