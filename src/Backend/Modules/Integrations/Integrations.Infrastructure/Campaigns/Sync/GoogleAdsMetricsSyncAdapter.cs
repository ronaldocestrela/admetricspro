using System.Net.Http.Headers;
using System.Text.Json;
using BuildingBlocks.Domain.Campaigns;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using Integrations.Domain.Campaigns.Models;
using Integrations.Domain.Campaigns.Resilience;
using Integrations.Domain.Campaigns.Sync;

namespace Integrations.Infrastructure.Campaigns.Sync;

/// <summary>
/// Adaptador para ingestão de métricas analíticas da Google Ads API v18 (SearchStream / GAQL).
/// </summary>
public sealed class GoogleAdsMetricsSyncAdapter : ICampaignMetricsSyncAdapter
{
    private readonly HttpClient _httpClient;
    private readonly IHierarchyRateLimitPolicy _rateLimitPolicy;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GoogleAdsMetricsSyncAdapter"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP injetado.</param>
    /// <param name="rateLimitPolicy">Política de resiliência e controle de taxa.</param>
    public GoogleAdsMetricsSyncAdapter(
        HttpClient httpClient,
        IHierarchyRateLimitPolicy rateLimitPolicy)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _rateLimitPolicy = rateLimitPolicy ?? throw new ArgumentNullException(nameof(rateLimitPolicy));
    }

    /// <inheritdoc />
    public string Platform => "GoogleAds";

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<UnifiedMetricItem>>> FetchMetricsAsync(
        ConnectedAdAccount account,
        string? decryptedAccessToken,
        DateTime startDateUtc,
        DateTime endDateUtc,
        MetricGranularity granularity,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(decryptedAccessToken))
        {
            return Result<IReadOnlyList<UnifiedMetricItem>>.Failure(
                Error.Unauthorized("GoogleAds.EmptyAccessToken", "Token de acesso do Google Ads não informado ou expirado."));
        }

        var customerId = account.ExternalAccountId.Replace("-", "").Trim();

        return await _rateLimitPolicy.ExecuteAsync(async ct =>
        {
            try
            {
                var query = granularity == MetricGranularity.Hourly
                    ? $"SELECT campaign.id, segments.date, segments.hour, metrics.cost_micros, metrics.impressions, metrics.clicks, metrics.conversions, metrics.conversions_value FROM campaign WHERE segments.date BETWEEN '{startDateUtc:yyyy-MM-dd}' AND '{endDateUtc:yyyy-MM-dd}'"
                    : $"SELECT campaign.id, segments.date, metrics.cost_micros, metrics.impressions, metrics.clicks, metrics.conversions, metrics.conversions_value FROM campaign WHERE segments.date BETWEEN '{startDateUtc:yyyy-MM-dd}' AND '{endDateUtc:yyyy-MM-dd}'";

                var payload = new { query };
                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"https://googleads.googleapis.com/v18/customers/{customerId}/googleAds:searchStream")
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", decryptedAccessToken);

                using var response = await _httpClient.SendAsync(request, ct);
                if (!response.IsSuccessStatusCode)
                {
                    return Result<IReadOnlyList<UnifiedMetricItem>>.Failure(
                        Error.Failure("GoogleAds.MetricsError", $"Falha ao consultar métricas do Google Ads. HTTP {(int)response.StatusCode}"));
                }

                var content = await response.Content.ReadAsStringAsync(ct);
                var items = ParseGoogleMetrics(content, account.Currency, granularity);
                return Result<IReadOnlyList<UnifiedMetricItem>>.Success(items);
            }
            catch (Exception ex)
            {
                return Result<IReadOnlyList<UnifiedMetricItem>>.Failure(
                    Error.Failure("GoogleAds.MetricsException", $"Exceção ao comunicar com a Google Ads API: {ex.Message}"));
            }
        }, Platform, cancellationToken);
    }

    private static IReadOnlyList<UnifiedMetricItem> ParseGoogleMetrics(string json, string currency, MetricGranularity granularity)
    {
        var list = new List<UnifiedMetricItem>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return list;

            foreach (var batch in doc.RootElement.EnumerateArray())
            {
                if (!batch.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var row in results.EnumerateArray())
                {
                    var campaignId = row.TryGetProperty("campaign", out var cmp) && cmp.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "";
                    var metricsObj = row.TryGetProperty("metrics", out var m) ? m : default;
                    var segmentsObj = row.TryGetProperty("segments", out var seg) ? seg : default;

                    var dateStr = segmentsObj.ValueKind != JsonValueKind.Undefined && segmentsObj.TryGetProperty("date", out var d) ? d.GetString() : null;
                    var date = DateTime.TryParse(dateStr, out var parsedDate) ? DateTime.SpecifyKind(parsedDate.Date, DateTimeKind.Utc) : DateTime.UtcNow.Date;

                    int? hour = null;
                    if (granularity == MetricGranularity.Hourly && segmentsObj.ValueKind != JsonValueKind.Undefined && segmentsObj.TryGetProperty("hour", out var hEl))
                    {
                        if (int.TryParse(hEl.GetString(), out var hVal))
                            hour = hVal;
                    }

                    var costMicros = metricsObj.ValueKind != JsonValueKind.Undefined && metricsObj.TryGetProperty("costMicros", out var cm) && long.TryParse(cm.GetString(), out var cmVal) ? cmVal : 0L;
                    var spend = Math.Round(costMicros / 1_000_000m, 4);

                    var impressions = metricsObj.ValueKind != JsonValueKind.Undefined && metricsObj.TryGetProperty("impressions", out var imp) && long.TryParse(imp.GetString(), out var iVal) ? iVal : 0L;
                    var clicks = metricsObj.ValueKind != JsonValueKind.Undefined && metricsObj.TryGetProperty("clicks", out var clk) && long.TryParse(clk.GetString(), out var clkVal) ? clkVal : 0L;
                    var conversions = metricsObj.ValueKind != JsonValueKind.Undefined && metricsObj.TryGetProperty("conversions", out var conv) && decimal.TryParse(conv.GetString(), out var cVal) ? cVal : 0m;
                    var conversionValue = metricsObj.ValueKind != JsonValueKind.Undefined && metricsObj.TryGetProperty("conversionsValue", out var cv) && decimal.TryParse(cv.GetString(), out var cvVal) ? cvVal : 0m;

                    list.Add(new UnifiedMetricItem(
                        ExternalCampaignId: campaignId,
                        ExternalAdSetId: null,
                        ExternalAdId: null,
                        Date: date,
                        Hour: hour,
                        Spend: spend,
                        Impressions: impressions,
                        Clicks: clicks,
                        Conversions: conversions,
                        ConversionValue: conversionValue,
                        Currency: currency));
                }
            }
        }
        catch
        {
            // Fallback gracioso
        }

        return list;
    }
}
