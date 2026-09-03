using BuildingBlocks.Application.Messaging;

namespace Master.Application.Billing.Dunning;

/// <summary>
/// Command to manually trigger an immediate dunning evaluation cycle against overdue tenants.
/// </summary>
/// <param name="ReferenceDateUtc">Optional explicit reference timestamp for the evaluation.</param>
public sealed record ExecuteDunningCycleCommand(DateTime? ReferenceDateUtc = null) : ICommand<DunningExecutionSummary>;
