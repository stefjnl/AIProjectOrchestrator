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

        /// <summary>
        /// Adds a new prompt template to the repository
        /// </summary>
        /// <param name="promptTemplate">The prompt template to add</param>
        /// <returns>The added prompt template</returns>
        public async Task<PromptTemplate> AddAsync(PromptTemplate promptTemplate)
        {
            promptTemplate.CreatedAt = DateTime.UtcNow;
            promptTemplate.UpdatedAt = DateTime.UtcNow;
            
            return await base.AddAsync(promptTemplate);
        }

        /// <summary>
        /// Adds a new prompt template to the repository with cancellation support
        /// </summary>
        /// <param name="promptTemplate">The prompt template to add</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The added prompt template</returns>
        public new async Task<PromptTemplate> AddAsync(PromptTemplate promptTemplate, CancellationToken cancellationToken = default)
        {
            promptTemplate.CreatedAt = DateTime.UtcNow;
            promptTemplate.UpdatedAt = DateTime.UtcNow;
            
            return await base.AddAsync(promptTemplate, cancellationToken);
        }

        /// <summary>
        /// Updates an existing prompt template in the repository
        /// </summary>
        /// <param name="promptTemplate">The prompt template to update</param>
        /// <returns>The updated prompt template</returns>
        public async Task<PromptTemplate> UpdateAsync(PromptTemplate promptTemplate)
        {
            promptTemplate.UpdatedAt = DateTime.UtcNow;
            
            await base.UpdateAsync(promptTemplate);
            return promptTemplate;
        }

        /// <summary>
        /// Updates an existing prompt template in the repository with cancellation support
        /// </summary>
        /// <param name="promptTemplate">The prompt template to update</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the update operation</returns>
        public new async Task UpdateAsync(PromptTemplate promptTemplate, CancellationToken cancellationToken = default)
        {
            promptTemplate.UpdatedAt = DateTime.UtcNow;
            
            await base.UpdateAsync(promptTemplate, cancellationToken);
        }

        /// <summary>
        /// Gets a prompt template by its ID
        /// </summary>
        /// <param name="id">The ID of the prompt template</param>
        /// <returns>The prompt template or null if not found</returns>
        public async Task<PromptTemplate?> GetByIdAsync(Guid id)
        {
            return await _dbSet.FindAsync(new object[] { id });
        }

        /// <summary>
        /// Gets a prompt template by its ID with cancellation support
        /// </summary>
        /// <param name="id">The ID of the prompt template</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The prompt template or null if not found</returns>
        public async Task<PromptTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
        }

        /// <summary>
        /// Deletes a prompt template by its ID
        /// </summary>
        /// <param name="id">The ID of the prompt template to delete</param>
        /// <returns>Task representing the delete operation</returns>
        public async Task DeleteAsync(Guid id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                _dbSet.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Deletes a prompt template by its ID with cancellation support
        /// </summary>
        /// <param name="id">The ID of the prompt template to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the delete operation</returns>
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