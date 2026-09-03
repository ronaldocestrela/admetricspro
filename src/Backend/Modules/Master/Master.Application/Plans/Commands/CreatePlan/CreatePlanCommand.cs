using BuildingBlocks.Application.Messaging;
using Master.Domain.Plans;
using Master.Domain.Tenants;

namespace Master.Application.Plans.Commands.CreatePlan;

/// <summary>
/// Command to create a new subscription plan with defined tier quotas and pricing.
/// </summary>
/// <param name="Name">Commercial name of the plan.</param>
/// <param name="Description">Detailed description.</param>
/// <param name="Tier">Tier classification level.</param>
/// <param name="MonthlyPrice">Monthly price in BRL.</param>
/// <param name="AnnualDiscountPercentage">Annual discount percentage (0 to 100).</param>
/// <param name="MaxSeats">Maximum allowed seats.</param>
/// <param name="MaxWorkspaces">Maximum allowed client workspaces.</param>
/// <param name="MonthlyAdSpendCap">Monthly ad spend cap.</param>
/// <param name="HasWhiteLabel">Whether white-label customization is enabled.</param>
/// <param name="HasCustomCname">Whether custom CNAME is enabled.</param>
/// <param name="HasAiCopilot">Whether AI Copilot is enabled.</param>
/// <param name="HasCrossNetworkAutomations">Whether cross-network automations are enabled.</param>
public sealed record CreatePlanCommand(
    string Name,
    string Description,
    SubscriptionTier Tier,
    decimal MonthlyPrice,
    int AnnualDiscountPercentage,
    int MaxSeats,
    int MaxWorkspaces,
    decimal MonthlyAdSpendCap,
    bool HasWhiteLabel,
    bool HasCustomCname,
    bool HasAiCopilot,
    bool HasCrossNetworkAutomations) : ICommand<PlanId>;
