namespace Analytics.Domain.Copilot;

/// <summary>
/// Implementação do detector de anomalias de sobreposição de públicos no Meta Ads (Audience Overlap).
/// Utiliza o índice de similaridade de Jaccard sobre tags de segmentação e analisa impacto em CPA e CPM.
/// </summary>
public sealed class MetaAudienceOverlapDetector : IAudienceOverlapDetector
{
    private const decimal HighOverlapThreshold = 30.0m;
    private const decimal CriticalOverlapThreshold = 50.0m;

    /// <inheritdoc />
    public IReadOnlyList<AudienceOverlapAnomaly> DetectOverlaps(IReadOnlyList<AdSetAudienceTargeting> adSets)
    {
        if (adSets is null || adSets.Count < 2)
        {
            return Array.Empty<AudienceOverlapAnomaly>();
        }

        // Analisa exclusivamente conjuntos ativos com segmentações registradas
        var activeAdSets = adSets
            .Where(a => string.Equals(a.Status, "Active", StringComparison.OrdinalIgnoreCase) && a.TargetingTags.Count > 0)
            .ToList();

        if (activeAdSets.Count < 2)
        {
            return Array.Empty<AudienceOverlapAnomaly>();
        }

        var anomalies = new List<AudienceOverlapAnomaly>();

        for (var i = 0; i < activeAdSets.Count; i++)
        {
            for (var j = i + 1; j < activeAdSets.Count; j++)
            {
                var adSetA = activeAdSets[i];
                var adSetB = activeAdSets[j];

                var tagsA = adSetA.TargetingTags.Select(t => t.Trim().ToLowerInvariant()).ToHashSet();
                var tagsB = adSetB.TargetingTags.Select(t => t.Trim().ToLowerInvariant()).ToHashSet();

                var intersection = tagsA.Intersect(tagsB).ToList();
                var union = tagsA.Union(tagsB).ToList();

                if (union.Count == 0)
                {
                    continue;
                }

                var jaccard = (decimal)intersection.Count / union.Count;
                var overlapPercentage = Math.Round(jaccard * 100m, 2);

                if (overlapPercentage < HighOverlapThreshold)
                {
                    continue;
                }

                var severity = overlapPercentage >= CriticalOverlapThreshold
                    ? CopilotAnomalySeverity.Critical
                    : CopilotAnomalySeverity.High;

                // Eleger o conjunto com pior desempenho (maior CPA ou maior CPM) para sugestão de pausa
                var worstAdSet = (adSetA.Cpa >= adSetB.Cpa && adSetA.Cpa > 0) ? adSetA :
                                 (adSetB.Cpa > 0) ? adSetB :
                                 (adSetA.Cpm >= adSetB.Cpm ? adSetA : adSetB);

                var bestAdSet = worstAdSet.AdSetId == adSetA.AdSetId ? adSetB : adSetA;

                var action = new CopilotRecommendationAction(
                    actionId: Guid.NewGuid(),
                    actionType: CopilotActionType.PauseAdSet,
                    targetEntityId: worstAdSet.AdSetId,
                    targetEntityName: worstAdSet.AdSetName,
                    platform: "MetaAds",
                    title: $"Pausar conjunto redundante ({worstAdSet.AdSetName})",
                    description: $"Pausa o conjunto '{worstAdSet.AdSetName}' (CPA R$ {worstAdSet.Cpa:N2}) para estancar a auto-concorrência contra '{bestAdSet.AdSetName}' (CPA R$ {bestAdSet.Cpa:N2}) e reduzir o CPM inflacionado.",
                    parameters: new Dictionary<string, string>
                    {
                        ["adSetIdToPause"] = worstAdSet.AdSetId.ToString(),
                        ["campaignId"] = worstAdSet.CampaignId.ToString(),
                        ["overlapPercentage"] = overlapPercentage.ToString("F1"),
                        ["sharedTags"] = string.Join(",", intersection)
                    }
                );

                var severityLabel = severity == CopilotAnomalySeverity.Critical ? "crítica" : "relevante";
                var description = $"Sobreposição {severityLabel} de público detectada ({overlapPercentage:N1}% de similaridade). " +
                                  $"Os conjuntos '{adSetA.AdSetName}' e '{adSetB.AdSetName}' disputam o mesmo leilão no Meta Ads " +
                                  $"com interesses sobrepostos [{string.Join(", ", intersection)}], gerando auto-concorrência e dispersão de verba.";

                anomalies.Add(new AudienceOverlapAnomaly(
                    adSetIdA: adSetA.AdSetId,
                    adSetNameA: adSetA.AdSetName,
                    campaignIdA: adSetA.CampaignId,
                    campaignNameA: adSetA.CampaignName,
                    adSetIdB: adSetB.AdSetId,
                    adSetNameB: adSetB.AdSetName,
                    campaignIdB: adSetB.CampaignId,
                    campaignNameB: adSetB.CampaignName,
                    overlapPercentage: overlapPercentage,
                    cpmA: adSetA.Cpm,
                    cpmB: adSetB.Cpm,
                    severity: severity,
                    description: description,
                    suggestedAction: action,
                    sharedTargetingTags: intersection
                ));
            }
        }

        return anomalies
            .OrderByDescending(a => a.Severity)
            .ThenByDescending(a => a.OverlapPercentage)
            .ToList();
    }
}
