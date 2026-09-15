using BuildingBlocks.Domain.Primitives;
using Tenants.Application.Branding.DTOs;
using WebApp.Models;

namespace WebApp.Services;

/// <summary>
/// Contrato do cliente HTTP para gestão de identidade visual e personalização White-Label do inquilino.
/// </summary>
public interface ITenantBrandingClientService
{
    /// <summary>
    /// Obtém as configurações de identidade visual White-Label do inquilino atual.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Dados de branding ou falha semântica.</returns>
    Task<Result<TenantBrandingDetailsDto>> GetBrandingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza as configurações de marca (cores, logotipos claro/escuro e favicon) do inquilino.
    /// </summary>
    /// <param name="model">Modelo com a paleta de cores e URLs dos ativos.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Dados de branding atualizados ou falha.</returns>
    Task<Result<TenantBrandingDetailsDto>> UpdateBrandingAsync(
        UpdateTenantBrandingModel model,
        CancellationToken cancellationToken = default);
}
