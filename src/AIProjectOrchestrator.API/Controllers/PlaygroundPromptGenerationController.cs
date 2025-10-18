using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AIProjectOrchestrator.API.Controllers
{
    [ApiController]
    [Route("api/playground-prompt-generation")]
    public class PlaygroundPromptGenerationController : ControllerBase
    {
        private readonly IPromptGenerationService _promptGenerationService;
        private readonly ILogger<PlaygroundPromptGenerationController> _logger;

        public PlaygroundPromptGenerationController(IPromptGenerationService promptGenerationService, ILogger<PlaygroundPromptGenerationController> logger)
        {
            _promptGenerationService = promptGenerationService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> GeneratePrompt([FromBody] PlaygroundPromptRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.PromptContent))
            {
                return BadRequest("Prompt content is required.");
            }

            try
            {
                var response = await _promptGenerationService.GeneratePromptFromPlaygroundAsync(request.PromptContent).ConfigureAwait(false);
                return Ok(response);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error generating prompt from playground");
                return StatusCode(500, "Internal server error");
            }
        }
    }

    public class PlaygroundPromptRequest
    {
        public string? PromptContent { get; set; }
    }
}