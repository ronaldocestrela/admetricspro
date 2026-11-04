using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Application.Brand.DTOs;
using SocialPublishing.Application.Connections.Commands.CompleteOrganicAuthorization;
using SocialPublishing.Application.Connections.DTOs;
using SocialPublishing.Application.Approvals.DTOs;
using SocialPublishing.Application.Connections.Queries.StartOrganicAuthorization;
using SocialPublishing.Application.Planning.DTOs;
using SocialPublishing.Application.Posts.DTOs;
using SocialPublishing.Application.Traction.DTOs;
using SocialPublishing.Domain.Brand;
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

    /// <summary>
    /// Lista posts orgânicos agendados em um intervalo UTC para alimentar o calendário.
    /// </summary>
    /// <param name="workspaceId">Workspace proprietário.</param>
    /// <param name="fromUtc">Limite inferior UTC inclusivo.</param>
    /// <param name="toUtc">Limite superior UTC inclusivo.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Posts no intervalo ou falha semântica.</returns>
    Task<Result<IReadOnlyList<OrganicPostDto>>> ListPostsAsync(
        Guid workspaceId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reagenda um post orgânico para um novo instante UTC.
    /// </summary>
    /// <param name="workspaceId">Workspace proprietário.</param>
    /// <param name="postId">Identificador do post.</param>
    /// <param name="scheduledAtUtc">Novo horário em UTC.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Post atualizado ou falha de negócio.</returns>
    Task<Result<OrganicPostDto>> RescheduleAsync(
        Guid workspaceId,
        Guid postId,
        DateTime scheduledAtUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Emite link compartilhável de aprovação orgânica (token em claro só nesta resposta).
    /// </summary>
    /// <param name="workspaceId">Workspace do pacote.</param>
    /// <param name="expiresAtUtc">Expiração do link em UTC.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task<Result<ApprovalLinkCreatedDto>> CreateApprovalLinkAsync(
        Guid workspaceId,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém o DNA de marca persistido para o workspace.
    /// </summary>
    /// <param name="workspaceId">Workspace proprietário.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Perfil de voz de marca ou ausência.</returns>
    Task<Result<BrandVoiceDto>> GetBrandVoiceAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cria ou atualiza o DNA de marca do workspace.
    /// </summary>
    /// <param name="workspaceId">Workspace proprietário.</param>
    /// <param name="tone">Tom de voz.</param>
    /// <param name="persona">Persona da marca.</param>
    /// <param name="mandatoryLinks">Links absolutos obrigatórios.</param>
    /// <param name="forbiddenTerms">Termos proibidos.</param>
    /// <param name="emojiPolicy">Política de emojis.</param>
    /// <param name="hashtagLimit">Teto de hashtags.</param>
    /// <param name="authorityWeight">Peso do pilar Autoridade.</param>
    /// <param name="saleWeight">Peso do pilar Venda.</param>
    /// <param name="connectionWeight">Peso do pilar Conexão.</param>
    /// <param name="socialProofWeight">Peso do pilar Prova social.</param>
    /// <param name="languageCode">Código de idioma.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Perfil persistido ou falha de validação.</returns>
    Task<Result<BrandVoiceDto>> UpsertBrandVoiceAsync(
        Guid workspaceId,
        string tone,
        string persona,
        IReadOnlyList<string> mandatoryLinks,
        IReadOnlyList<string> forbiddenTerms,
        EmojiPolicy emojiPolicy,
        int hashtagLimit,
        int authorityWeight,
        int saleWeight,
        int connectionWeight,
        int socialProofWeight,
        string languageCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Materializa um plano editorial em posts orgânicos em rascunho.
    /// </summary>
    /// <param name="workspaceId">Workspace proprietário.</param>
    /// <param name="rangeStart">Data inicial inclusiva.</param>
    /// <param name="rangeEnd">Data final inclusiva.</param>
    /// <param name="timeZoneId">Identificador IANA do fuso.</param>
    /// <param name="postsPerWeek">Posts por semana (1–7).</param>
    /// <param name="seed">Semente determinística do planner.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Posts criados e slots que falharam.</returns>
    Task<Result<GeneratedPlanDto>> GeneratePlanAsync(
        Guid workspaceId,
        DateOnly rangeStart,
        DateOnly rangeEnd,
        string timeZoneId,
        int postsPerWeek,
        int seed,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista sugestões de impulsionamento por tração orgânica do workspace.
    /// </summary>
    /// <param name="workspaceId">Workspace proprietário.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Sugestões persistidas ou falha semântica.</returns>
    Task<Result<IReadOnlyList<PaidBoostSuggestionDto>>> ListBoostSuggestionsAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Aceita uma sugestão e solicita rascunho pausado de anúncio pago.
    /// </summary>
    /// <param name="workspaceId">Workspace proprietário.</param>
    /// <param name="suggestionId">Identificador da sugestão.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Sugestão atualizada ou erro propagado.</returns>
    Task<Result<PaidBoostSuggestionDto>> AcceptBoostSuggestionAsync(
        Guid workspaceId,
        Guid suggestionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dispensa uma sugestão de impulsionamento sem criar rascunho.
    /// </summary>
    /// <param name="workspaceId">Workspace proprietário.</param>
    /// <param name="suggestionId">Identificador da sugestão.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Sugestão dispensada ou falha de negócio.</returns>
    Task<Result<PaidBoostSuggestionDto>> DismissBoostSuggestionAsync(
        Guid workspaceId,
        Guid suggestionId,
        CancellationToken cancellationToken = default);
}
