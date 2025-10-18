using AIProjectOrchestrator.Domain.Common;
using AIProjectOrchestrator.Domain.Interfaces;
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
        /// Gets all entities in the repository
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of all entities</returns>
        public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.ToListAsync(cancellationToken).ConfigureAwait(false);
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
