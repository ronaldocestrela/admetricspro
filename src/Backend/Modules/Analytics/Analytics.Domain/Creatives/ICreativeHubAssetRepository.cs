using BuildingBlocks.Domain.Primitives;

namespace Analytics.Domain.Creatives;

/// <summary>
/// Persistência de ativos do Creative Hub no banco do tenant (schema analytics).
/// </summary>
public interface ICreativeHubAssetRepository
{
    /// <summary>
    /// Insere ou atualiza um ativo identificado pela fingerprint no workspace.
    /// </summary>
    /// <param name="request">Dados da detecção orgânica.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Identificador do ativo persistido.</returns>
    Task<Result<Guid>> UpsertByFingerprintAsync(
        CreativeHubAssetUpsertRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista ativos com origem orgânica (post social) no workspace.
    /// </summary>
    /// <param name="workspaceId">Workspace proprietário.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Ativos orgânicos persistidos ou falha de acesso.</returns>
    Task<Result<IReadOnlyList<CreativeHubAsset>>> ListOrganicByWorkspaceAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default);
}
