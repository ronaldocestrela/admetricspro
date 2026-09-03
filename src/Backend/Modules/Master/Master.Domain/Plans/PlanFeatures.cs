using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;

namespace Master.Domain.Plans;

/// <summary>
/// Value object defining functional capability flags enabled for a subscription plan.
/// </summary>
public sealed class PlanFeatures : ValueObject
{
    private PlanFeatures(
        bool hasWhiteLabel,
        bool hasCustomCname,
        bool hasAiCopilot,
        bool hasCrossNetworkAutomations)
    {
        HasWhiteLabel = hasWhiteLabel;
        HasCustomCname = hasCustomCname;
        HasAiCopilot = hasAiCopilot;
        HasCrossNetworkAutomations = hasCrossNetworkAutomations;
    }

    /// <summary>
    /// Gets a value indicating whether complete white-label customization is enabled.
    /// </summary>
    public bool HasWhiteLabel { get; }

    /// <summary>
    /// Gets a value indicating whether custom CNAME / custom subdomain mapping is enabled.
    /// </summary>
    public bool HasCustomCname { get; }

    /// <summary>
    /// Gets a value indicating whether AI Copilot campaign optimization is enabled.
    /// </summary>
    public bool HasAiCopilot { get; }

    /// <summary>
    /// Gets a value indicating whether cross-network automation rules and pacing are enabled.
    /// </summary>
    public bool HasCrossNetworkAutomations { get; }

    /// <summary>
    /// Creates a new instance of <see cref="PlanFeatures"/>.
    /// </summary>
    /// <param name="hasWhiteLabel">White-label flag.</param>
    /// <param name="hasCustomCname">Custom CNAME flag.</param>
    /// <param name="hasAiCopilot">AI Copilot flag.</param>
    /// <param name="hasCrossNetworkAutomations">Cross-network automations flag.</param>
    /// <returns>A successful result containing the plan features.</returns>
    public static Result<PlanFeatures> Create(
        bool hasWhiteLabel,
        bool hasCustomCname,
        bool hasAiCopilot,
        bool hasCrossNetworkAutomations)
    {
        return Result<PlanFeatures>.Success(new PlanFeatures(
            hasWhiteLabel,
            hasCustomCname,
            hasAiCopilot,
            hasCrossNetworkAutomations));
    }

    /// <summary>
    /// Returns default plan features with all advanced capabilities disabled.
    /// </summary>
    /// <returns>Default basic plan features.</returns>
    public static PlanFeatures Default() => new(false, false, false, false);

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return HasWhiteLabel;
        yield return HasCustomCname;
        yield return HasAiCopilot;
        yield return HasCrossNetworkAutomations;
    }
}
