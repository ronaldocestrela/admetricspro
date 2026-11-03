using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Application.Connections.Commands.CompleteOrganicAuthorization;
using SocialPublishing.Application.Connections.DTOs;
using SocialPublishing.Application.Connections.Queries.StartOrganicAuthorization;
using SocialPublishing.Domain.Connections;

namespace WebApp.Services.Social;

/// <summary>
/// Contrato do cliente HTTP fortemente tipado para publicação orgânica (conexões OAuth, calendário e copilot).
/// </summary>
public interface ISocialPublishingClient
{
    /// <summary>
    /// Lista conexões orgânicas de um workspace sem expor segredos.
    /// </summary>
    /// <param name="workspaceId">Workspace proprietário.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Metadados das conexões ou falha semântica.</returns>
    Task<Result<IReadOnlyList<OrganicConnectionSummaryDto>>> ListConnectionsAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inicia o fluxo OAuth orgânico e retorna a URL de consentimento da rede.
    /// </summary>
    /// <param name="workspaceId">Workspace que receberá a conexão.</param>
    /// <param name="platform">Rede orgânica alvo.</param>
    /// <param name="redirectUri">URI de retorno absoluta registrada no app da rede.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>URL de autorização e state anti-CSRF.</returns>
    Task<Result<StartOrganicAuthorizationResponse>> StartAuthorizationAsync(
        Guid workspaceId,
        OrganicPlatform platform,
        string redirectUri,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Conclui o callback OAuth orgânico após consentimento do usuário na rede.
    /// </summary>
    /// <param name="workspaceId">Workspace alvo da conexão.</param>
    /// <param name="code">Código de autorização.</param>
    /// <param name="state">State anti-CSRF emitido no authorize.</param>
    /// <param name="redirectUri">URI de retorno idêntica à usada no authorize.</param>
    /// <param name="providerError">Erro retornado pelo provedor, se houver.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Metadados da conexão persistida (sem tokens).</returns>
    Task<Result<CompleteOrganicAuthorizationResponse>> CompleteAuthorizationAsync(
        Guid workspaceId,
        string code,
        string state,
        string redirectUri,
        string? providerError = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoga localmente uma conexão orgânica do workspace.
    /// </summary>
    /// <param name="workspaceId">Workspace proprietário.</param>
    /// <param name="connectionId">Identificador da conexão.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    Task<Result> RevokeConnectionAsync(
        Guid workspaceId,
        Guid connectionId,
        CancellationToken cancellationToken = default);
}
