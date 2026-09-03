using BuildingBlocks.Application.Messaging;
using Master.Domain.Tenants;

namespace Master.Application.Tenants.Commands.SuspendTenant;

/// <summary>
/// Command to suspend operations of a tenant.
/// </summary>
/// <param name="TenantId">The unique tenant identifier.</param>
/// <param name="Reason">The justification for suspension.</param>
public sealed record SuspendTenantCommand(
    TenantId TenantId,
    string Reason) : ICommand;
