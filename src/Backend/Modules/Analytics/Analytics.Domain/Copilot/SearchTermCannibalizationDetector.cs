using System.Globalization;
using System.Text;

namespace Analytics.Domain.Copilot;

/// <summary>
/// Implementação do detector de canibalização e disputa de termos de busca entre canais (Google Ads vs. Bing Ads) ou entre campanhas internas.
/// Normaliza termos e avalia disparidades de CPA, CPC e desperdício de orçamento.
/// </summary>
public sealed class SearchTermCannibalizationDetector : ISearchTermCannibalizationDetector
{
    private const decimal ModerateDisparityThreshold = 1.75m;
    private const decimal CriticalDisparityThreshold = 2.0m;

    /// <inheritdoc />
    public IReadOnlyList<SearchCannibalizationAnomaly> DetectCannibalization(IReadOnlyList<SearchKeywordPerformance> keywords)
    {
        if (keywords is null || keywords.Count < 2)
        {
            return Array.Empty<SearchCannibalizationAnomaly>();
        }

        // Agrupar palavras-chave por termo normalizado (sem acentos e em minúsculas)
        var grouped = keywords
            .Where(k => !string.IsNullOrWhiteSpace(k.Keyword))
            .GroupBy(k => NormalizeSearchTerm(k.Keyword))
            .Where(g => g.Count() >= 2)
            .ToList();

        var anomalies = new List<SearchCannibalizationAnomaly>();

        foreach (var group in grouped)
        {
            var normalizedTerm = group.Key;
            var items = group.ToList();

            // Buscar pares de campanhas ou plataformas distintas competindo pelo mesmo termo
            for (var i = 0; i < items.Count; i++)
            {
                for (var j = i + 1; j < items.Count; j++)
                {
                    var itemA = items[i];
                    var itemB = items[j];

                    // Ignorar comparação consigo mesmo na mesma campanha
                    if (itemA.CampaignId == itemB.CampaignId && itemA.AdGroupId == itemB.AdGroupId)
                    {
                        continue;
                    }

                    // Calcular disparidade de CPA se ambos converteram
                    if (itemA.Cpa > 0 && itemB.Cpa > 0)
                    {
                        var inefficient = itemA.Cpa >= itemB.Cpa ? itemA : itemB;
                        var efficient = inefficient == itemA ? itemB : itemA;

                        var disparity = Math.Round(inefficient.Cpa / efficient.Cpa, 2);

                        if (disparity >= ModerateDisparityThreshold)
                        {
                            var severity = disparity >= CriticalDisparityThreshold
                                ? CopilotAnomalySeverity.Critical
                                : CopilotAnomalySeverity.High;

                            // Desperdício estimado: diferença de CPA multiplicada pelas conversões do canal ineficiente
                            var wastedSpend = Math.Round((inefficient.Cpa - efficient.Cpa) * inefficient.Conversions, 2);
                            if (wastedSpend <= 0)
                            {
                                wastedSpend = Math.Round(Math.Max(0m, (inefficient.Cpc - efficient.Cpc) * inefficient.Clicks), 2);
                            }

                            var action = new CopilotRecommendationAction(
                                actionId: Guid.NewGuid(),
                                actionType: CopilotActionType.AddNegativeKeyword,
                                targetEntityId: inefficient.CampaignId,
                                targetEntityName: inefficient.CampaignName,
                                platform: inefficient.Platform,
                                title: $"Negativar palavra-chave em {inefficient.Platform} ({normalizedTerm})",
                                description: $"Adiciona o termo '{normalizedTerm}' como palavra-chave negativa na campanha '{inefficient.CampaignName}' ({inefficient.Platform}) para concentrar o tráfego no {efficient.Platform}, cujo CPA é {disparity:N1}x mais eficiente.",
                                parameters: new Dictionary<string, string>
                                {
                                    ["keyword"] = normalizedTerm,
                                    ["platformToNegate"] = inefficient.Platform,
                                    ["campaignId"] = inefficient.CampaignId.ToString(),
                                    ["efficientPlatform"] = efficient.Platform,
                                    ["disparityRatio"] = disparity.ToString("F2")
                                }
                            );

                            var severityLabel = severity == CopilotAnomalySeverity.Critical ? "severa" : "relevante";
                            var description = $"Canibalização {severityLabel} detectada no termo '{normalizedTerm}'. " +
                                              $"O canal {inefficient.Platform} (CPA R$ {inefficient.Cpa:N2}) está {disparity:N1}x mais caro que o canal {efficient.Platform} (CPA R$ {efficient.Cpa:N2}), " +
                                              $"gerando um desperdício estimado de R$ {wastedSpend:N2} em leilões concorrentes.";

                            anomalies.Add(new SearchCannibalizationAnomaly(
                                searchTerm: normalizedTerm,
                                channelA: itemA.Platform,
                                campaignIdA: itemA.CampaignId,
                                campaignNameA: itemA.CampaignName,
                                cpcA: itemA.Cpc,
                                cpaA: itemA.Cpa,
                                spendA: itemA.Spend,
                                channelB: itemB.Platform,
                                campaignIdB: itemB.CampaignId,
                                campaignNameB: itemB.CampaignName,
                                cpcB: itemB.Cpc,
                                cpaB: itemB.Cpa,
                                spendB: itemB.Spend,
                                disparityRatio: disparity,
                                estimatedMonthlyWastedSpend: wastedSpend,
                                severity: severity,
                                description: description,
                                suggestedAction: action
                            ));
                        }
                    }
                }
            }
        }

        return anomalies
            .OrderByDescending(a => a.Severity)
            .ThenByDescending(a => a.EstimatedMonthlyWastedSpend)
            .ToList();
    }

    /// <summary>
    /// Normaliza o termo de busca removendo acentuação, pontuações desnecessárias e padronizando em minúsculas.
    /// </summary>
    private static string NormalizeSearchTerm(string term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return string.Empty;
        }

        var normalizedString = term.Trim().Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }
}
