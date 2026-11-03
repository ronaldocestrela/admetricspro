using BuildingBlocks.Domain.Primitives;

namespace SocialPublishing.Application.Abstractions;

/// <summary>
/// Porta de armazenamento de binário de mídia orgânica (sem persistência SQL do conteúdo).
/// </summary>
public interface IOrganicMediaStore
{
    /// <summary>
    /// Persiste o conteúdo e devolve a chave do objeto.
    /// </summary>
    /// <param name="tenantId">Inquilino atual.</param>
    /// <param name="workspaceId">Workspace proprietário.</param>
    /// <param name="assetId">Identificador do ativo.</param>
    /// <param name="content">Fluxo de leitura do binário.</param>
    /// <param name="contentType">Tipo MIME.</param>
    /// <param name="tier">Área temporária ou definitiva.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Chave do objeto ou falha técnica.</returns>
    Task<Result<string>> SaveAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid assetId,
        Stream content,
        string contentType,
        OrganicMediaStorageTier tier,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Abre leitura do objeto pela chave.
    /// </summary>
    /// <param name="objectKey">Chave retornada por <see cref="SaveAsync"/>.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Fluxo legível ou falha.</returns>
    Task<Result<Stream>> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove o objeto armazenado.
    /// </summary>
    /// <param name="objectKey">Chave do objeto.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    Task<Result> DeleteAsync(string objectKey, CancellationToken cancellationToken = default);
}

/// <summary>
/// Tier de armazenamento para uploads orgânicos.
/// </summary>
public enum OrganicMediaStorageTier
{
    /// <summary>
    /// Área temporária antes da validação de domínio.
    /// </summary>
    Staging = 0,

    /// <summary>
    /// Armazenamento definitivo após validação.
    /// </summary>
    Durable = 1,
}
