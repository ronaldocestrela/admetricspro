namespace Master.Application.Plans.DTOs;

/// <summary>
/// Data transfer object representing a subscription plan and its operational parameters.
/// </summary>
/// <param name="Id">Unique identifier of the plan.</param>
/// <param name="Name">Commercial display name.</param>
/// <param name="Description">Detailed description.</param>
/// <param name="Tier">Tier classification string.</param>
/// <param name="MonthlyPrice">Monthly price in BRL.</param>
/// <param name="AnnualDiscountPercentage">Annual discount percentage.</param>
/// <param name="MaxSeats">Maximum allowed seats.</param>
/// <param name="MaxWorkspaces">Maximum allowed client workspaces.</param>
/// <param name="MonthlyAdSpendCap">Monthly ad spend cap.</param>
/// <param name="HasWhiteLabel">Whether white-label customization is enabled.</param>
/// <param name="HasCustomCname">Whether custom CNAME is enabled.</param>
/// <param name="HasAiCopilot">Whether AI Copilot is enabled.</param>
/// <param name="HasCrossNetworkAutomations">Whether cross-network automations are enabled.</param>
/// <param name="IsActive">Whether the plan is active for new subscriptions.</param>
/// <param name="CreatedAtUtc">Creation timestamp in UTC.</param>
/// <param name="UpdatedAtUtc">Last update timestamp in UTC, if any.</param>
public sealed record PlanDto(
    Guid Id,
    string Name,
    string Description,
    string Tier,
    decimal MonthlyPrice,
    int AnnualDiscountPercentage,
    int MaxSeats,
    int MaxWorkspaces,
    decimal MonthlyAdSpendCap,
    bool HasWhiteLabel,
    bool HasCustomCname,
    bool HasAiCopilot,
    bool HasCrossNetworkAutomations,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
