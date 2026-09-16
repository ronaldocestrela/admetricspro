namespace Analytics.Domain.Copilot;

/// <summary>
/// Contrato do detector de anomalias de sobreposição de públicos no Meta Ads (Audience Overlap).
/// </summary>
public interface IAudienceOverlapDetector
{
    /// <summary>
    /// Analisa uma lista de conjuntos de anúncios e identifica pares de conjuntos ativos com alta sobreposição prejudicial.
    /// </summary>
    /// <param name="adSets">Lista de conjuntos de anúncios com segmentações e métricas.</param>
    /// <returns>Lista de anomalias de sobreposição detectadas ordenadas por severidade e taxa de overlap.</returns>
    IReadOnlyList<AudienceOverlapAnomaly> DetectOverlaps(IReadOnlyList<AdSetAudienceTargeting> adSets);
}
