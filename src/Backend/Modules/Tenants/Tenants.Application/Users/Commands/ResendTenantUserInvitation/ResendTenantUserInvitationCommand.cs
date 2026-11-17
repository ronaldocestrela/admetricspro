using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Tenants;
using Tenants.Application.Rbac.Attributes;
using Tenants.Application.Users.DTOs;

namespace Tenants.Application.Users.Commands.ResendTenantUserInvitation;

/// <summary>
/// Comando para reenviar o convite de um colaborador pendente.
/// </summary>
/// <param name="UserId">Identificador do colaborador.</param>
[RequireTenantPermission(TenantPermission.ManageTeamMembers)]
public sealed record ResendTenantUserInvitationCommand(Guid UserId) : ICommand<InviteTenantUserResultDto>;