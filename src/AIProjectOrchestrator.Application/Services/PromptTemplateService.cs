using AIProjectOrchestrator.Application.Interfaces;
using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Interfaces;

namespace AIProjectOrchestrator.Application.Services
{
    public class PromptTemplateService : IPromptTemplateService
    {
        private readonly IPromptTemplateRepository _repository;

        public PromptTemplateService(IPromptTemplateRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<PromptTemplate>> GetAllTemplatesAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<PromptTemplate?> GetTemplateByIdAsync(Guid id)
        {
            return await ((IPromptTemplateRepository)_repository).GetByIdAsync(id);
        }

        public async Task<PromptTemplate> CreateTemplateAsync(PromptTemplate promptTemplate)
        {
            return await _repository.AddAsync(promptTemplate);
        }

        public async Task<PromptTemplate> UpdateTemplateAsync(PromptTemplate promptTemplate)
        {
            await _repository.UpdateAsync(promptTemplate);
            return promptTemplate;
        }

        public async Task DeleteTemplateAsync(Guid id)
        {
            await ((IPromptTemplateRepository)_repository).DeleteAsync(id);
        }
    }
}