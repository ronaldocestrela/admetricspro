using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;
using Master.Domain.Plans.Events;
using Master.Domain.Tenants;

namespace Master.Domain.Plans;

/// <summary>
/// Master catalog aggregate representing a commercial subscription plan and its tier quotas.
/// </summary>
public sealed class SubscriptionPlan : AggregateRoot<PlanId>
{
    private SubscriptionPlan(
        PlanId id,
        string name,
        string description,
        SubscriptionTier tier,
        decimal monthlyPrice,
        int annualDiscountPercentage,
        PlanLimits limits,
        PlanFeatures features)
        : base(id)
    {
        Name = name;
        Description = description;
        Tier = tier;
        MonthlyPrice = monthlyPrice;
        AnnualDiscountPercentage = annualDiscountPercentage;
        Limits = limits;
        Features = features;
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    private SubscriptionPlan()
        : base(new PlanId(Guid.NewGuid()))
    {
        Name = string.Empty;
        Description = string.Empty;
        Tier = SubscriptionTier.Trial;
        MonthlyPrice = 0m;
        AnnualDiscountPercentage = 0;
        Limits = PlanLimits.Create(1, 1, 0m).Value;
        Features = PlanFeatures.Default();
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the commercial display name of the plan.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the detailed description of target audience and included value.
    /// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// Gets the tier classification level for this plan.
    /// </summary>
    public SubscriptionTier Tier { get; private set; }

    /// <summary>
    /// Gets the recurring monthly price in BRL.
    /// </summary>
    public decimal MonthlyPrice { get; private set; }

    /// <summary>
    /// Gets the discount percentage applied for annual billing commitments (0 to 100).
    /// </summary>
    public int AnnualDiscountPercentage { get; private set; }

    /// <summary>
    /// Gets the structural limits and quotas (seats, workspaces, ad spend cap).
    /// </summary>
    public PlanLimits Limits { get; private set; }

    /// <summary>
    /// Gets the functional feature flags unlocked for this plan.
    /// </summary>
    public PlanFeatures Features { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this plan is currently active for new tenant subscriptions.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Gets the UTC creation date of the plan.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the UTC last modification date of the plan, if any.
    /// </summary>
    public DateTime? UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Factory method to create a new <see cref="SubscriptionPlan"/> with validation rules.
    /// </summary>
    /// <param name="name">Plan commercial name.</param>
    /// <param name="description">Plan description.</param>
    /// <param name="tier">Tier level.</param>
    /// <param name="monthlyPrice">Monthly price.</param>
    /// <param name="annualDiscountPercentage">Annual discount percentage (0 to 100).</param>
    /// <param name="limits">Structural limits.</param>
    /// <param name="features">Functional features.</param>
    /// <returns>A result containing the created plan or validation errors.</returns>
    public static Result<SubscriptionPlan> Create(
        string name,
        string description,
        SubscriptionTier tier,
        decimal monthlyPrice,
        int annualDiscountPercentage,
        PlanLimits limits,
        PlanFeatures features)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<SubscriptionPlan>.Failure(Error.Validation("Plan.NameRequired", "O nome do plano é obrigatório."));
        }

        if (monthlyPrice < 0)
        {
            return Result<SubscriptionPlan>.Failure(Error.Validation("Plan.InvalidMonthlyPrice", "O preço mensal não pode ser negativo."));
        }

        if (annualDiscountPercentage < 0 || annualDiscountPercentage > 100)
        {
            return Result<SubscriptionPlan>.Failure(Error.Validation("Plan.InvalidAnnualDiscount", "O desconto anual deve estar entre 0 e 100%."));
        }

        ArgumentNullException.ThrowIfNull(limits);
        ArgumentNullException.ThrowIfNull(features);

        var planId = PlanId.New();
        var plan = new SubscriptionPlan(
            planId,
            name.Trim(),
            description?.Trim() ?? string.Empty,
            tier,
            monthlyPrice,
            annualDiscountPercentage,
            limits,
            features);

        plan.RaiseDomainEvent(new PlanCreatedDomainEvent(plan.Id, plan.Name, plan.Tier));

        return Result<SubscriptionPlan>.Success(plan);
    }

    /// <summary>
    /// Updates the plan commercial details and pricing.
    /// </summary>
    /// <param name="name">New plan name.</param>
    /// <param name="description">New plan description.</param>
    /// <param name="monthlyPrice">New monthly price.</param>
    /// <param name="annualDiscountPercentage">New annual discount percentage.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result UpdateDetails(string name, string description, decimal monthlyPrice, int annualDiscountPercentage)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(Error.Validation("Plan.NameRequired", "O nome do plano é obrigatório."));
        }

        if (monthlyPrice < 0)
        {
            return Result.Failure(Error.Validation("Plan.InvalidMonthlyPrice", "O preço mensal não pode ser negativo."));
        }

        if (annualDiscountPercentage < 0 || annualDiscountPercentage > 100)
        {
            return Result.Failure(Error.Validation("Plan.InvalidAnnualDiscount", "O desconto anual deve estar entre 0 e 100%."));
        }

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        MonthlyPrice = monthlyPrice;
        AnnualDiscountPercentage = annualDiscountPercentage;
        UpdatedAtUtc = DateTime.UtcNow;

        RaiseDomainEvent(new PlanUpdatedDomainEvent(Id, Name, MonthlyPrice));

        return Result.Success();
    }

    /// <summary>
    /// Updates the structural quotas and limits of the plan.
    /// </summary>
    /// <param name="limits">New plan limits.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result UpdateLimits(PlanLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);

        Limits = limits;
        UpdatedAtUtc = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Updates the functional feature flags of the plan.
    /// </summary>
    /// <param name="features">New plan features.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result UpdateFeatures(PlanFeatures features)
    {
        ArgumentNullException.ThrowIfNull(features);

        Features = features;
        UpdatedAtUtc = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Deactivates the plan, preventing new subscriptions.
    /// </summary>
    /// <returns>A result indicating success or failure.</returns>
    public Result Deactivate()
    {
        if (!IsActive)
        {
            return Result.Failure(Error.Conflict("Plan.AlreadyInactive", "O plano já se encontra inativo."));
        }

        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Reactivates the plan, allowing new subscriptions.
    /// </summary>
    /// <returns>A result indicating success or failure.</returns>
    public Result Reactivate()
    {
        if (IsActive)
        {
            return Result.Failure(Error.Conflict("Plan.AlreadyActive", "O plano já se encontra ativo."));
        }

        IsActive = true;
        UpdatedAtUtc = DateTime.UtcNow;

        return Result.Success();
    }
}
