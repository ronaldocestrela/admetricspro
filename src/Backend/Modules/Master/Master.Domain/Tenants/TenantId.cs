using BuildingBlocks.Domain.Abstractions;

namespace Master.Domain.Tenants;

/// <summary>
/// Strongly typed identifier for tenant aggregates.
/// </summary>
public sealed class TenantId : ValueObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantId"/> class.
    /// </summary>
    /// <param name="value">Tenant unique identifier.</param>
    public TenantId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("TenantId cannot be empty.", nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// Gets the raw GUID value.
    /// </summary>
    public Guid Value { get; }

    /// <summary>
    /// Creates a new tenant identifier.
    /// </summary>
    /// <returns>A new tenant identifier.</returns>
    public static TenantId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc />
    public override string ToString() => Value.ToString("D");
}