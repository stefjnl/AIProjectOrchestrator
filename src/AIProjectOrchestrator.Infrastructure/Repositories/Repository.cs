using AIProjectOrchestrator.Domain.Common;
using AIProjectOrchestrator.Domain.Interfaces;
using AIProjectOrchestrator.Domain.Models;
using AIProjectOrchestrator.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIProjectOrchestrator.Infrastructure.Repositories
{
    /// <summary>
    /// Base repository implementation for entities with integer IDs.
    /// Implements IFullRepository following Interface Segregation Principle (ISP).
    /// For entities with Guid IDs, use PromptTemplateRepository as a reference.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    public class Repository<T> : IFullRepository<T, int> where T : class
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        /// <summary>
        /// Gets an entity by its integer ID
        /// </summary>
        /// <param name="id">The ID of the entity</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The entity or null if not found</returns>
        public virtual async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FindAsync(new object[] { id }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets all entities in the repository.
        /// WARNING: This method loads the entire table into memory. For large datasets, 
        /// use GetQueryable() or GetPagedAsync() instead for better performance.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of all entities</returns>
        public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.ToListAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets a queryable for efficient filtering and pagination.
        /// Use this for complex queries or when dealing with large datasets.
        /// The query is not executed until enumerated or a ToListAsync/FirstOrDefaultAsync call is made.
        /// </summary>
        /// <returns>IQueryable for deferred execution</returns>
        public virtual IQueryable<T> GetQueryable()
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
        public virtual async Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            // Validate parameters
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100; // Limit max page size

            // Get total count (single query)
            var totalCount = await _dbSet.CountAsync(cancellationToken).ConfigureAwait(false);

            // Get paged items (single query with SKIP/TAKE)
            var items = await _dbSet
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new PagedResult<T>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// Adds a new entity to the repository
        /// </summary>
        /// <param name="entity">The entity to add</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The added entity</returns>
        public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            await _dbSet.AddAsync(entity, cancellationToken).ConfigureAwait(false);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return entity;
        }

        /// <summary>
        /// Updates an existing entity in the repository
        /// </summary>
        /// <param name="entity">The entity to update</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the update operation</returns>
        public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes an entity by its integer ID
        /// </summary>
        /// <param name="id">The ID of the entity to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the delete operation</returns>
        public virtual async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (entity != null)
            {
                _dbSet.Remove(entity);
                await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        // REMOVED: GetByStringIdAsync method
        // This method had serious performance issues:
        // 1. Used reflection which doesn't translate to SQL
        // 2. Called ToListAsync() to load ENTIRE table into memory
        // 3. Then filtered in C# code - very inefficient
        // 4. Was never used in production code (0 usages found)
        // 5. All tests for this method were marked as [Skip] due to implementation issues
        // 
        // If string-based lookups are needed, create domain-specific methods
        // like GetByAnalysisIdAsync, GetByGenerationIdAsync, etc.
    }
}
