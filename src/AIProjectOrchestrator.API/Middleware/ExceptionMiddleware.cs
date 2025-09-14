using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Exceptions;
using AIProjectOrchestrator.API.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace AIProjectOrchestrator.API.Middleware
{
    /// <summary>
    /// Global exception handling middleware that provides consistent error responses
    /// with correlation ID tracking for debugging and monitoring.
    /// </summary>
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly ExceptionHandlingOptions _options;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IWebHostEnvironment env, IOptions<ExceptionHandlingOptions> options)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _env = env ?? throw new ArgumentNullException(nameof(env));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Processes HTTP requests and handles any unhandled exceptions.
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            // Handle correlation ID for request tracking
            var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                ?? Guid.NewGuid().ToString();
            
            context.Items["CorrelationId"] = correlationId;
            if (_options.IncludeCorrelationId)
            {
                context.Response.Headers["X-Correlation-ID"] = correlationId;
            }

            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Use configuration option for request logging
                if (_options.LogAllRequests || _env.IsDevelopment())
                {
                    _logger.LogDebug("Processing request {Method} {Path} with CorrelationId: {CorrelationId}",
                        context.Request.Method, context.Request.Path, correlationId);
                }
                
                await _next(context);
                
                stopwatch.Stop();
                
                // Log only error responses (4xx, 5xx) in production unless configured otherwise
                if (_options.LogAllRequests || _env.IsDevelopment())
                {
                    _logger.LogDebug("Request completed successfully in {ElapsedMs}ms. CorrelationId: {CorrelationId}",
                        stopwatch.ElapsedMilliseconds, correlationId);
                }
                else if (context.Response.StatusCode >= 400)
                {
                    _logger.LogWarning("Request failed with status {StatusCode} for {Method} {Path}. CorrelationId: {CorrelationId}",
                        context.Response.StatusCode, context.Request.Method, context.Request.Path, correlationId);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Request failed for {Method} {Path}. CorrelationId: {CorrelationId}",
                    context.Request.Method, context.Request.Path, correlationId);
                await HandleExceptionAsync(context, ex, correlationId);
            }
        }

        /// <summary>
        /// Handles exceptions by mapping them to appropriate HTTP status codes and responses.
        /// </summary>
        private async Task HandleExceptionAsync(HttpContext context, Exception ex, string correlationId)
        {
            var (statusCode, message) = MapException(ex);
            
            var response = new ProblemDetails
            {
                Status = statusCode,
                Title = GetTitle(statusCode),
                Detail = message,
                Instance = context.Request.Path,
                Extensions = { ["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") }
            };

            // Add correlation ID if configured
            if (_options.IncludeCorrelationId)
            {
                response.Extensions["correlationId"] = correlationId;
            }

            // Add stack trace if configured or in development
            if (_options.IncludeStackTrace || _env.IsDevelopment())
            {
                response.Extensions["stackTrace"] = ex.StackTrace;
            }

            // Add detailed error messages for client errors if configured
            if (statusCode >= 400 && statusCode < 500 && _options.IncludeDetailedClientErrors)
            {
                response.Extensions["errorDetails"] = GetErrorDetails(ex);
            }

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";
            
            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
            
            await context.Response.WriteAsync(jsonResponse);
        }

        /// <summary>
        /// Gets detailed error information for client errors.
        /// </summary>
        private static string GetErrorDetails(Exception ex)
        {
            return ex switch
            {
                ValidationException validationEx => $"Validation failed: {validationEx.Message}",
                ArgumentException argEx => $"Invalid argument: {argEx.Message}",
                InvalidOperationException opEx => $"Invalid operation: {opEx.Message}",
                _ => ex.Message
            };
        }

        /// <summary>
        /// Maps exceptions to HTTP status codes and user-friendly messages.
        /// </summary>
        private static (int statusCode, string message) MapException(Exception ex) => ex switch
        {
            AIProviderException providerEx => (503, $"AI service '{providerEx.ProviderName}' is temporarily unavailable"),
            ValidationException validationEx => (400, validationEx.Message),
            ArgumentException argEx => (400, argEx.Message),
            UnauthorizedAccessException => (401, "Access denied. Please authenticate."),
            KeyNotFoundException => (404, "The requested resource was not found"),
            NotImplementedException => (501, "This feature is not yet implemented"),
            _ => (500, "An unexpected error occurred. Please try again later.")
        };

        /// <summary>
        /// Gets the appropriate title for the HTTP status code.
        /// </summary>
        private static string GetTitle(int statusCode) => statusCode switch
        {
            400 => "Bad Request",
            401 => "Unauthorized", 
            404 => "Not Found",
            500 => "Internal Server Error",
            501 => "Not Implemented",
            503 => "Service Unavailable",
            _ => "Error"
        };
    }
}