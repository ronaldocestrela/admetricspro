using BuildingBlocks.Domain.Primitives;

namespace SocialPublishing.Application.Abstractions;

/// <summary>
/// Armazenamento da logo do cliente associada ao Brand Voice por workspace.
/// </summary>
public interface IBrandLogoStore
{
    /// <summary>
    /// Monta a chave canônica da logo do workspace.
    /// </summary>
    /// <param name="tenantId">Tenant contextual.</param>
    /// <param name="workspaceId">Workspace do cliente.</param>
    /// <returns>Chave relativa no bucket orgânico.</returns>
    string BuildObjectKey(Guid tenantId, Guid workspaceId);

    /// <summary>
    /// Grava ou substitui o objeto da logo.
    /// </summary>
    /// <param name="tenantId">Tenant contextual.</param>
    /// <param name="workspaceId">Workspace do cliente.</param>
    /// <param name="content">Conteúdo da imagem.</param>
    /// <param name="contentType">MIME permitido (PNG, JPEG ou WebP).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Chave persistida ou falha de armazenamento.</returns>
    Task<Result<string>> SaveAsync(
        Guid tenantId,
        Guid workspaceId,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Abre leitura do objeto da logo.
    /// </summary>
    /// <param name="objectKey">Chave persistida.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Stream de leitura ou falha.</returns>
    Task<Result<Stream>> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove o objeto da logo.
    /// </summary>
    /// <param name="objectKey">Chave persistida.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    Task<Result> DeleteAsync(string objectKey, CancellationToken cancellationToken = default);
}