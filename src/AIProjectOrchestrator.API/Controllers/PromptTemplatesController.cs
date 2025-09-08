using Microsoft.AspNetCore.Mvc;
using AIProjectOrchestrator.Application.Interfaces;
using AIProjectOrchestrator.Domain.Entities;

namespace AIProjectOrchestrator.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PromptTemplatesController : ControllerBase
    {
        private readonly IPromptTemplateService _service;

        public PromptTemplatesController(IPromptTemplateService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PromptTemplate>>> GetAll()
        {
            var templates = await _service.GetAllTemplatesAsync();
            // Ensure we're returning an array, not an object
            var templateList = templates.ToList();
            return Ok(templateList);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<PromptTemplate>> GetById(Guid id)
        {
            var template = await _service.GetTemplateByIdAsync(id);
            if (template == null)
            {
                return NotFound();
            }
            return Ok(template);
        }

        [HttpPost]
        public async Task<ActionResult<PromptTemplate>> CreateOrUpdate(PromptTemplate promptTemplate)
        {
            if (promptTemplate.Id == Guid.Empty)
            {
                var created = await _service.CreateTemplateAsync(promptTemplate);
                return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
            }
            else
            {
                var updated = await _service.UpdateTemplateAsync(promptTemplate);
                return Ok(updated);
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _service.DeleteTemplateAsync(id);
            return NoContent();
        }
    }
}