using BuildingBlocks.Domain.Primitives;

namespace Analytics.Domain.Creatives;

/// <summary>
/// Contrato do provedor de dados de criativos e métricas históricas de desempenho do inquilino.
/// </summary>
public interface ICreativeHubDataProvider
{
    /// <summary>
    /// Recupera os pontos diários de métricas de um anúncio em uma janela de datas para cálculo de fadiga.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="adId">Identificador do anúncio.</param>
    /// <param name="startDateUtc">Data inicial da janela.</param>
    /// <param name="endDateUtc">Data final da janela.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista ordenada cronologicamente de pontos diários de métricas.</returns>
    Task<Result<IReadOnlyList<CreativeDailyMetricPoint>>> GetAdDailyMetricsAsync(
        Guid workspaceId,
        Guid adId,
        DateTime startDateUtc,
        DateTime endDateUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Recupera todos os anúncios ativos de um workspace com metadados básicos para análise em lote.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista de metadados de anúncios ativos do workspace.</returns>
    Task<Result<IReadOnlyList<CreativeMetadata>>> GetWorkspaceActiveAdsAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Recupera os pontos de métricas agrupados por ativo de mídia veiculados no Meta Ads e no TikTok Ads.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="assetFingerprint">Identificador de mídia / preview da peça.</param>
    /// <param name="startDateUtc">Data inicial da janela de análise.</param>
    /// <param name="endDateUtc">Data final da janela de análise.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Tupla com séries de métricas do Meta Ads e do TikTok Ads.</returns>
    Task<Result<(IReadOnlyList<CreativeDailyMetricPoint> Meta, IReadOnlyList<CreativeDailyMetricPoint> TikTok)>> GetCrossPlatformMetricsAsync(
        Guid workspaceId,
        string assetFingerprint,
        DateTime startDateUtc,
        DateTime endDateUtc,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Metadados descritivos de um criativo publicitário.
/// </summary>
/// <param name="AdId">Identificador do anúncio.</param>
/// <param name="Name">Nome amigável do criativo.</param>
/// <param name="Platform">Plataforma (MetaAds, TikTokAds, etc.).</param>
/// <param name="PreviewUrl">URL de pré-visualização ou miniatura.</param>
/// <param name="AssetFingerprint">Hash de identificação de mídia compartilhada.</param>
public sealed record CreativeMetadata(
    Guid AdId,
    string Name,
    string Platform,
    string? PreviewUrl,
    string AssetFingerprint);
