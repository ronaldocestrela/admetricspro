using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;

namespace Tenants.Application.Auth.Services;

/// <summary>
/// Contrato do serviço de geração e assinatura de tokens de acesso JWT para usuários de inquilinos.
/// </summary>
public interface ITenantTokenService
{
    /// <summary>
    /// Gera um token JWT assinado digitalmente contendo as claims da identidade do usuário e contexto do inquilino.
    /// </summary>
    /// <param name="user">Entidade de usuário do inquilino autenticado.</param>
    /// <param name="tenantId">Identificador único do inquilino.</param>
    /// <param name="subdomain">Subdomínio exclusivo do inquilino.</param>
    /// <returns>Resultado contendo a string codificada do token JWT assinado ou falha de configuração.</returns>
    Result<string> GenerateToken(TenantUser user, Guid tenantId, string subdomain);
}
