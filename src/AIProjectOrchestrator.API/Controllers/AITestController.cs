using Microsoft.AspNetCore.Mvc;
using AIProjectOrchestrator.Domain.Models.AI;
using AIProjectOrchestrator.Domain.Services;
using AIProjectOrchestrator.Infrastructure.AI;
using System.Threading.Tasks;

namespace AIProjectOrchestrator.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AITestController : ControllerBase
    {
        private readonly IAIClientFactory _clientFactory;

        public AITestController(IAIClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        [HttpPost("claude")]
        public async Task<ActionResult<AIResponse>> TestClaude([FromBody] AIRequest request)
        {
            var client = _clientFactory.GetClient("Claude");
            if (client == null)
            {
                return BadRequest("Claude client not available");
            }

            var response = await client.CallAsync(request);
            return Ok(response);
        }
        
        [HttpPost("lmstudio")]
        public async Task<ActionResult<AIResponse>> TestLMStudio([FromBody] AIRequest request)
        {
            var client = _clientFactory.GetClient("LMStudio");
            if (client == null)
            {
                return BadRequest("LMStudio client not available");
            }

            var response = await client.CallAsync(request);
            return Ok(response);
        }
        
        [HttpPost("openrouter")]
        public async Task<ActionResult<AIResponse>> TestOpenRouter([FromBody] AIRequest request)
        {
            var client = _clientFactory.GetClient("OpenRouter");
            if (client == null)
            {
                return BadRequest("OpenRouter client not available");
            }

            var response = await client.CallAsync(request);
            return Ok(response);
        }
    }
}