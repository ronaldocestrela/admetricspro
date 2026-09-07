using BuildingBlocks.Domain.Primitives;
using Tenants.Application.Auth.DTOs;
using WebApp.Models;

namespace WebApp.Services;

/// <summary>
/// Contrato do cliente HTTP tipado responsável pela autenticação e resolução de branding de inquilinos na Web API.
/// </summary>
public interface ITenantAuthClientService
{
    /// <summary>
    /// Consulta os metadados públicos de identificação e branding de um inquilino pelo seu subdomínio.
    /// </summary>
    /// <param name="subdomain">Subdomínio da agência (ex: "vanguarda").</param>
    /// <param name="cancellationToken">Token de cancelamento assíncrono.</param>
    /// <returns>Resultado contendo os dados de marca do inquilino ou falha tipada.</returns>
    Task<Result<TenantPublicBrandingViewModel>> GetPublicBrandingAsync(
        string subdomain,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Envia as credenciais de login para autenticação na Web API e emissão de token de sessão.
    /// </summary>
    /// <param name="model">Modelo com credenciais de e-mail, senha e subdomínio.</param>
    /// <param name="cancellationToken">Token de cancelamento assíncrono.</param>
    /// <returns>Resultado contendo as credenciais autenticadas e token JWT ou falha tipada.</returns>
    Task<Result<AuthenticatedTenantUserDto>> LoginAsync(
        TenantLoginModel model,
        CancellationToken cancellationToken = default);
}
