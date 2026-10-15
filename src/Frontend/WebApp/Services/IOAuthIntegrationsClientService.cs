using BuildingBlocks.Domain.Primitives;
using Integrations.Application.OAuth.DTOs;

namespace WebApp.Services;

/// <summary>
/// Contrato do cliente HTTP fortemente tipado para gestão das conexões OAuth2 com redes de anúncios (Meta, Google, TikTok, Bing)
/// e renovação de credenciais no Token Vault.
/// </summary>
public interface IOAuthIntegrationsClientService
{
    /// <summary>Lista contas de anúncios acessíveis pela autorização do workspace.</summary>
    /// <param name="workspaceId">Workspace proprietário.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Contas verificadas na Meta.</returns>
    Task<Result<IReadOnlyList<Integrations.Domain.Meta.MetaAdAccount>>> GetMetaAdAccountsAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    /// <summary>Lista vínculos já cadastrados no workspace.</summary>
    /// <param name="workspaceId">Workspace proprietário.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Contas vinculadas sem tokens.</returns>
    Task<Result<IReadOnlyList<Tenants.Application.Integrations.DTOs.ConnectedAdAccountDto>>> GetLinkedAdAccountsAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    /// <summary>Confirma contas Meta selecionadas pelo usuário.</summary>
    /// <param name="workspaceId">Workspace proprietário.</param>
    /// <param name="accountIds">IDs externos escolhidos.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>IDs internos dos vínculos.</returns>
    Task<Result<IReadOnlyList<Guid>>> SelectMetaAdAccountsAsync(Guid workspaceId, IReadOnlyList<string> accountIds, CancellationToken cancellationToken = default);

    /// <summary>Desvincula uma conta sem revogar a autorização Meta.</summary>
    /// <param name="workspaceId">Workspace proprietário.</param>
    /// <param name="accountId">ID interno do vínculo.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    Task<Result> DisconnectMetaAdAccountAsync(Guid workspaceId, Guid accountId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Obtém o status de todas as conexões OAuth vinculadas a um workspace da agência.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista com metadados e status das conexões ativas ou falha semântica.</returns>
    Task<Result<IReadOnlyList<OAuthConnectionStatusDto>>> GetConnectionsStatusAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dispara renovação preventiva imediata dos tokens de acesso prestes a expirar.
    /// </summary>
    /// <param name="thresholdHours">Janela de antecedência em horas (opcional).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Total de conexões renovadas com sucesso.</returns>
    Task<Result<int>> RefreshExpiringTokensAsync(
        int? thresholdHours = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inicia o fluxo de consentimento OAuth gerando a URL de autorização da plataforma.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="platform">Plataforma (MetaAds, GoogleAds, TikTokAds, BingAds).</param>
    /// <param name="redirectUri">URI de retorno configurada.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Dados contendo a URL de autorização externa.</returns>
    Task<Result<OAuthAuthorizationUrlDto>> InitiateOAuthFlowAsync(
        Guid workspaceId,
        string platform,
        string redirectUri,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoga e desativa uma conexão OAuth do workspace no Token Vault.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="platform">Plataforma a revogar.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    Task<Result> RevokeConnectionAsync(
        Guid workspaceId,
        string platform,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processa o retorno da plataforma de anúncios após consentimento do usuário, troca o código por tokens e cifra no Token Vault.
    /// </summary>
    /// <param name="code">Código de autorização retornado pela plataforma.</param>
    /// <param name="state">Estado assinado anti-CSRF retornado pela plataforma.</param>
    /// <param name="redirectUri">URI de retorno utilizada na inicialização do fluxo.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Dados e status da conexão persistida no Token Vault.</returns>
    /// <param name="providerError">Negativa de autorização retornada pela plataforma.</param>
    Task<Result<OAuthConnectionStatusDto>> HandleOAuthCallbackAsync(
        string code,
        string state,
        string redirectUri,
        CancellationToken cancellationToken = default,
        string? providerError = null);
}
