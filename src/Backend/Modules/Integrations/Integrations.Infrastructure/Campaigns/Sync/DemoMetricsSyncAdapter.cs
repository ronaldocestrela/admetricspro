using BuildingBlocks.Domain.Campaigns;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using Integrations.Domain.Campaigns.Models;
using Integrations.Domain.Campaigns.Sync;

namespace Integrations.Infrastructure.Campaigns.Sync;

/// <summary>
/// Adaptador de ingestão de métricas para contas de demonstração (FTUX).
/// Gera séries temporais determinísticas diárias e horárias com KPIs realistas para visualização imediata.
/// </summary>
public sealed class DemoMetricsSyncAdapter : ICampaignMetricsSyncAdapter
{
    /// <inheritdoc />
    public string Platform => "Demo";

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<UnifiedMetricItem>>> FetchMetricsAsync(
        ConnectedAdAccount account,
        string? decryptedAccessToken,
        DateTime startDateUtc,
        DateTime endDateUtc,
        MetricGranularity granularity,
        CancellationToken cancellationToken = default)
    {
        var platform = account.Platform;
        var prefix = platform.ToLowerInvariant();
        var currency = string.IsNullOrWhiteSpace(account.Currency) ? "BRL" : account.Currency;

        var campaigns = new[]
        {
            $"{prefix}_demo_cmp_101",
            $"{prefix}_demo_cmp_102",
            $"{prefix}_demo_cmp_103"
        };

        var start = DateTime.SpecifyKind(startDateUtc.Date, DateTimeKind.Utc);
        var end = DateTime.SpecifyKind(endDateUtc.Date, DateTimeKind.Utc);

        var metrics = new List<UnifiedMetricItem>();

        for (var date = start; date <= end; date = date.AddDays(1))
        {
            var dayOffset = (int)(date - start).TotalDays;

            foreach (var cmpId in campaigns)
            {
                var baseSpend = cmpId.EndsWith("101", StringComparison.Ordinal) ? 100.00m :
                                cmpId.EndsWith("102", StringComparison.Ordinal) ? 220.00m : 350.00m;

                // Fator de variação determinístico pelo dia
                var dayFactor = 0.85m + ((dayOffset % 7) * 0.05m);
                var dailySpend = Math.Round(baseSpend * dayFactor, 2);
                var dailyImpressions = (long)(dailySpend * (cmpId.EndsWith("101", StringComparison.Ordinal) ? 80 : 35));
                var dailyClicks = (long)(dailyImpressions * 0.038m);
                var dailyConversions = Math.Round((decimal)dailyClicks * (cmpId.EndsWith("103", StringComparison.Ordinal) ? 0.08m : 0.03m), 2);
                var dailyRevenue = Math.Round(dailySpend * (cmpId.EndsWith("103", StringComparison.Ordinal) ? 4.2m : 2.8m), 2);

                if (granularity == MetricGranularity.Daily)
                {
                    metrics.Add(new UnifiedMetricItem(
                        ExternalCampaignId: cmpId,
                        ExternalAdSetId: null,
                        ExternalAdId: null,
                        Date: date,
                        Hour: null,
                        Spend: dailySpend,
                        Impressions: dailyImpressions,
                        Clicks: dailyClicks,
                        Conversions: dailyConversions,
                        ConversionValue: dailyRevenue,
                        Currency: currency));
                }
                else
                {
                    // Granularidade horária: distribui o dia em 24 fatias com curva de pico comercial (9h às 21h)
                    for (var hour = 0; hour < 24; hour++)
                    {
                        var hourWeight = hour is >= 9 and <= 21 ? 0.06m : 0.015m;
                        var hourlySpend = Math.Round(dailySpend * hourWeight, 2);
                        var hourlyImpressions = (long)(dailyImpressions * hourWeight);
                        var hourlyClicks = (long)(dailyClicks * hourWeight);
                        var hourlyConversions = Math.Round(dailyConversions * hourWeight, 2);
                        var hourlyRevenue = Math.Round(dailyRevenue * hourWeight, 2);

                        metrics.Add(new UnifiedMetricItem(
                            ExternalCampaignId: cmpId,
                            ExternalAdSetId: null,
                            ExternalAdId: null,
                            Date: date,
                            Hour: hour,
                            Spend: hourlySpend,
                            Impressions: hourlyImpressions,
                            Clicks: hourlyClicks,
                            Conversions: hourlyConversions,
                            ConversionValue: hourlyRevenue,
                            Currency: currency));
                    }
                }
            }
        }

        return Task.FromResult(Result<IReadOnlyList<UnifiedMetricItem>>.Success(metrics));
    }
}
