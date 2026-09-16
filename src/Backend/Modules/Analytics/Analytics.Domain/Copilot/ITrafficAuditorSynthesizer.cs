namespace Analytics.Domain.Copilot;

/// <summary>
/// Contrato do orquestrador de síntese analítica do Copiloto de IA em texto natural.
/// </summary>
public interface ITrafficAuditorSynthesizer
{
    /// <summary>
    /// Sintetiza o relatório executivo diário em linguagem natural baseado nas anomalias encontradas e no contexto do workspace.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="reportDate">Data de referência.</param>
    /// <param name="overlapAnomalies">Anomalias de sobreposição detectadas.</param>
    /// <param name="cannibalizationAnomalies">Anomalias de canibalização detectadas.</param>
    /// <param name="overallPerformance">Resumo textual de métricas ou dados agregados.</param>
    /// <returns>Relatório executivo diário consolidado.</returns>
    DailyDiagnosticReport SynthesizeDailyDiagnostic(
        Guid workspaceId,
        DateTime reportDate,
        IReadOnlyList<AudienceOverlapAnomaly> overlapAnomalies,
        IReadOnlyList<SearchCannibalizationAnomaly> cannibalizationAnomalies,
        string? overallPerformance = null);
}
