using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Interfaces;
using AIProjectOrchestrator.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIProjectOrchestrator.Infrastructure.Repositories
{
    /// <summary>
    /// Repository for PromptTemplate entities with Guid IDs.
    /// Implements IFullRepository<PromptTemplate, Guid> following Interface Segregation Principle (ISP).
    /// </summary>
    public class PromptTemplateRepository : IPromptTemplateRepository
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<PromptTemplate> _dbSet;

        public PromptTemplateRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Set<PromptTemplate>();
        }

        /// <summary>
        /// Gets a prompt template by its Guid ID
        /// </summary>
        /// <param name="id">The Guid ID of the prompt template</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The prompt template or null if not found</returns>
        public async Task<PromptTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FindAsync(new object[] { id }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets all prompt templates
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of all prompt templates</returns>
        public async Task<IEnumerable<PromptTemplate>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.ToListAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Adds a new prompt template to the repository
        /// </summary>
        /// <param name="promptTemplate">The prompt template to add</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The added prompt template</returns>
        public async Task<PromptTemplate> AddAsync(PromptTemplate promptTemplate, CancellationToken cancellationToken = default)
        {
            promptTemplate.CreatedAt = DateTime.UtcNow;
            promptTemplate.UpdatedAt = DateTime.UtcNow;
            
            await _dbSet.AddAsync(promptTemplate, cancellationToken).ConfigureAwait(false);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return promptTemplate;
        }

        /// <summary>
        /// Updates an existing prompt template in the repository
        /// </summary>
        /// <param name="promptTemplate">The prompt template to update</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the update operation</returns>
        public async Task UpdateAsync(PromptTemplate promptTemplate, CancellationToken cancellationToken = default)
        {
            promptTemplate.UpdatedAt = DateTime.UtcNow;
            
            _dbSet.Update(promptTemplate);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes a prompt template by its Guid ID
        /// </summary>
        /// <param name="id">The Guid ID of the prompt template to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the delete operation</returns>
        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var entity = await GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (entity != null)
            {
                _dbSet.Remove(entity);
                await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}