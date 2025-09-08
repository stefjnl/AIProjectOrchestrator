using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Interfaces;
using AIProjectOrchestrator.Infrastructure.Data;

namespace AIProjectOrchestrator.Infrastructure.Repositories
{
    public class PromptTemplateRepository : Repository<PromptTemplate>, IPromptTemplateRepository
    {
        public PromptTemplateRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<PromptTemplate> AddAsync(PromptTemplate promptTemplate)
        {
            promptTemplate.CreatedAt = DateTime.UtcNow;
            promptTemplate.UpdatedAt = DateTime.UtcNow;

            return await base.AddAsync(promptTemplate);
        }

        public async Task<PromptTemplate> UpdateAsync(PromptTemplate promptTemplate)
        {
            promptTemplate.UpdatedAt = DateTime.UtcNow;

            await base.UpdateAsync(promptTemplate);
            return promptTemplate;
        }

        public async Task<PromptTemplate> GetByIdAsync(Guid id)
        {
            return await _dbSet.FindAsync(new object[] { id });
        }

        public async Task<PromptTemplate> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                _dbSet.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var entity = await GetByIdAsync(id, cancellationToken);
            if (entity != null)
            {
                _dbSet.Remove(entity);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}