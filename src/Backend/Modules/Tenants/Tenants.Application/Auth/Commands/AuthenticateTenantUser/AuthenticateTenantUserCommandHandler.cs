using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Tenants.Application.Auth.DTOs;
using Tenants.Application.Auth.Services;

namespace Tenants.Application.Auth.Commands.AuthenticateTenantUser;

/// <summary>
/// Manipulador MediatR para o comando de autenticação de usuários de inquilino.
/// </summary>
public sealed class AuthenticateTenantUserCommandHandler : ICommandHandler<AuthenticateTenantUserCommand, AuthenticatedTenantUserDto>
{
    private readonly ITenantAuthService _tenantAuthService;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="AuthenticateTenantUserCommandHandler"/>.
    /// </summary>
    /// <param name="tenantAuthService">Instância do serviço de segurança de inquilinos.</param>
    public AuthenticateTenantUserCommandHandler(ITenantAuthService tenantAuthService)
    {
        _tenantAuthService = tenantAuthService ?? throw new ArgumentNullException(nameof(tenantAuthService));
    }

    /// <summary>
    /// Processa o fluxo de autenticação do usuário do inquilino.
    /// </summary>
    /// <param name="command">Comando contendo credenciais e dados contextuais do inquilino.</param>
    /// <param name="cancellationToken">Token de cancelamento assíncrono.</param>
    /// <returns>Resultado com o DTO do usuário autenticado ou falha tipada.</returns>
    public async Task<Result<AuthenticatedTenantUserDto>> Handle(
        AuthenticateTenantUserCommand command,
        CancellationToken cancellationToken)
    {
        return await _tenantAuthService.AuthenticateAsync(
            command.Email,
            command.Password,
            command.Subdomain,
            command.TenantId,
            command.IpAddress,
            cancellationToken);
    }
}
