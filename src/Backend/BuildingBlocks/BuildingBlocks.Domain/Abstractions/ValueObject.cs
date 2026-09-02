namespace BuildingBlocks.Domain.Abstractions;

/// <summary>
/// Base class for value objects using structural equality.
/// </summary>
public abstract class ValueObject
{
    /// <summary>
    /// Gets the components that participate in equality.
    /// </summary>
    /// <returns>Sequence of equality components.</returns>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType())
        {
            return false;
        }

        var other = (ValueObject)obj;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(component => component?.GetHashCode() ?? 0)
            .Aggregate(0, HashCode.Combine);
    }

    /// <summary>
    /// Compares two value objects for equality.
    /// </summary>
    /// <param name="left">First value object.</param>
    /// <param name="right">Second value object.</param>
    /// <returns>True when both are equal.</returns>
    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Compares two value objects for inequality.
    /// </summary>
    /// <param name="left">First value object.</param>
    /// <param name="right">Second value object.</param>
    /// <returns>True when both are different.</returns>
    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !Equals(left, right);
    }
}