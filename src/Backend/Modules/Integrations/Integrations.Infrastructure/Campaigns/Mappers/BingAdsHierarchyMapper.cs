using System.Text.Json;
using BuildingBlocks.Domain.Campaigns;
using BuildingBlocks.Domain.Primitives;
using Integrations.Domain.Campaigns.Mappers;
using Integrations.Domain.Campaigns.Models;

namespace Integrations.Infrastructure.Campaigns.Mappers;

/// <summary>
/// Implementação concreta do conversor de hierarquia para a Microsoft Advertising / Bing Ads API v13.
/// </summary>
public sealed class BingAdsHierarchyMapper : IBingAdsHierarchyMapper
{
    /// <inheritdoc />
    public Result<UnifiedCampaignHierarchy> Map(
        string? campaignsJson,
        string? adGroupsJson,
        string? adsJson,
        string defaultCurrency = "BRL")
    {
        var campaigns = ParseCampaigns(campaignsJson, defaultCurrency);
        var adSets = ParseAdGroups(adGroupsJson);
        var ads = ParseAds(adsJson);

        return Result<UnifiedCampaignHierarchy>.Success(
            new UnifiedCampaignHierarchy(campaigns, adSets, ads));
    }

    private static List<UnifiedCampaignItem> ParseCampaigns(string? json, string currency)
    {
        var list = new List<UnifiedCampaignItem>();
        if (string.IsNullOrWhiteSpace(json)) return list;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var array = root.ValueKind == JsonValueKind.Array ? root.EnumerateArray() : Enumerable.Empty<JsonElement>();

            foreach (var elem in array)
            {
                string? id = null;
                if (elem.TryGetProperty("Id", out var pId))
                {
                    id = pId.ValueKind == JsonValueKind.Number ? pId.GetInt64().ToString() : pId.GetString();
                }

                var name = elem.TryGetProperty("Name", out var pName) ? pName.GetString() : null;
                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name)) continue;

                var statusStr = elem.TryGetProperty("Status", out var pStat) ? pStat.GetString() : null;
                var cmpType = elem.TryGetProperty("CampaignType", out var pType) ? pType.GetString() ?? "Search" : "Search";

                decimal? dailyBudget = null;
                if (elem.TryGetProperty("DailyBudget", out var pBudget))
                {
                    if (pBudget.ValueKind == JsonValueKind.Number && pBudget.TryGetDecimal(out var bVal))
                        dailyBudget = bVal;
                    else if (decimal.TryParse(pBudget.GetString(), out var bStrVal))
                        dailyBudget = bStrVal;
                }

                list.Add(new UnifiedCampaignItem(
                    id,
                    name,
                    MapBingStatusToCampaign(statusStr),
                    cmpType,
                    dailyBudget,
                    null,
                    currency,
                    null,
                    null));
            }
        }
        catch { }

        return list;
    }

    private static List<UnifiedAdSetItem> ParseAdGroups(string? json)
    {
        var list = new List<UnifiedAdSetItem>();
        if (string.IsNullOrWhiteSpace(json)) return list;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var array = root.ValueKind == JsonValueKind.Array ? root.EnumerateArray() : Enumerable.Empty<JsonElement>();

            foreach (var elem in array)
            {
                string? id = null;
                if (elem.TryGetProperty("Id", out var pId))
                {
                    id = pId.ValueKind == JsonValueKind.Number ? pId.GetInt64().ToString() : pId.GetString();
                }

                string? cmpId = null;
                if (elem.TryGetProperty("CampaignId", out var pCmpId))
                {
                    cmpId = pCmpId.ValueKind == JsonValueKind.Number ? pCmpId.GetInt64().ToString() : pCmpId.GetString();
                }

                var name = elem.TryGetProperty("Name", out var pName) ? pName.GetString() : null;
                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(cmpId) || string.IsNullOrWhiteSpace(name)) continue;

                var statusStr = elem.TryGetProperty("Status", out var pStat) ? pStat.GetString() : null;

                list.Add(new UnifiedAdSetItem(
                    id,
                    cmpId,
                    name,
                    MapBingStatusToAdSet(statusStr),
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null));
            }
        }
        catch { }

        return list;
    }

    private static List<UnifiedAdItem> ParseAds(string? json)
    {
        var list = new List<UnifiedAdItem>();
        if (string.IsNullOrWhiteSpace(json)) return list;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var array = root.ValueKind == JsonValueKind.Array ? root.EnumerateArray() : Enumerable.Empty<JsonElement>();

            foreach (var elem in array)
            {
                string? id = null;
                if (elem.TryGetProperty("Id", out var pId))
                {
                    id = pId.ValueKind == JsonValueKind.Number ? pId.GetInt64().ToString() : pId.GetString();
                }

                string? groupId = null;
                if (elem.TryGetProperty("AdGroupId", out var pGId))
                {
                    groupId = pGId.ValueKind == JsonValueKind.Number ? pGId.GetInt64().ToString() : pGId.GetString();
                }

                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(groupId)) continue;

                var statusStr = elem.TryGetProperty("Status", out var pStat) ? pStat.GetString() : null;
                var typeStr = elem.TryGetProperty("Type", out var pType) ? pType.GetString() : null;

                string? headline = null;
                if (elem.TryGetProperty("Headlines", out var hList) && hList.ValueKind == JsonValueKind.Array)
                {
                    foreach (var h in hList.EnumerateArray())
                    {
                        if (h.TryGetProperty("Text", out var t))
                        {
                            headline = t.GetString();
                            break;
                        }
                    }
                }

                string? body = null;
                if (elem.TryGetProperty("Descriptions", out var dList) && dList.ValueKind == JsonValueKind.Array)
                {
                    foreach (var d in dList.EnumerateArray())
                    {
                        if (d.TryGetProperty("Text", out var t))
                        {
                            body = t.GetString();
                            break;
                        }
                    }
                }

                string? destinationUrl = null;
                if (elem.TryGetProperty("FinalUrls", out var fList) && fList.ValueKind == JsonValueKind.Array)
                {
                    foreach (var f in fList.EnumerateArray())
                    {
                        destinationUrl = f.GetString();
                        break;
                    }
                }

                var creativeType = typeStr?.ToUpperInvariant() switch
                {
                    "RESPONSIVESEARCH" => AdCreativeType.ResponsiveSearch,
                    "IMAGE" => AdCreativeType.Image,
                    _ => AdCreativeType.Text
                };

                list.Add(new UnifiedAdItem(
                    id,
                    groupId,
                    string.Empty,
                    $"Bing Ad {id}",
                    MapBingStatusToAd(statusStr),
                    creativeType,
                    headline,
                    body,
                    destinationUrl,
                    null,
                    null));
            }
        }
        catch { }

        return list;
    }

    private static CampaignStatus MapBingStatusToCampaign(string? status) =>
        status?.ToUpperInvariant() switch
        {
            "ACTIVE" => CampaignStatus.Active,
            "PAUSED" => CampaignStatus.Paused,
            "DELETED" => CampaignStatus.Deleted,
            _ => CampaignStatus.Unknown
        };

    private static AdSetStatus MapBingStatusToAdSet(string? status) =>
        status?.ToUpperInvariant() switch
        {
            "ACTIVE" => AdSetStatus.Active,
            "PAUSED" => AdSetStatus.Paused,
            "DELETED" => AdSetStatus.Deleted,
            _ => AdSetStatus.Unknown
        };

    private static AdStatus MapBingStatusToAd(string? status) =>
        status?.ToUpperInvariant() switch
        {
            "ACTIVE" => AdStatus.Active,
            "PAUSED" => AdStatus.Paused,
            "DELETED" => AdStatus.Deleted,
            _ => AdStatus.Unknown
        };
}
