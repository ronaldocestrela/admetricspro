using BuildingBlocks.Application.Messaging;

namespace Master.Application.Plans.Commands.UpdatePlan;

/// <summary>
/// Command to update an existing subscription plan's commercial details, quotas, and features.
/// </summary>
/// <param name="PlanId">Identifier of the plan to update.</param>
/// <param name="Name">Updated commercial name.</param>
/// <param name="Description">Updated description.</param>
/// <param name="MonthlyPrice">Updated monthly price.</param>
/// <param name="AnnualDiscountPercentage">Updated annual discount percentage.</param>
/// <param name="MaxSeats">Updated seat limit.</param>
/// <param name="MaxWorkspaces">Updated workspace limit.</param>
/// <param name="MonthlyAdSpendCap">Updated ad spend cap.</param>
/// <param name="HasWhiteLabel">Updated white-label flag.</param>
/// <param name="HasCustomCname">Updated custom CNAME flag.</param>
/// <param name="HasAiCopilot">Updated AI Copilot flag.</param>
/// <param name="HasCrossNetworkAutomations">Updated cross-network automations flag.</param>
public sealed record UpdatePlanCommand(
    Guid PlanId,
    string Name,
    string Description,
    decimal MonthlyPrice,
    int AnnualDiscountPercentage,
    int MaxSeats,
    int MaxWorkspaces,
    decimal MonthlyAdSpendCap,
    bool HasWhiteLabel,
    bool HasCustomCname,
    bool HasAiCopilot,
    bool HasCrossNetworkAutomations) : ICommand;
