using BuildingBlocks.Domain.Tenants;

namespace Tenants.Application.Branding.Repositories;

/// <summary>
/// Contrato de persistência para as configurações de identidade visual do inquilino no banco operacional dedicado.
/// </summary>
public interface ITenantBrandingRepository
{
    /// <summary>
    /// Obtém o registro de branding único configurado para o inquilino atual.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A entidade <see cref="TenantBranding"/> se configurada; caso contrário, null.</returns>
    Task<TenantBranding?> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona um novo registro de branding no contexto de persistência.
    /// </summary>
    /// <param name="branding">Instância a ser persistida.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task AddAsync(TenantBranding branding, CancellationToken cancellationToken = default);
}
