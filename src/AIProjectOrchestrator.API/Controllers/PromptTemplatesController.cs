using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AIProjectOrchestrator.Application.Interfaces;
using AIProjectOrchestrator.Domain.Entities;

namespace AIProjectOrchestrator.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PromptTemplatesController : ControllerBase
    {
        private readonly IPromptTemplateService _service;
        private readonly ILogger<PromptTemplatesController> _logger;

        public PromptTemplatesController(IPromptTemplateService service, ILogger<PromptTemplatesController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PromptTemplate>>> GetAll()
        {
            var templates = await _service.GetAllTemplatesAsync().ConfigureAwait(false);
            // Ensure we're returning an array, not an object
            var templateList = templates.ToList();
            return Ok(templateList);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<PromptTemplate>> GetById(Guid id)
        {
            var template = await _service.GetTemplateByIdAsync(id).ConfigureAwait(false);
            if (template == null)
            {
                return NotFound();
            }
            return Ok(template);
        }

        [HttpPost]
        public async Task<ActionResult<PromptTemplate>> CreateOrUpdate(PromptTemplate? promptTemplate)
        {
            _logger.LogInformation("Received prompt template: {@PromptTemplate}", promptTemplate);

            if (promptTemplate == null)
            {
                _logger.LogWarning("Received null promptTemplate");
                return BadRequest("Prompt template cannot be null");
            }

            _logger.LogInformation("Binding successful. Title: '{Title}', Content length: {ContentLength}",
                promptTemplate.Title, promptTemplate.Content?.Length ?? 0);

            if (promptTemplate.Id == Guid.Empty)
            {
                var created = await _service.CreateTemplateAsync(promptTemplate).ConfigureAwait(false);
                _logger.LogInformation("Created template with ID: {Id}", created.Id);
                return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
            }
            else
            {
                var updated = await _service.UpdateTemplateAsync(promptTemplate).ConfigureAwait(false);
                _logger.LogInformation("Updated template with ID: {Id}", updated.Id);
                return Ok(updated);
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _service.DeleteTemplateAsync(id).ConfigureAwait(false);
            return NoContent();
        }
    }
}