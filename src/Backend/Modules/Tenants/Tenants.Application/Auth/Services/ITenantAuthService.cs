using BuildingBlocks.Domain.Primitives;
using Tenants.Application.Auth.DTOs;

namespace Tenants.Application.Auth.Services;

/// <summary>
/// Contrato do serviço de autenticação e validação de credenciais de usuários operacionais de agências (tenants).
/// </summary>
public interface ITenantAuthService
{
    /// <summary>
    /// Autentica um usuário de inquilino validando contexto, permissões, status da conta e credenciais criptográficas.
    /// </summary>
    /// <param name="email">E-mail de acesso informado pelo usuário.</param>
    /// <param name="password">Senha em texto plano a ser verificada.</param>
    /// <param name="subdomain">Subdomínio opcional caso o host HTTP não tenha resolvido o tenant.</param>
    /// <param name="tenantId">Identificador opcional de tenant caso fornecido explicitamente.</param>
    /// <param name="ipAddress">Endereço IP da requisição para fins de auditoria de segurança.</param>
    /// <param name="cancellationToken">Token de cancelamento assíncrono.</param>
    /// <returns>Resultado com dados da sessão e token JWT, ou erro de negócio tipado.</returns>
    Task<Result<AuthenticatedTenantUserDto>> AuthenticateAsync(
        string email,
        string password,
        string? subdomain = null,
        Guid? tenantId = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);
}
