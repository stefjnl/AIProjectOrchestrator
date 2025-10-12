using AIProjectOrchestrator.Domain.Common;
using AIProjectOrchestrator.Domain.Interfaces;
using AIProjectOrchestrator.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIProjectOrchestrator.Infrastructure.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
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
        public async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
        }

        /// <summary>
        /// Gets an entity by its string ID
        /// </summary>
        /// <param name="id">The string ID of the entity</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The entity or null if not found</returns>
        public async Task<T?> GetByStringIdAsync(string id, CancellationToken cancellationToken = default)
        {
            // This is a generic implementation that assumes the entity has an "Id" property of type string
            // For specific entities, we'll override this in specialized repositories
            var property = typeof(T).GetProperty(PropertyNameConstants.Id) ?? typeof(T).GetProperty(PropertyNameConstants.ReviewId);
            if (property != null)
            {
                var entities = await _dbSet.ToListAsync(cancellationToken);
                return entities.FirstOrDefault(e =>
                {
                    var propertyValue = property.GetValue(e);
                    return propertyValue != null && propertyValue.ToString() == id;
                });
            }
            
            throw new InvalidOperationException($"Entity {typeof(T).Name} does not have an Id property");
        }

        /// <summary>
        /// Gets all entities in the repository
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of all entities</returns>
        public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Adds a new entity to the repository
        /// </summary>
        /// <param name="entity">The entity to add</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The added entity</returns>
        public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            await _dbSet.AddAsync(entity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return entity;
        }

        /// <summary>
        /// Updates an existing entity in the repository
        /// </summary>
        /// <param name="entity">The entity to update</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the update operation</returns>
        public async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Deletes an entity by its integer ID
        /// </summary>
        /// <param name="id">The ID of the entity to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the delete operation</returns>
        public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
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
