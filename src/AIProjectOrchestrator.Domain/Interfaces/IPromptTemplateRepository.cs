using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Interfaces;

namespace AIProjectOrchestrator.Domain.Interfaces
{
    public interface IPromptTemplateRepository : IRepository<PromptTemplate>
    {
        Task<PromptTemplate> GetByIdAsync(Guid id);
        Task DeleteAsync(Guid id);
        Task<PromptTemplate> UpdateAsync(PromptTemplate promptTemplate);
    }
}