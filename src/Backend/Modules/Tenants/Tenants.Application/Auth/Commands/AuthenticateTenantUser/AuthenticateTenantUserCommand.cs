using BuildingBlocks.Application.Messaging;
using Tenants.Application.Auth.DTOs;

namespace Tenants.Application.Auth.Commands.AuthenticateTenantUser;

/// <summary>
/// Comando de autenticação de usuários e colaboradores de agências (tenants operacionais).
/// Suporta identificação contextual por subdomínio ou identificador de inquilino.
/// </summary>
/// <param name="Email">E-mail corporativo do usuário cadastrado na base operacional do tenant.</param>
/// <param name="Password">Senha em texto plano a ser validada contra o hash seguro.</param>
/// <param name="Subdomain">Subdomínio opcional caso não tenha sido inferido pelo host/header HTTP.</param>
/// <param name="TenantId">Identificador único do inquilino caso informado diretamente.</param>
/// <param name="IpAddress">Endereço IP de origem da tentativa de autenticação para trilha de auditoria.</param>
public sealed record AuthenticateTenantUserCommand(
    string Email,
    string Password,
    string? Subdomain = null,
    Guid? TenantId = null,
    string? IpAddress = null) : ICommand<AuthenticatedTenantUserDto>;
