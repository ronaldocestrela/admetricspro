using BuildingBlocks.Domain.Primitives;

namespace Analytics.Domain.Creatives;

/// <summary>
/// Contrato do agregador analítico responsável por consolidar métricas por ativo de mídia e contrastar redes distintas.
/// </summary>
public interface ICreativeHubAggregator
{
    /// <summary>
    /// Compara o desempenho do mesmo ativo publicitário entre o Meta Ads e o TikTok Ads.
    /// </summary>
    /// <param name="assetFingerprint">Identificador único da mídia / peça.</param>
    /// <param name="assetName">Nome de exibição da peça.</param>
    /// <param name="previewUrl">URL opcional de preview.</param>
    /// <param name="metaMetrics">Série de métricas observadas no Meta Ads.</param>
    /// <param name="tikTokMetrics">Série de métricas observadas no TikTok Ads.</param>
    /// <returns>Resultado com o comparativo detalhado e a rede vencedora.</returns>
    Result<CrossPlatformComparisonResult> CompareCrossPlatform(
        string assetFingerprint,
        string assetName,
        string? previewUrl,
        IReadOnlyList<CreativeDailyMetricPoint> metaMetrics,
        IReadOnlyList<CreativeDailyMetricPoint> tikTokMetrics);
}
