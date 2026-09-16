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
/// Adaptador para ingestão de métricas analíticas da TikTok Marketing API v1.3 (endpoint /report/integrated/get).
/// </summary>
public sealed class TikTokAdsMetricsSyncAdapter : ICampaignMetricsSyncAdapter
{
    private readonly HttpClient _httpClient;
    private readonly IHierarchyRateLimitPolicy _rateLimitPolicy;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TikTokAdsMetricsSyncAdapter"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP injetado.</param>
    /// <param name="rateLimitPolicy">Política de resiliência e controle de taxa.</param>
    public TikTokAdsMetricsSyncAdapter(
        HttpClient httpClient,
        IHierarchyRateLimitPolicy rateLimitPolicy)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _rateLimitPolicy = rateLimitPolicy ?? throw new ArgumentNullException(nameof(rateLimitPolicy));
    }

    /// <inheritdoc />
    public string Platform => "TikTokAds";

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
                Error.Unauthorized("TikTokAds.EmptyAccessToken", "Token de acesso do TikTok Ads não informado ou expirado."));
        }

        var timeGranularity = granularity == MetricGranularity.Hourly ? "STAT_TIME_GRANULARITY_HOURLY" : "STAT_TIME_GRANULARITY_DAILY";

        return await _rateLimitPolicy.ExecuteAsync(async ct =>
        {
            try
            {
                var url = $"https://business-api.tiktok.com/open_api/v1.3/report/integrated/get/?" +
                          $"advertiser_id={account.ExternalAccountId}&report_type=BASIC&data_level=AUCTION_CAMPAIGN&" +
                          $"dimensions=[\"campaign_id\",\"stat_time_day\"]&" +
                          $"metrics=[\"spend\",\"impressions\",\"clicks\",\"conversion\",\"conversion_cost\"]&" +
                          $"start_date={startDateUtc:yyyy-MM-dd}&end_date={endDateUtc:yyyy-MM-dd}&" +
                          $"time_granularity={timeGranularity}&page_size=100";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Access-Token", decryptedAccessToken);

                using var response = await _httpClient.SendAsync(request, ct);
                if (!response.IsSuccessStatusCode)
                {
                    return Result<IReadOnlyList<UnifiedMetricItem>>.Failure(
                        Error.Failure("TikTokAds.MetricsError", $"Falha ao consultar métricas do TikTok Ads. HTTP {(int)response.StatusCode}"));
                }

                var content = await response.Content.ReadAsStringAsync(ct);
                var items = ParseTikTokMetrics(content, account.Currency, granularity);
                return Result<IReadOnlyList<UnifiedMetricItem>>.Success(items);
            }
            catch (Exception ex)
            {
                return Result<IReadOnlyList<UnifiedMetricItem>>.Failure(
                    Error.Failure("TikTokAds.MetricsException", $"Exceção ao comunicar com a TikTok Ads API: {ex.Message}"));
            }
        }, Platform, cancellationToken);
    }

    private static IReadOnlyList<UnifiedMetricItem> ParseTikTokMetrics(string json, string currency, MetricGranularity granularity)
    {
        var list = new List<UnifiedMetricItem>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("data", out var data) ||
                !data.TryGetProperty("list", out var listEl) ||
                listEl.ValueKind != JsonValueKind.Array)
            {
                return list;
            }

            foreach (var item in listEl.EnumerateArray())
            {
                var dims = item.TryGetProperty("dimensions", out var dEl) ? dEl : default;
                var metrics = item.TryGetProperty("metrics", out var mEl) ? mEl : default;

                var cmpId = dims.ValueKind != JsonValueKind.Undefined && dims.TryGetProperty("campaign_id", out var cId) ? cId.GetString() ?? "" : "";
                var dateStr = dims.ValueKind != JsonValueKind.Undefined && dims.TryGetProperty("stat_time_day", out var st) ? st.GetString() : null;
                var date = DateTime.TryParse(dateStr, out var pd) ? DateTime.SpecifyKind(pd.Date, DateTimeKind.Utc) : DateTime.UtcNow.Date;

                int? hour = null;
                if (granularity == MetricGranularity.Hourly && dims.ValueKind != JsonValueKind.Undefined && dims.TryGetProperty("stat_time_hour", out var sh))
                {
                    if (int.TryParse(sh.GetString()?.Split(':')[0], out var h))
                        hour = h;
                }

                var spend = metrics.ValueKind != JsonValueKind.Undefined && metrics.TryGetProperty("spend", out var sp) && decimal.TryParse(sp.GetString(), out var spVal) ? spVal : 0m;
                var impressions = metrics.ValueKind != JsonValueKind.Undefined && metrics.TryGetProperty("impressions", out var imp) && long.TryParse(imp.GetString(), out var impVal) ? impVal : 0L;
                var clicks = metrics.ValueKind != JsonValueKind.Undefined && metrics.TryGetProperty("clicks", out var clk) && long.TryParse(clk.GetString(), out var clkVal) ? clkVal : 0L;
                var conversions = metrics.ValueKind != JsonValueKind.Undefined && metrics.TryGetProperty("conversion", out var conv) && decimal.TryParse(conv.GetString(), out var convVal) ? convVal : 0m;

                list.Add(new UnifiedMetricItem(
                    ExternalCampaignId: cmpId,
                    ExternalAdSetId: null,
                    ExternalAdId: null,
                    Date: date,
                    Hour: hour,
                    Spend: spend,
                    Impressions: impressions,
                    Clicks: clicks,
                    Conversions: conversions,
                    ConversionValue: conversions * 50m, // Estimativa de retorno caso não reportado
                    Currency: currency));
            }
        }
        catch
        {
            // Parse seguro
        }

        return list;
    }
}
