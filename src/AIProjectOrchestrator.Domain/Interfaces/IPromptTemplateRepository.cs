using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Interfaces;

namespace AIProjectOrchestrator.Domain.Interfaces
{
    /// <summary>
    /// Repository for PromptTemplate entities (Guid ID).
    /// Follows Interface Segregation Principle by using IFullRepository with Guid as ID type.
    /// </summary>
    public interface IPromptTemplateRepository : IFullRepository<PromptTemplate, Guid>
    {
        // IFullRepository provides: GetByIdAsync(Guid), GetAllAsync, AddAsync, UpdateAsync, DeleteAsync(Guid)
        // All methods are provided by the base interface with proper Guid ID type
    }
}