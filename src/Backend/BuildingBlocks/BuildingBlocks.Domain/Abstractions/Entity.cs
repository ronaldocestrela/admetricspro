namespace BuildingBlocks.Domain.Abstractions;

/// <summary>
/// Base class for domain entities identified by a typed identifier.
/// </summary>
/// <typeparam name="TId">Identifier type.</typeparam>
public abstract class Entity<TId>
    where TId : notnull
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Entity{TId}"/> class.
    /// </summary>
    /// <param name="id">Entity identifier.</param>
    protected Entity(TId id)
    {
        Id = id;
    }

    /// <summary>
    /// Gets the entity identifier.
    /// </summary>
    public TId Id { get; protected set; }
}