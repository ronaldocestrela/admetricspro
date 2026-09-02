namespace BuildingBlocks.Application.Persistence;

/// <summary>
/// Generic repository contract for aggregate persistence operations.
/// </summary>
/// <typeparam name="TEntity">Aggregate/entity type.</typeparam>
/// <typeparam name="TId">Identifier type.</typeparam>
public interface IRepository<TEntity, in TId>
    where TEntity : class
    where TId : notnull
{
    /// <summary>
    /// Adds an entity to the persistence context.
    /// </summary>
    /// <param name="entity">Entity to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An awaitable task.</returns>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken);

    /// <summary>
    /// Fetches an entity by identifier.
    /// </summary>
    /// <param name="id">Entity identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The entity if found; otherwise null.</returns>
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken);
}