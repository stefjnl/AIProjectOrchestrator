namespace AIProjectOrchestrator.Domain.Interfaces
{
    /// <summary>
    /// Legacy repository interface. This interface violates Interface Segregation Principle (ISP)
    /// by forcing all implementers to support methods they may not need.
    /// Use IRepository&lt;T, TId&gt; from IRepositoryBase.cs instead.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    [System.Obsolete("This interface violates ISP. Use IRepository<T, TId> or smaller interfaces (IReadRepository, IWriteRepository, etc.) from IRepositoryBase.cs instead. This will be removed in a future version.", error: false)]
    public interface IRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// This method has performance issues (uses reflection, loads entire table) and is not used in production.
        /// Use GetByIdAsync with proper ID type instead.
        /// </summary>
        [System.Obsolete("This method has performance issues. Use GetByIdAsync with proper ID type instead.", error: false)]
        Task<T?> GetByStringIdAsync(string id, CancellationToken cancellationToken = default);
        
        Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
        Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
        Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    }
}
