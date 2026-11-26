using BuildingBlocks.Domain.Primitives;

namespace Tenants.Application.Branding;

/// <summary>
/// Porta de armazenamento de logos e favicon no bucket de branding.
/// </summary>
public interface ITenantBrandAssetStore
{
    /// <summary>
    /// Monta a chave canônica do slot para o tenant atual.
    /// </summary>
    /// <param name="slot">Slot do ativo.</param>
    /// <returns>Chave relativa no bucket.</returns>
    string BuildObjectKey(TenantBrandAssetSlot slot);

    /// <summary>
    /// Grava ou substitui o conteúdo do slot.
    /// </summary>
    /// <param name="slot">Slot do ativo.</param>
    /// <param name="content">Fluxo de leitura.</param>
    /// <param name="contentType">MIME declarado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Chave gravada ou falha de storage.</returns>
    Task<Result<string>> SaveAsync(
        TenantBrandAssetSlot slot,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Abre leitura pela chave persistida.
    /// </summary>
    /// <param name="objectKey">Chave no bucket.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Conteúdo e tipo MIME ou falha.</returns>
    Task<Result<TenantBrandAssetReadResult>> OpenReadAsync(
        string objectKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove o objeto se existir.
    /// </summary>
    /// <param name="objectKey">Chave no bucket.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Sucesso ou falha de storage.</returns>
    Task<Result> DeleteAsync(string objectKey, CancellationToken cancellationToken = default);
}