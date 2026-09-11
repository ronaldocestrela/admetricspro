using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Tenants;

namespace Tenants.Application.Users.Commands.ChangeTenantUserRole;

/// <summary>
/// Comando para alteração do papel/função de um colaborador no inquilino com rastreabilidade de auditoria.
/// </summary>
/// <param name="UserId">Identificador do colaborador alvo.</param>
/// <param name="NewRole">Novo papel a ser atribuído.</param>
/// <param name="OperatorUserId">Identificador do usuário que está executando a alteração.</param>
/// <param name="OperatorUserEmail">Email do operador que está executando a alteração.</param>
/// <param name="IpAddress">Endereço IP de origem da requisição.</param>
public sealed record ChangeTenantUserRoleCommand(
    Guid UserId,
    TenantRole NewRole,
    Guid OperatorUserId,
    string OperatorUserEmail,
    string? IpAddress = null) : ICommand;
