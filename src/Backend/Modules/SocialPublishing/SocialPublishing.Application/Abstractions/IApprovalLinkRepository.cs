using SocialPublishing.Domain.Approvals;

namespace SocialPublishing.Application.Abstractions;

/// <summary>
/// Repositório de links compartilháveis de aprovação orgânica no banco do tenant.
/// </summary>
public interface IApprovalLinkRepository
{
    /// <summary>
    /// Obtém um link pelo hash SHA-256 do token.
    /// </summary>
    /// <param name="tokenHash">Hash hexadecimal de 64 caracteres.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O link ou <c>null</c>.</returns>
    Task<ApprovalLink?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona um novo link ao contexto.
    /// </summary>
    /// <param name="link">Agregado a persistir.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task AddAsync(ApprovalLink link, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marca como modificados links revogados ou atualizados.
    /// </summary>
    /// <param name="link">Agregado modificado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task UpdateAsync(ApprovalLink link, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoga todos os links ainda ativos (não revogados) de um workspace.
    /// </summary>
    /// <param name="workspaceId">Workspace proprietário.</param>
    /// <param name="revokedAtUtc">Instante UTC da revogação.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Quantidade de links revogados.</returns>
    Task<int> RevokeActiveByWorkspaceAsync(
        Guid workspaceId,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken = default);
}
