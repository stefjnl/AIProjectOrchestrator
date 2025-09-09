using AIProjectOrchestrator.Domain.Entities;

namespace AIProjectOrchestrator.Application.Interfaces
{
    public interface IPromptTemplateService
    {
        Task<IEnumerable<PromptTemplate>> GetAllTemplatesAsync();
        Task<PromptTemplate?> GetTemplateByIdAsync(Guid id);
        Task<PromptTemplate> CreateTemplateAsync(PromptTemplate promptTemplate);
        Task<PromptTemplate> UpdateTemplateAsync(PromptTemplate promptTemplate);
        Task DeleteTemplateAsync(Guid id);
    }
}