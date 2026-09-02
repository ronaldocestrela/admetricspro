using BuildingBlocks.Domain.Abstractions;

namespace BuildingBlocks.Application.Persistence;

/// <summary>
/// Generic repository contract for aggregate persistence operations.
/// </summary>
/// <typeparam name="TEntity">Aggregate root type.</typeparam>
/// <typeparam name="TId">Identifier type.</typeparam>
public interface IRepository<TEntity, in TId>
    where TEntity : AggregateRoot<TId>
    where TId : notnull
{
    /// <summary>
    /// Adds an aggregate to the persistence context.
    /// </summary>
    /// <param name="entity">Aggregate to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An awaitable task.</returns>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches an aggregate by its identifier.
    /// </summary>
    /// <param name="id">Aggregate identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The aggregate if found; otherwise null.</returns>
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an aggregate as modified in the persistence context.
    /// </summary>
    /// <param name="entity">Aggregate to update.</param>
    void Update(TEntity entity);

    /// <summary>
    /// Marks an aggregate for removal from the persistence context.
    /// </summary>
    /// <param name="entity">Aggregate to remove.</param>
    void Remove(TEntity entity);
}