using System.Globalization;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using Integrations.Application.Options;
using Integrations.Domain.Meta;
using Microsoft.Extensions.Options;

namespace Integrations.Infrastructure.Meta;

/// <summary>Consulta Insights sem fabricar métricas ou agregar alcance/frequência.</summary>
/// <param name="client">Cliente HTTP configurado.</param>
/// <param name="options">Configurações validadas da operação.</param>
public sealed class MetaInsightsApi(HttpClient client, IOptions<MetaAdsOAuthOptions> options) : IMetaInsightsApi
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<MetaInsight>>> ReadAsync(string externalAccountId, string token, MetaInsightsFilter filter, CancellationToken cancellationToken)
    {
        if (!externalAccountId.StartsWith("act_", StringComparison.Ordinal) || externalAccountId.Length < 5 || !externalAccountId[4..].All(char.IsAsciiDigit) ||
            filter.Until < filter.Since || filter.Until.DayNumber - filter.Since.DayNumber > 366 ||
            filter.Level is not ("account" or "campaign" or "adset" or "ad") ||
            filter.Breakdown is not (null or "device_platform" or "publisher_platform" or "platform_position") ||
            new[] { filter.CampaignId, filter.AdSetId, filter.AdId }.Any(id => id is not null && (id.Length == 0 || !id.All(char.IsAsciiDigit))) ||
            filter.ConversionAction?.Length > 150)
            return Result<IReadOnlyList<MetaInsight>>.Failure(Error.Validation("Meta.InvalidInsightsFilter", "Filtros de Insights inválidos ou combinação de breakdown não suportada."));
        var range = Uri.EscapeDataString(JsonSerializer.Serialize(new { since = filter.Since.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), until = filter.Until.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) }));
        var fields = "account_id,campaign_id,adset_id,ad_id,date_start,date_stop,spend,impressions,reach,clicks,inline_link_clicks,cpc,cpm,ctr,frequency,actions,action_values,cost_per_action_type,video_play_actions,video_p100_watched_actions";
        var path = $"{externalAccountId}/insights?level={filter.Level}&time_increment=1&time_range={range}&use_unified_attribution_setting=true&fields={fields}&limit=100";
        if (filter.Breakdown is not null) path += "&breakdowns=" + filter.Breakdown;
        var filters = new List<object>();
        foreach (var (field, value) in new[] { ("campaign.id", filter.CampaignId), ("adset.id", filter.AdSetId), ("ad.id", filter.AdId) })
            if (value is not null) filters.Add(new { field, @operator = "IN", value = new[] { value } });
        if (filters.Count > 0) path += "&filtering=" + Uri.EscapeDataString(JsonSerializer.Serialize(filters));
        var response = await new MetaGraphClient(client, options.Value).ReadCollectionAsync(path, token, cancellationToken);
        if (response.IsFailure) return Result<IReadOnlyList<MetaInsight>>.Failure(response.Error);
        var result = new List<MetaInsight>();
        foreach (var row in response.Value)
        {
            if (!DateOnly.TryParseExact(Text(row, "date_start"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start) ||
                !DateOnly.TryParseExact(Text(row, "date_stop"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var stop))
                return Result<IReadOnlyList<MetaInsight>>.Failure(Error.Failure("Meta.InvalidResponse", "A Meta retornou datas de Insights inválidas."));
            var actions = Actions(row, "actions");
            var values = Actions(row, "action_values");
            var spend = Number(row, "spend");
            decimal? conversions = filter.ConversionAction is not null && actions.TryGetValue(filter.ConversionAction, out var count) ? count : null;
            decimal? revenue = filter.ConversionAction is not null && values.TryGetValue(filter.ConversionAction, out var value) ? value : null;
            result.Add(new()
            {
                DateStart = start, DateStop = stop, CampaignId = Text(row, "campaign_id"), AdSetId = Text(row, "adset_id"), AdId = Text(row, "ad_id"),
                Spend = spend, Impressions = Integer(row, "impressions"), Reach = Integer(row, "reach"), Clicks = Integer(row, "clicks"), LinkClicks = Integer(row, "inline_link_clicks"),
                Cpc = Number(row, "cpc"), Cpm = Number(row, "cpm"), Ctr = Number(row, "ctr"), Frequency = Number(row, "frequency"),
                ConversionAction = filter.ConversionAction, Conversions = conversions, ConversionValue = revenue,
                Cpa = conversions > 0 ? spend / conversions : null, Roas = spend > 0 && revenue.HasValue ? revenue / spend : null,
                Actions = actions, CostPerAction = Actions(row, "cost_per_action_type"),
                VideoMetrics = new Dictionary<string, decimal?> { ["video_play_actions"] = Video(row, "video_play_actions"), ["video_p100_watched_actions"] = Video(row, "video_p100_watched_actions") },
                Breakdown = filter.Breakdown, BreakdownValue = filter.Breakdown is not null ? Text(row, filter.Breakdown) : null
            });
        }
        return Result<IReadOnlyList<MetaInsight>>.Success(result);
    }

    private static string? Text(JsonElement row, string field) => row.TryGetProperty(field, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    private static decimal? Number(JsonElement row, string field) => row.TryGetProperty(field, out var value) && decimal.TryParse(value.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var n) ? n : null;
    private static long? Integer(JsonElement row, string field) => row.TryGetProperty(field, out var value) && long.TryParse(value.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out var n) ? n : null;
    private static Dictionary<string, decimal> Actions(JsonElement row, string field)
    {
        var result = new Dictionary<string, decimal>(StringComparer.Ordinal);
        if (row.TryGetProperty(field, out var entries) && entries.ValueKind == JsonValueKind.Array)
            foreach (var entry in entries.EnumerateArray())
                if (Text(entry, "action_type") is { } type && Number(entry, "value") is { } value) result[type] = value;
        return result;
    }
    private static decimal? Video(JsonElement row, string field)
    {
        var actions = Actions(row, field);
        return actions.TryGetValue("video_view", out var value) ? value : null;
    }
}
