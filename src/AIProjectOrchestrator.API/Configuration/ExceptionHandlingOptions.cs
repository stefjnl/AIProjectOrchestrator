namespace AIProjectOrchestrator.API.Configuration
{
    /// <summary>
    /// Configuration options for exception handling behavior.
    /// </summary>
    public class ExceptionHandlingOptions
    {
        /// <summary>
        /// Whether to include exception stack traces in error responses.
        /// Default: false in production, true in development.
        /// </summary>
        public bool IncludeStackTrace { get; set; }

        /// <summary>
        /// Whether to log all requests (verbose logging).
        /// Default: false in production, true in development.
        /// </summary>
        public bool LogAllRequests { get; set; }

        /// <summary>
        /// Whether to include detailed error messages for client errors (4xx).
        /// Default: true.
        /// </summary>
        public bool IncludeDetailedClientErrors { get; set; } = true;

        /// <summary>
        /// Whether to include correlation IDs in error responses.
        /// Default: true.
        /// </summary>
        public bool IncludeCorrelationId { get; set; } = true;
    }
}