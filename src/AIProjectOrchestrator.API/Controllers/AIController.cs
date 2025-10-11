using Microsoft.AspNetCore.Mvc;
using AIProjectOrchestrator.Domain.Models.AI;
using AIProjectOrchestrator.Domain.Services;
using AIProjectOrchestrator.Infrastructure.AI;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Configuration;

namespace AIProjectOrchestrator.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AIController : ControllerBase
    {
        private readonly IAIClientFactory _clientFactory;

        public AIController(IAIClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        [HttpPost("generate")]
        public async Task<ActionResult<AIResponse>> Generate([FromBody] AIRequest request)
        {
            // For the prompt playground, we'll use a default model
            // In a real implementation, this might be configurable
            var client = _clientFactory.GetClient(ProviderNames.Claude);
            if (client == null)
            {
                return BadRequest("AI client not available");
            }

            var response = await client.CallAsync(request);
            return Ok(response);
        }
    }
}