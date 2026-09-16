using BuildingBlocks.Domain.Primitives;

namespace Analytics.Domain.Creatives;

/// <summary>
/// Contrato do motor analítico de detecção de fadiga e saturação de criativos.
/// </summary>
public interface IAdFatigueDetector
{
    /// <summary>
    /// Analisa uma série temporal contínua de métricas diárias de um criativo para diagnosticar sinais de fadiga publicitária.
    /// </summary>
    /// <param name="adId">Identificador do anúncio.</param>
    /// <param name="adName">Nome do anúncio.</param>
    /// <param name="platform">Plataforma do anúncio.</param>
    /// <param name="previewUrl">URL opcional de preview.</param>
    /// <param name="dailyMetrics">Histórico de pontos diários de métricas (idealmente últimos 7 dias).</param>
    /// <returns>Resultado com o diagnóstico de integridade ou erro de validação caso os dados sejam insuficientes.</returns>
    Result<CreativeFatigueAnalysisResult> AnalyzeFatigue(
        Guid adId,
        string adName,
        string platform,
        string? previewUrl,
        IReadOnlyList<CreativeDailyMetricPoint> dailyMetrics);
}
