using System.Text.Json;
using BuildingBlocks.Domain.Campaigns;
using BuildingBlocks.Domain.Primitives;
using Integrations.Domain.Campaigns.Mappers;
using Integrations.Domain.Campaigns.Models;

namespace Integrations.Infrastructure.Campaigns.Mappers;

/// <summary>
/// Implementação concreta do conversor de hierarquia para a TikTok Marketing API v1.3.
/// </summary>
public sealed class TikTokAdsHierarchyMapper : ITikTokAdsHierarchyMapper
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
            if (!doc.RootElement.TryGetProperty("data", out var data) ||
                !data.TryGetProperty("list", out var items) ||
                items.ValueKind != JsonValueKind.Array)
            {
                return list;
            }

            foreach (var elem in items.EnumerateArray())
            {
                var id = elem.TryGetProperty("campaign_id", out var pId) ? pId.GetString() : null;
                var name = elem.TryGetProperty("campaign_name", out var pName) ? pName.GetString() : null;
                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name)) continue;

                var statusStr = elem.TryGetProperty("operation_status", out var pStat) ? pStat.GetString() : null;
                var objective = elem.TryGetProperty("objective_type", out var pObj) ? pObj.GetString() ?? string.Empty : string.Empty;

                decimal? budget = null;
                if (elem.TryGetProperty("budget", out var pBudget))
                {
                    if (pBudget.ValueKind == JsonValueKind.Number && pBudget.TryGetDecimal(out var bVal))
                        budget = bVal;
                    else if (decimal.TryParse(pBudget.GetString(), out var bStrVal))
                        budget = bStrVal;
                }

                list.Add(new UnifiedCampaignItem(
                    id,
                    name,
                    MapTikTokStatusToCampaign(statusStr),
                    objective,
                    budget,
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
            if (!doc.RootElement.TryGetProperty("data", out var data) ||
                !data.TryGetProperty("list", out var items) ||
                items.ValueKind != JsonValueKind.Array)
            {
                return list;
            }

            foreach (var elem in items.EnumerateArray())
            {
                var id = elem.TryGetProperty("adgroup_id", out var pId) ? pId.GetString() : null;
                var cmpId = elem.TryGetProperty("campaign_id", out var pCmpId) ? pCmpId.GetString() : null;
                var name = elem.TryGetProperty("adgroup_name", out var pName) ? pName.GetString() : null;
                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(cmpId) || string.IsNullOrWhiteSpace(name)) continue;

                var statusStr = elem.TryGetProperty("operation_status", out var pStat) ? pStat.GetString() : null;
                var bidType = elem.TryGetProperty("bid_type", out var pBid) ? pBid.GetString() : null;
                var optGoal = elem.TryGetProperty("optimize_goal", out var pGoal) ? pGoal.GetString() : null;

                decimal? budget = null;
                if (elem.TryGetProperty("budget", out var pBudget))
                {
                    if (pBudget.ValueKind == JsonValueKind.Number && pBudget.TryGetDecimal(out var bVal))
                        budget = bVal;
                    else if (decimal.TryParse(pBudget.GetString(), out var bStrVal))
                        budget = bStrVal;
                }

                list.Add(new UnifiedAdSetItem(
                    id,
                    cmpId,
                    name,
                    MapTikTokStatusToAdSet(statusStr),
                    bidType,
                    optGoal,
                    budget,
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
            if (!doc.RootElement.TryGetProperty("data", out var data) ||
                !data.TryGetProperty("list", out var items) ||
                items.ValueKind != JsonValueKind.Array)
            {
                return list;
            }

            foreach (var elem in items.EnumerateArray())
            {
                var id = elem.TryGetProperty("ad_id", out var pId) ? pId.GetString() : null;
                var groupId = elem.TryGetProperty("adgroup_id", out var pGId) ? pGId.GetString() : null;
                var cmpId = elem.TryGetProperty("campaign_id", out var pCmpId) ? pCmpId.GetString() ?? string.Empty : string.Empty;
                var name = elem.TryGetProperty("ad_name", out var pName) ? pName.GetString() : null;
                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(groupId) || string.IsNullOrWhiteSpace(name)) continue;

                var statusStr = elem.TryGetProperty("operation_status", out var pStat) ? pStat.GetString() : null;
                var adText = elem.TryGetProperty("ad_text", out var pTxt) ? pTxt.GetString() : null;
                var landingUrl = elem.TryGetProperty("landing_page_url", out var pUrl) ? pUrl.GetString() : null;
                var cta = elem.TryGetProperty("call_to_action", out var pCta) ? pCta.GetString() : null;

                list.Add(new UnifiedAdItem(
                    id,
                    groupId,
                    cmpId,
                    name,
                    MapTikTokStatusToAd(statusStr),
                    AdCreativeType.Video,
                    null,
                    adText,
                    landingUrl,
                    null,
                    cta));
            }
        }
        catch { }

        return list;
    }

    private static CampaignStatus MapTikTokStatusToCampaign(string? status) =>
        status?.ToUpperInvariant() switch
        {
            "ENABLE" => CampaignStatus.Active,
            "DISABLE" => CampaignStatus.Paused,
            "DELETE" => CampaignStatus.Deleted,
            _ => CampaignStatus.Unknown
        };

    private static AdSetStatus MapTikTokStatusToAdSet(string? status) =>
        status?.ToUpperInvariant() switch
        {
            "ENABLE" => AdSetStatus.Active,
            "DISABLE" => AdSetStatus.Paused,
            "DELETE" => AdSetStatus.Deleted,
            _ => AdSetStatus.Unknown
        };

    private static AdStatus MapTikTokStatusToAd(string? status) =>
        status?.ToUpperInvariant() switch
        {
            "ENABLE" => AdStatus.Active,
            "DISABLE" => AdStatus.Paused,
            "DELETE" => AdStatus.Deleted,
            _ => AdStatus.Unknown
        };
}
