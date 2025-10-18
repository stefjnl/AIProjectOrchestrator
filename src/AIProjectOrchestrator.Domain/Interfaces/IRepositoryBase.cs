using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AIProjectOrchestrator.Domain.Models;

namespace AIProjectOrchestrator.Domain.Interfaces
{
    /// <summary>
    /// Base repository interface for read operations.
    /// Follows Interface Segregation Principle (ISP) by separating concerns.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <typeparam name="TId">The ID type (int, Guid, string, etc.)</typeparam>
    public interface IReadRepository<T, TId> where T : class
    {
        /// <summary>
        /// Gets an entity by its ID
        /// </summary>
        /// <param name="id">The ID of the entity</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The entity or null if not found</returns>
        Task<T?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all entities in the repository.
        /// WARNING: This method loads the entire table into memory. For large datasets, 
        /// use GetQueryable() or GetPagedAsync() instead for better performance.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of all entities</returns>
        Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a queryable for efficient filtering and pagination.
        /// Use this for complex queries or when dealing with large datasets.
        /// </summary>
        /// <returns>IQueryable for deferred execution</returns>
        IQueryable<T> GetQueryable();

        /// <summary>
        /// Gets a paged result set with metadata for pagination.
        /// </summary>
        /// <param name="pageNumber">The page number (1-based)</param>
        /// <param name="pageSize">The number of items per page</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paged result with items and pagination metadata</returns>
        Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Repository interface for write operations.
    /// Follows Interface Segregation Principle (ISP) by separating concerns.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    public interface IWriteRepository<T> where T : class
    {
        /// <summary>
        /// Adds a new entity to the repository
        /// </summary>
        /// <param name="entity">The entity to add</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The added entity</returns>
        Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing entity in the repository
        /// </summary>
        /// <param name="entity">The entity to update</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the update operation</returns>
        Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Repository interface for delete operations.
    /// Follows Interface Segregation Principle (ISP) by separating concerns.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <typeparam name="TId">The ID type (int, Guid, string, etc.)</typeparam>
    public interface IDeleteRepository<T, TId> where T : class
    {
        /// <summary>
        /// Deletes an entity by its ID
        /// </summary>
        /// <param name="id">The ID of the entity to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the delete operation</returns>
        Task DeleteAsync(TId id, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Full repository interface combining read, write, and delete operations.
    /// Use this when you need full CRUD capabilities.
    /// For read-only or write-only scenarios, use IReadRepository or IWriteRepository instead.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <typeparam name="TId">The ID type (int, Guid, string, etc.)</typeparam>
    public interface IFullRepository<T, TId> : IReadRepository<T, TId>, IWriteRepository<T>, IDeleteRepository<T, TId>
        where T : class
    {
        // This interface combines all repository operations
        // Clients can depend on smaller interfaces (IReadRepository, IWriteRepository, etc.)
        // following the Interface Segregation Principle
    }
}
