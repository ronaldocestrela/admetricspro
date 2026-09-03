using BuildingBlocks.Application.Messaging;
using Master.Domain.Tenants;

namespace Master.Application.Tenants.Commands.ImpersonateTenant;

/// <summary>
/// Command to issue an impersonation session and contextual token to access a tenant in Shadow Mode.
/// </summary>
/// <param name="TenantId">Target tenant identifier.</param>
/// <param name="SuperAdminId">Unique identifier of the SuperAdmin or support engineer requesting access.</param>
/// <param name="SupportTicketId">Mandatory reference to the customer support incident ticket.</param>
/// <param name="Reason">Detailed justification for accessing customer environment.</param>
/// <param name="DurationMinutes">Requested session validity duration in minutes (between 5 and 120).</param>
public sealed record ImpersonateTenantCommand(
    TenantId TenantId,
    Guid SuperAdminId,
    string SupportTicketId,
    string Reason,
    int DurationMinutes = 30) : ICommand<ImpersonateTenantResponse>;
