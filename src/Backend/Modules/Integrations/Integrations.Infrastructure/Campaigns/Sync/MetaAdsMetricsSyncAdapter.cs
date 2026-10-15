using System.Globalization;
using System.Text.Json;
using BuildingBlocks.Domain.Campaigns;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using Integrations.Application.Options;
using Integrations.Domain.Campaigns.Models;
using Integrations.Domain.Campaigns.Resilience;
using Integrations.Domain.Campaigns.Sync;
using Integrations.Infrastructure.Meta;
using Microsoft.Extensions.Options;

namespace Integrations.Infrastructure.Campaigns.Sync;

/// <summary>Ingestão diária paginada sem somar ações de tipos diferentes como conversões.</summary>
public sealed class MetaAdsMetricsSyncAdapter : ICampaignMetricsSyncAdapter
{
    private readonly MetaGraphClient _graph;

    /// <summary>Configura o adaptador de métricas com transporte seguro da Meta.</summary>
    /// <param name="httpClient">Cliente HTTP.</param>
    /// <param name="rateLimitPolicy">Contrato existente; retries são tratados por página no transporte Meta.</param>
    /// <param name="options">Configurações da versão Graph e do aplicativo.</param>
    public MetaAdsMetricsSyncAdapter(HttpClient httpClient, IHierarchyRateLimitPolicy rateLimitPolicy, IOptions<MetaAdsOAuthOptions> options)
        => _graph = new MetaGraphClient(httpClient, options.Value);

    /// <inheritdoc />
    public string Platform => "MetaAds";

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<UnifiedMetricItem>>> FetchMetricsAsync(ConnectedAdAccount account,
        string? decryptedAccessToken, DateTime startDateUtc, DateTime endDateUtc, MetricGranularity granularity,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(decryptedAccessToken))
            return Result<IReadOnlyList<UnifiedMetricItem>>.Failure(Error.Unauthorized("MetaAds.EmptyAccessToken", "Reconecte a autorização Meta."));
        if (granularity != MetricGranularity.Daily)
            return Result<IReadOnlyList<UnifiedMetricItem>>.Failure(Error.Validation("Meta.UnsupportedGranularity", "A sincronização Meta suporta granularidade diária; hourly não é um valor válido de time_increment."));
        if (endDateUtc.Date < startDateUtc.Date || (endDateUtc.Date - startDateUtc.Date).TotalDays > 366)
            return Result<IReadOnlyList<UnifiedMetricItem>>.Failure(Error.Validation("Meta.InvalidDateRange", "Selecione um intervalo de até 366 dias."));
        var id = account.ExternalAccountId.StartsWith("act_", StringComparison.Ordinal) ? account.ExternalAccountId : $"act_{account.ExternalAccountId}";
        if (id.Length < 5 || !id[4..].All(char.IsAsciiDigit))
            return Result<IReadOnlyList<UnifiedMetricItem>>.Failure(Error.Validation("Meta.InvalidAccountId", "ID de conta Meta inválido."));
        var range = Uri.EscapeDataString(JsonSerializer.Serialize(new { since = startDateUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), until = endDateUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) }));
        var response = await _graph.ReadCollectionAsync($"{id}/insights?level=campaign&time_increment=1&time_range={range}&use_unified_attribution_setting=true&fields=campaign_id,date_start,date_stop,spend,impressions,clicks&limit=100", decryptedAccessToken, cancellationToken);
        if (response.IsFailure) return Result<IReadOnlyList<UnifiedMetricItem>>.Failure(response.Error);
        var items = new List<UnifiedMetricItem>();
        foreach (var row in response.Value)
        {
            var campaignId = row.TryGetProperty("campaign_id", out var cid) ? cid.GetString() : null;
            var dateText = row.TryGetProperty("date_start", out var dateValue) ? dateValue.GetString() : null;
            if (string.IsNullOrWhiteSpace(campaignId) || !DateTime.TryParseExact(dateText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ||
                !TryDecimal(row, "spend", out var spend) || !TryDecimal(row, "impressions", out var impressions) || !TryDecimal(row, "clicks", out var clicks))
                return Result<IReadOnlyList<UnifiedMetricItem>>.Failure(Error.Failure("Meta.InvalidMetric", "A Meta retornou uma métrica sem identificação, data ou valores válidos."));
            // O modelo unificado não define evento de conversão: não inferir compras a partir de actions.
            // A consulta detalhada de Insights deve definir explicitamente a ação para CPA/ROAS.
            items.Add(new(campaignId, null, null, DateTime.SpecifyKind(date.Date, DateTimeKind.Utc), null,
                spend, (long)impressions, (long)clicks, 0m, 0m, account.Currency));
        }
        return Result<IReadOnlyList<UnifiedMetricItem>>.Success(items);
    }

    private static bool TryDecimal(JsonElement row, string key, out decimal value)
    {
        value = 0;
        return row.TryGetProperty(key, out var field) && decimal.TryParse(field.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out value) && value >= 0;
    }
}
