namespace Analytics.Domain.Copilot;

/// <summary>
/// Implementação do sintetizador analítico do Copiloto de IA em texto natural (Subfase 5.3.2).
/// Compõe relatórios executivos diários estruturados em Vitórias, Riscos e Ações Recomendadas.
/// </summary>
public sealed class TrafficAuditorSynthesizer : ITrafficAuditorSynthesizer
{
    /// <inheritdoc />
    public DailyDiagnosticReport SynthesizeDailyDiagnostic(
        Guid workspaceId,
        DateTime reportDate,
        IReadOnlyList<AudienceOverlapAnomaly> overlapAnomalies,
        IReadOnlyList<SearchCannibalizationAnomaly> cannibalizationAnomalies,
        string? overallPerformance = null)
    {
        overlapAnomalies ??= Array.Empty<AudienceOverlapAnomaly>();
        cannibalizationAnomalies ??= Array.Empty<SearchCannibalizationAnomaly>();

        var totalAnomalies = overlapAnomalies.Count + cannibalizationAnomalies.Count;

        if (totalAnomalies == 0)
        {
            return new DailyDiagnosticReport(
                workspaceId: workspaceId,
                reportDate: reportDate,
                executiveSummary: "Diagnóstico do Copiloto: Campanhas operando dentro dos parâmetros de estabilidade e eficiência. Nenhuma anomalia crítica ou conflito entre canais detectado no período.",
                winsSummary: "As métricas de conversão e custo por clique permanecem consistentes entre as plataformas conectadas.",
                risksSummary: "Nenhum risco iminente de sobreposição ou canibalização de verba registrado.",
                audienceOverlapAnomalies: overlapAnomalies,
                searchCannibalizationAnomalies: cannibalizationAnomalies,
                actions: Array.Empty<CopilotRecommendationAction>(),
                estimatedMonthlySavings: 0m
            );
        }

        var criticalCount = overlapAnomalies.Count(a => a.Severity == CopilotAnomalySeverity.Critical) +
                            cannibalizationAnomalies.Count(a => a.Severity == CopilotAnomalySeverity.Critical);

        var highCount = overlapAnomalies.Count(a => a.Severity == CopilotAnomalySeverity.High) +
                        cannibalizationAnomalies.Count(a => a.Severity == CopilotAnomalySeverity.High);

        // Estimativa de economia: desperdício de canibalização somado a alívio projetado de CPM em conjuntos sobrepostos (est. R$ 250/conjunto/mês)
        var estimatedSavings = cannibalizationAnomalies.Sum(a => a.EstimatedMonthlyWastedSpend) +
                               (overlapAnomalies.Count * 250m);

        var executiveSummary = $"Diagnóstico do Copiloto: Foram identificadas {criticalCount} anomalias críticas e {highCount} alertas de alta prioridade " +
                               $"demandando ação imediata. A aplicação das recomendações sugeridas pode gerar uma economia potencial de R$ {estimatedSavings:N2} por mês.";

        var winsSummary = !string.IsNullOrWhiteSpace(overallPerformance)
            ? overallPerformance
            : "Campanhas sem anomalias continuam operando com retorno positivo. O foco primordial de otimização hoje é estancar auto-concorrência e disparidades entre canais.";

        var riskPoints = new List<string>();
        if (overlapAnomalies.Count > 0)
        {
            riskPoints.Add($"Sobreposição de públicos detectada no Meta Ads ({overlapAnomalies.Count} ocorrências), provocando inflação de CPM e canibalização de leilão interno.");
        }

        if (cannibalizationAnomalies.Count > 0)
        {
            riskPoints.Add($"Canibalização e disputa de termos de busca ({cannibalizationAnomalies.Count} ocorrências) entre Google e Bing, gerando dispersão de verba em canais de alto CPA.");
        }

        var risksSummary = string.Join(" ", riskPoints);

        var actions = new List<CopilotRecommendationAction>();
        actions.AddRange(overlapAnomalies.Select(a => a.SuggestedAction));
        actions.AddRange(cannibalizationAnomalies.Select(a => a.SuggestedAction));

        return new DailyDiagnosticReport(
            workspaceId: workspaceId,
            reportDate: reportDate,
            executiveSummary: executiveSummary,
            winsSummary: winsSummary,
            risksSummary: risksSummary,
            audienceOverlapAnomalies: overlapAnomalies,
            searchCannibalizationAnomalies: cannibalizationAnomalies,
            actions: actions,
            estimatedMonthlySavings: estimatedSavings
        );
    }
}
