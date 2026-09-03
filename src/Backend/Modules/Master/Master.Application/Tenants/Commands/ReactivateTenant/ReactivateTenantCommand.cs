using BuildingBlocks.Application.Messaging;
using Master.Domain.Tenants;

namespace Master.Application.Tenants.Commands.ReactivateTenant;

/// <summary>
/// Command to reactivate a suspended tenant back to active status.
/// </summary>
/// <param name="TenantId">The unique tenant identifier.</param>
public sealed record ReactivateTenantCommand(
    TenantId TenantId) : ICommand;
