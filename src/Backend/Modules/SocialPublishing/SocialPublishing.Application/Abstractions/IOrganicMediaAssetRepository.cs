using SocialPublishing.Domain.Posts;

namespace SocialPublishing.Application.Abstractions;

/// <summary>
/// Repositório de ativos de mídia orgânica standalone no banco do tenant.
/// </summary>
public interface IOrganicMediaAssetRepository
{
    /// <summary>
    /// Obtém um ativo pelo identificador.
    /// </summary>
    /// <param name="id">Identificador do ativo.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O ativo ou <c>null</c>.</returns>
    Task<OrganicMediaAsset?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona um ativo standalone ao contexto.
    /// </summary>
    /// <param name="asset">Entidade validada.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task AddAsync(OrganicMediaAsset asset, CancellationToken cancellationToken = default);
}
