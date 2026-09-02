namespace BuildingBlocks.Domain.Abstractions;

/// <summary>
/// Base class for domain entities identified by a typed identifier.
/// </summary>
/// <typeparam name="TId">Identifier type.</typeparam>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
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

    /// <inheritdoc />
    public bool Equals(Entity<TId>? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (GetType() != other.GetType())
        {
            return false;
        }

        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is not Entity<TId> other)
        {
            return false;
        }

        return Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(GetType(), Id);
    }

    /// <summary>
    /// Compares two entity instances for equality.
    /// </summary>
    /// <param name="left">The left entity instance.</param>
    /// <param name="right">The right entity instance.</param>
    /// <returns><c>true</c> if both entities are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Compares two entity instances for inequality.
    /// </summary>
    /// <param name="left">The left entity instance.</param>
    /// <param name="right">The right entity instance.</param>
    /// <returns><c>true</c> if entities are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !Equals(left, right);
    }
}