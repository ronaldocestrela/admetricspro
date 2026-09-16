namespace Analytics.Domain.Copilot;

/// <summary>
/// Relatório analítico diário consolidado emitido pelo Copiloto de IA (Auditor de Tráfego).
/// </summary>
public sealed record DailyDiagnosticReport
{
    /// <summary>
    /// Identificador do workspace auditado.
    /// </summary>
    public Guid WorkspaceId { get; init; }

    /// <summary>
    /// Data de referência do diagnóstico.
    /// </summary>
    public DateTime ReportDate { get; init; }

    /// <summary>
    /// Resumo executivo principal sintetizado em texto natural.
    /// </summary>
    public string ExecutiveSummary { get; init; }

    /// <summary>
    /// Destaque sintetizado das vitórias do período (campanhas e criativos com desempenho acima da média).
    /// </summary>
    public string WinsSummary { get; init; }

    /// <summary>
    /// Alerta sintetizado dos riscos operacionais (saturação, pacing excessivo, desvios).
    /// </summary>
    public string RisksSummary { get; init; }

    /// <summary>
    /// Lista de anomalias de sobreposição de públicos no Meta Ads detectadas.
    /// </summary>
    public IReadOnlyList<AudienceOverlapAnomaly> AudienceOverlapAnomalies { get; init; }

    /// <summary>
    /// Lista de anomalias de canibalização e disputa de termos de busca detectadas.
    /// </summary>
    public IReadOnlyList<SearchCannibalizationAnomaly> SearchCannibalizationAnomalies { get; init; }

    /// <summary>
    /// Conjunto de ações recomendadas prontas para execução em 1 clique.
    /// </summary>
    public IReadOnlyList<CopilotRecommendationAction> Actions { get; init; }

    /// <summary>
    /// Estimativa total de economia financeira mensal potencial caso todas as ações sejam aplicadas.
    /// </summary>
    public decimal EstimatedMonthlySavings { get; init; }

    /// <summary>
    /// Total de anomalias críticas encontradas.
    /// </summary>
    public int CriticalAnomaliesCount =>
        AudienceOverlapAnomalies.Count(a => a.Severity == CopilotAnomalySeverity.Critical) +
        SearchCannibalizationAnomalies.Count(a => a.Severity == CopilotAnomalySeverity.Critical);

    /// <summary>
    /// Total de anomalias de alta prioridade encontradas.
    /// </summary>
    public int HighAnomaliesCount =>
        AudienceOverlapAnomalies.Count(a => a.Severity == CopilotAnomalySeverity.High) +
        SearchCannibalizationAnomalies.Count(a => a.Severity == CopilotAnomalySeverity.High);

    /// <summary>
    /// Inicializa uma nova instância de <see cref="DailyDiagnosticReport"/>.
    /// </summary>
    public DailyDiagnosticReport(
        Guid workspaceId,
        DateTime reportDate,
        string executiveSummary,
        string winsSummary,
        string risksSummary,
        IEnumerable<AudienceOverlapAnomaly>? audienceOverlapAnomalies,
        IEnumerable<SearchCannibalizationAnomaly>? searchCannibalizationAnomalies,
        IEnumerable<CopilotRecommendationAction>? actions,
        decimal estimatedMonthlySavings)
    {
        WorkspaceId = workspaceId;
        ReportDate = reportDate;
        ExecutiveSummary = executiveSummary;
        WinsSummary = winsSummary;
        RisksSummary = risksSummary;
        AudienceOverlapAnomalies = audienceOverlapAnomalies?.ToList() ?? new List<AudienceOverlapAnomaly>();
        SearchCannibalizationAnomalies = searchCannibalizationAnomalies?.ToList() ?? new List<SearchCannibalizationAnomaly>();
        Actions = actions?.ToList() ?? new List<CopilotRecommendationAction>();
        EstimatedMonthlySavings = estimatedMonthlySavings;
    }
}
