using BuildingBlocks.Domain.Abstractions;

namespace Master.Domain.Plans;

/// <summary>
/// Strongly typed identifier for subscription plan aggregates.
/// </summary>
public sealed class PlanId : ValueObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlanId"/> class.
    /// </summary>
    /// <param name="value">Plan unique identifier.</param>
    public PlanId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("PlanId cannot be empty.", nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// Gets the raw GUID value.
    /// </summary>
    public Guid Value { get; }

    /// <summary>
    /// Creates a new subscription plan identifier.
    /// </summary>
    /// <returns>A new subscription plan identifier.</returns>
    public static PlanId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc />
    public override string ToString() => Value.ToString("D");
}
