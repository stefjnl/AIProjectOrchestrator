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
            var providers = await _service.GetAvailableProvidersAsync().ConfigureAwait(false);
            return Ok(providers);
        }

        [HttpGet("health/{name}")]
        public async Task<ActionResult<object>> GetProviderHealth(string name)
        {
            var health = await _service.GetProviderHealthAsync(name).ConfigureAwait(false);
            return Ok(health);
        }

        [HttpGet("current")]
        public async Task<ActionResult<string>> GetCurrentProvider()
        {
            var current = await _defaultService.GetDefaultProviderAsync().ConfigureAwait(false) ?? "NanoGpt";
            return Ok(current);
        }

        [HttpPost("switch")]
        public async Task<ActionResult> SwitchProvider([FromBody] SwitchProviderRequest request)
        {
            if (string.IsNullOrEmpty(request.Provider))
            {
                return BadRequest("Provider is required");
            }

            if (!await _service.IsValidProviderAsync(request.Provider).ConfigureAwait(false))
            {
                return BadRequest("Invalid provider");
            }

            await _defaultService.SetDefaultProviderAsync(request.Provider).ConfigureAwait(false);
            return Ok(new { Message = $"Switched to {request.Provider}" });
        }

        public class SwitchProviderRequest
        {
            public string Provider { get; set; } = string.Empty;
        }
    }
}
