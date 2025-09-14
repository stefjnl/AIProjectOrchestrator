using Microsoft.AspNetCore.Mvc;
using AIProjectOrchestrator.Application.Interfaces;

namespace AIProjectOrchestrator.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProviderManagementController : ControllerBase
    {
        private readonly IProviderManagementService _service;
        private readonly IDefaultProviderService _defaultService;

        public ProviderManagementController(IProviderManagementService service, IDefaultProviderService defaultService)
        {
            _service = service;
            _defaultService = defaultService;
        }

        [HttpGet("providers")]
        public async Task<ActionResult<IEnumerable<string>>> GetAvailableProviders()
        {
            var providers = await _service.GetAvailableProvidersAsync();
            return Ok(providers);
        }

        [HttpGet("health/{name}")]
        public async Task<ActionResult<object>> GetProviderHealth(string name)
        {
            var health = await _service.GetProviderHealthAsync(name);
            return Ok(health);
        }

        [HttpGet("current")]
        public async Task<ActionResult<string>> GetCurrentProvider()
        {
            var current = await _defaultService.GetDefaultProviderAsync() ?? "NanoGpt";
            return Ok(current);
        }

        [HttpPost("switch")]
        public async Task<ActionResult> SwitchProvider([FromBody] SwitchProviderRequest request)
        {
            if (string.IsNullOrEmpty(request.Provider))
            {
                return BadRequest("Provider is required");
            }

            if (!await _service.IsValidProviderAsync(request.Provider))
            {
                return BadRequest("Invalid provider");
            }

            await _defaultService.SetDefaultProviderAsync(request.Provider);
            return Ok(new { Message = $"Switched to {request.Provider}" });
        }

        public class SwitchProviderRequest
        {
            public string Provider { get; set; } = string.Empty;
        }
    }
}
