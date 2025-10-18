using AIProjectOrchestrator.Domain.Entities;
using AIProjectOrchestrator.Domain.Interfaces;
using AIProjectOrchestrator.Domain.Models;
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
        /// Gets all prompt templates.
        /// WARNING: This method loads the entire table into memory. For large datasets, 
        /// use GetQueryable() or GetPagedAsync() instead for better performance.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of all prompt templates</returns>
        public async Task<IEnumerable<PromptTemplate>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.ToListAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets a queryable for efficient filtering and pagination.
        /// Use this for complex queries or when dealing with large datasets.
        /// </summary>
        /// <returns>IQueryable for deferred execution</returns>
        public IQueryable<PromptTemplate> GetQueryable()
        {
            return _dbSet.AsQueryable();
        }

        /// <summary>
        /// Gets a paged result set with metadata for pagination.
        /// </summary>
        /// <param name="pageNumber">The page number (1-based)</param>
        /// <param name="pageSize">The number of items per page</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paged result with items and pagination metadata</returns>
        public async Task<PagedResult<PromptTemplate>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            // Validate parameters
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100; // Limit max page size

            // Get total count
            var totalCount = await _dbSet.CountAsync(cancellationToken).ConfigureAwait(false);

            // Get paged items
            var items = await _dbSet
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new PagedResult<PromptTemplate>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
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