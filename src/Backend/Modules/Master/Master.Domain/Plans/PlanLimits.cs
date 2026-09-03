using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;

namespace Master.Domain.Plans;

/// <summary>
/// Value object defining usage quotas and structural limits of a subscription plan.
/// </summary>
public sealed class PlanLimits : ValueObject
{
    private PlanLimits(int maxSeats, int maxWorkspaces, decimal monthlyAdSpendCap)
    {
        MaxSeats = maxSeats;
        MaxWorkspaces = maxWorkspaces;
        MonthlyAdSpendCap = monthlyAdSpendCap;
    }

    /// <summary>
    /// Gets the maximum allowed user seats.
    /// </summary>
    public int MaxSeats { get; }

    /// <summary>
    /// Gets the maximum allowed client workspaces.
    /// </summary>
    public int MaxWorkspaces { get; }

    /// <summary>
    /// Gets the monthly managed ad spend cap in currency units.
    /// </summary>
    public decimal MonthlyAdSpendCap { get; }

    /// <summary>
    /// Creates a new instance of <see cref="PlanLimits"/> with validation rules.
    /// </summary>
    /// <param name="maxSeats">Maximum allowed seats (must be greater than zero).</param>
    /// <param name="maxWorkspaces">Maximum allowed workspaces (must be greater than zero).</param>
    /// <param name="monthlyAdSpendCap">Monthly ad spend cap (must be non-negative).</param>
    /// <returns>A successful result containing the limits or a validation failure.</returns>
    public static Result<PlanLimits> Create(int maxSeats, int maxWorkspaces, decimal monthlyAdSpendCap)
    {
        if (maxSeats <= 0)
        {
            return Result<PlanLimits>.Failure(Error.Validation("Plan.InvalidSeats", "O limite de assentos deve ser maior que zero."));
        }

        if (maxWorkspaces <= 0)
        {
            return Result<PlanLimits>.Failure(Error.Validation("Plan.InvalidWorkspaces", "O limite de workspaces deve ser maior que zero."));
        }

        if (monthlyAdSpendCap < 0)
        {
            return Result<PlanLimits>.Failure(Error.Validation("Plan.InvalidAdSpendCap", "O teto de ad spend mensal não pode ser negativo."));
        }

        return Result<PlanLimits>.Success(new PlanLimits(maxSeats, maxWorkspaces, monthlyAdSpendCap));
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return MaxSeats;
        yield return MaxWorkspaces;
        yield return MonthlyAdSpendCap;
    }
}
