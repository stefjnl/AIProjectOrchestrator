using Microsoft.AspNetCore.Mvc;
using AIProjectOrchestrator.Domain.Exceptions;

namespace AIProjectOrchestrator.API.Controllers
{
    /// <summary>
    /// Test controller to verify exception handling middleware functionality.
    /// </summary>
    [ApiController]
    [Route("api/test-exception")]
    public class TestExceptionController : ControllerBase
    {
        /// <summary>
        /// Test endpoint that throws different types of exceptions.
        /// </summary>
        [HttpGet("{exceptionType}")]
        public IActionResult ThrowException(string exceptionType)
        {
            return exceptionType.ToLower() switch
            {
                "validation" => throw new ValidationException("Test validation exception"),
                "ai-provider" => throw new AIProviderException("TestProvider", "Test AI provider exception"),
                "argument" => throw new ArgumentException("Test argument exception"),
                "key-not-found" => throw new KeyNotFoundException("Test key not found exception"),
                "unauthorized" => throw new UnauthorizedAccessException("Test unauthorized exception"),
                _ => throw new InvalidOperationException("Test general exception")
            };
        }
    }
}