using BuildingBlocks.Domain.Primitives;

namespace Analytics.Domain.Copilot;

/// <summary>
/// Contrato do provedor de dados e executor operacional para as análises do Copiloto de IA no banco do tenant.
/// </summary>
public interface ICopilotDataProvider
{
    /// <summary>
    /// Recupera conjuntos de anúncios do Meta Ads com suas respectivas segmentações e métricas para análise de sobreposição.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="startDateUtc">Data de início da janela analítica.</param>
    /// <param name="endDateUtc">Data de término da janela analítica.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista de conjuntos de anúncios formatados para o detector de overlap.</returns>
    Task<Result<IReadOnlyList<AdSetAudienceTargeting>>> GetMetaAdSetsForAuditAsync(
        Guid workspaceId,
        DateTime startDateUtc,
        DateTime endDateUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Recupera palavras-chave e termos de busca com suas métricas consolidando Google Ads e Bing Ads para análise de canibalização.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="startDateUtc">Data de início da janela analítica.</param>
    /// <param name="endDateUtc">Data de término da janela analítica.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista de palavras-chave formatadas para o detector de canibalização.</returns>
    Task<Result<IReadOnlyList<SearchKeywordPerformance>>> GetSearchKeywordsForAuditAsync(
        Guid workspaceId,
        DateTime startDateUtc,
        DateTime endDateUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executa operacionalmente uma recomendação de 1 clique no banco do tenant.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="action">Ação de recomendação a ser executada.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da execução contendo status e mensagem descritiva.</returns>
    Task<Result<bool>> ExecuteRecommendationActionAsync(
        Guid workspaceId,
        CopilotRecommendationAction action,
        CancellationToken cancellationToken = default);
}
