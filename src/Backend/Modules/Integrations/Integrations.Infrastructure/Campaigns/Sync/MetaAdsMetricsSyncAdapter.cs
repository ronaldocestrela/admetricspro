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
/// Adaptador para ingestão de métricas analíticas da Meta Ads Graph API v21.0 (endpoint /insights).
/// </summary>
public sealed class MetaAdsMetricsSyncAdapter : ICampaignMetricsSyncAdapter
{
    private readonly HttpClient _httpClient;
    private readonly IHierarchyRateLimitPolicy _rateLimitPolicy;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="MetaAdsMetricsSyncAdapter"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP injetado.</param>
    /// <param name="rateLimitPolicy">Política de resiliência e controle de taxa.</param>
    public MetaAdsMetricsSyncAdapter(
        HttpClient httpClient,
        IHierarchyRateLimitPolicy rateLimitPolicy)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _rateLimitPolicy = rateLimitPolicy ?? throw new ArgumentNullException(nameof(rateLimitPolicy));
    }

    /// <inheritdoc />
    public string Platform => "MetaAds";

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
                Error.Unauthorized("MetaAds.EmptyAccessToken", "Token de acesso da Meta Ads não informado ou expirado."));
        }

        var externalId = account.ExternalAccountId.StartsWith("act_", StringComparison.OrdinalIgnoreCase)
            ? account.ExternalAccountId
            : $"act_{account.ExternalAccountId}";

        var timeIncrement = granularity == MetricGranularity.Hourly ? "hourly" : "1";
        var datePreset = $"{startDateUtc:yyyy-MM-dd},{endDateUtc:yyyy-MM-dd}";

        return await _rateLimitPolicy.ExecuteAsync(async ct =>
        {
            try
            {
                var url = $"https://graph.facebook.com/v21.0/{externalId}/insights?" +
                          $"level=campaign&time_increment={timeIncrement}&time_range={{\"since\":\"{startDateUtc:yyyy-MM-dd}\",\"until\":\"{endDateUtc:yyyy-MM-dd}\"}}&" +
                          $"fields=campaign_id,spend,impressions,clicks,actions,action_values&limit=100";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", decryptedAccessToken);

                using var response = await _httpClient.SendAsync(request, ct);

                if (!response.IsSuccessStatusCode)
                {
                    var statusCode = (int)response.StatusCode;
                    return Result<IReadOnlyList<UnifiedMetricItem>>.Failure(
                        Error.Failure("MetaAds.InsightsError", $"Falha ao consultar métricas da Meta Ads. HTTP {statusCode}"));
                }

                // Parse básico ou retorno de lista consolidada
                var content = await response.Content.ReadAsStringAsync(ct);
                var items = ParseMetaInsights(content, account.Currency, granularity);

                return Result<IReadOnlyList<UnifiedMetricItem>>.Success(items);
            }
            catch (Exception ex)
            {
                return Result<IReadOnlyList<UnifiedMetricItem>>.Failure(
                    Error.Failure("MetaAds.InsightsException", $"Exceção ao comunicar com a Meta Insights API: {ex.Message}"));
            }
        }, Platform, cancellationToken);
    }

    private static IReadOnlyList<UnifiedMetricItem> ParseMetaInsights(string json, string currency, MetricGranularity granularity)
    {
        var list = new List<UnifiedMetricItem>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            {
                return list;
            }

            foreach (var element in data.EnumerateArray())
            {
                var cmpId = element.TryGetProperty("campaign_id", out var cid) ? cid.GetString() ?? "" : "";
                var spend = element.TryGetProperty("spend", out var sp) && decimal.TryParse(sp.GetString(), out var s) ? s : 0m;
                var impressions = element.TryGetProperty("impressions", out var imp) && long.TryParse(imp.GetString(), out var i) ? i : 0L;
                var clicks = element.TryGetProperty("clicks", out var cl) && long.TryParse(cl.GetString(), out var c) ? c : 0L;
                var dateStr = element.TryGetProperty("date_start", out var ds) ? ds.GetString() : null;
                var date = DateTime.TryParse(dateStr, out var d) ? DateTime.SpecifyKind(d.Date, DateTimeKind.Utc) : DateTime.UtcNow.Date;

                int? hour = null;
                if (granularity == MetricGranularity.Hourly && element.TryGetProperty("hourly_stats_aggregated_by_audience_time_zone", out var hEl))
                {
                    var hStr = hEl.GetString();
                    if (int.TryParse(hStr?.Split(':')[0], out var h))
                    {
                        hour = h;
                    }
                }

                decimal conversions = 0m;
                decimal conversionValue = 0m;

                if (element.TryGetProperty("actions", out var actions) && actions.ValueKind == JsonValueKind.Array)
                {
                    foreach (var action in actions.EnumerateArray())
                    {
                        if (action.TryGetProperty("value", out var val) && decimal.TryParse(val.GetString(), out var cv))
                        {
                            conversions += cv;
                        }
                    }
                }

                if (element.TryGetProperty("action_values", out var actionVals) && actionVals.ValueKind == JsonValueKind.Array)
                {
                    foreach (var actVal in actionVals.EnumerateArray())
                    {
                        if (actVal.TryGetProperty("value", out var v) && decimal.TryParse(v.GetString(), out var rv))
                        {
                            conversionValue += rv;
                        }
                    }
                }

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
                    ConversionValue: conversionValue,
                    Currency: currency));
            }
        }
        catch
        {
            // Fallback seguro em parse
        }

        return list;
    }
}
