using System.Text.Json;
using BuildingBlocks.Domain.Campaigns;
using BuildingBlocks.Domain.Primitives;
using Integrations.Domain.Campaigns.Mappers;
using Integrations.Domain.Campaigns.Models;

namespace Integrations.Infrastructure.Campaigns.Mappers;

/// <summary>
/// Implementação concreta do conversor de hierarquia para a Meta Ads Graph API v21.0.
/// </summary>
public sealed class MetaAdsHierarchyMapper : IMetaAdsHierarchyMapper
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <inheritdoc />
    public Result<UnifiedCampaignHierarchy> Map(
        string? campaignsJson,
        string? adSetsJson,
        string? adsJson,
        string defaultCurrency = "BRL")
    {
        var campaigns = ParseCampaigns(campaignsJson, defaultCurrency);
        var adSets = ParseAdSets(adSetsJson);
        var ads = ParseAds(adsJson);

        return Result<UnifiedCampaignHierarchy>.Success(
            new UnifiedCampaignHierarchy(campaigns, adSets, ads));
    }

    private static List<UnifiedCampaignItem> ParseCampaigns(string? json, string currency)
    {
        var list = new List<UnifiedCampaignItem>();
        if (string.IsNullOrWhiteSpace(json))
        {
            return list;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            {
                return list;
            }

            foreach (var elem in data.EnumerateArray())
            {
                var id = elem.TryGetProperty("id", out var pId) ? pId.GetString() : null;
                var name = elem.TryGetProperty("name", out var pName) ? pName.GetString() : null;
                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var statusStr = elem.TryGetProperty("status", out var pStatus) ? pStatus.GetString() : null;
                var objective = elem.TryGetProperty("objective", out var pObj) ? pObj.GetString() ?? string.Empty : string.Empty;

                decimal? dailyBudget = null;
                if (elem.TryGetProperty("daily_budget", out var pBudget) && decimal.TryParse(pBudget.GetString(), out var budgetCents))
                {
                    dailyBudget = budgetCents / 100m;
                }

                decimal? lifetimeBudget = null;
                if (elem.TryGetProperty("lifetime_budget", out var pLtBudget) && decimal.TryParse(pLtBudget.GetString(), out var ltBudgetCents))
                {
                    lifetimeBudget = ltBudgetCents / 100m;
                }

                DateTime? startDate = null;
                if (elem.TryGetProperty("start_time", out var pStart) && DateTime.TryParse(pStart.GetString(), out var dtStart))
                {
                    startDate = dtStart.ToUniversalTime();
                }

                DateTime? endDate = null;
                if (elem.TryGetProperty("stop_time", out var pEnd) && DateTime.TryParse(pEnd.GetString(), out var dtEnd))
                {
                    endDate = dtEnd.ToUniversalTime();
                }

                list.Add(new UnifiedCampaignItem(
                    id,
                    name,
                    MapCampaignStatus(statusStr),
                    objective,
                    dailyBudget,
                    lifetimeBudget,
                    currency,
                    startDate,
                    endDate));
            }
        }
        catch
        {
            // Erros de parsing retornam lista parcial ou vazia
        }

        return list;
    }

    private static List<UnifiedAdSetItem> ParseAdSets(string? json)
    {
        var list = new List<UnifiedAdSetItem>();
        if (string.IsNullOrWhiteSpace(json))
        {
            return list;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            {
                return list;
            }

            foreach (var elem in data.EnumerateArray())
            {
                var id = elem.TryGetProperty("id", out var pId) ? pId.GetString() : null;
                var campaignId = elem.TryGetProperty("campaign_id", out var pCmpId) ? pCmpId.GetString() : null;
                var name = elem.TryGetProperty("name", out var pName) ? pName.GetString() : null;
                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(campaignId) || string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var statusStr = elem.TryGetProperty("status", out var pStatus) ? pStatus.GetString() : null;
                var bidStrategy = elem.TryGetProperty("bid_strategy", out var pBid) ? pBid.GetString() : null;
                var optGoal = elem.TryGetProperty("optimization_goal", out var pOpt) ? pOpt.GetString() : null;

                decimal? dailyBudget = null;
                if (elem.TryGetProperty("daily_budget", out var pBudget) && decimal.TryParse(pBudget.GetString(), out var budgetCents))
                {
                    dailyBudget = budgetCents / 100m;
                }

                decimal? lifetimeBudget = null;
                if (elem.TryGetProperty("lifetime_budget", out var pLtBudget) && decimal.TryParse(pLtBudget.GetString(), out var ltBudgetCents))
                {
                    lifetimeBudget = ltBudgetCents / 100m;
                }

                string? targeting = null;
                if (elem.TryGetProperty("targeting", out var pTargeting))
                {
                    targeting = pTargeting.GetRawText();
                }

                list.Add(new UnifiedAdSetItem(
                    id,
                    campaignId,
                    name,
                    MapAdSetStatus(statusStr),
                    bidStrategy,
                    optGoal,
                    dailyBudget,
                    lifetimeBudget,
                    targeting,
                    null,
                    null));
            }
        }
        catch
        {
            // Tratamento silencioso
        }

        return list;
    }

    private static List<UnifiedAdItem> ParseAds(string? json)
    {
        var list = new List<UnifiedAdItem>();
        if (string.IsNullOrWhiteSpace(json))
        {
            return list;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            {
                return list;
            }

            foreach (var elem in data.EnumerateArray())
            {
                var id = elem.TryGetProperty("id", out var pId) ? pId.GetString() : null;
                var adSetId = elem.TryGetProperty("adset_id", out var pSetId) ? pSetId.GetString() : null;
                var campaignId = elem.TryGetProperty("campaign_id", out var pCmpId) ? pCmpId.GetString() ?? string.Empty : string.Empty;
                var name = elem.TryGetProperty("name", out var pName) ? pName.GetString() : null;
                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(adSetId) || string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var statusStr = elem.TryGetProperty("status", out var pStatus) ? pStatus.GetString() : null;
                string? headline = null;
                string? body = null;
                string? destinationUrl = null;
                string? previewUrl = null;
                string? callToAction = null;
                var creativeType = AdCreativeType.Image;

                if (elem.TryGetProperty("creative", out var cr))
                {
                    headline = cr.TryGetProperty("title", out var pTitle) ? pTitle.GetString() : null;
                    body = cr.TryGetProperty("body", out var pBody) ? pBody.GetString() : null;
                    previewUrl = cr.TryGetProperty("image_url", out var pImg) ? pImg.GetString() : null;

                    if (cr.TryGetProperty("object_story_spec", out var spec) &&
                        spec.TryGetProperty("link_data", out var linkData))
                    {
                        destinationUrl = linkData.TryGetProperty("link", out var pLink) ? pLink.GetString() : null;
                        if (linkData.TryGetProperty("call_to_action", out var cta) &&
                            cta.TryGetProperty("type", out var pType))
                        {
                            callToAction = pType.GetString();
                        }
                    }
                }

                list.Add(new UnifiedAdItem(
                    id,
                    adSetId,
                    campaignId,
                    name,
                    MapAdStatus(statusStr),
                    creativeType,
                    headline,
                    body,
                    destinationUrl,
                    previewUrl,
                    callToAction));
            }
        }
        catch
        {
            // Tratamento silencioso
        }

        return list;
    }

    private static CampaignStatus MapCampaignStatus(string? status) =>
        status?.ToUpperInvariant() switch
        {
            "ACTIVE" => CampaignStatus.Active,
            "PAUSED" => CampaignStatus.Paused,
            "ARCHIVED" => CampaignStatus.Archived,
            "DELETED" => CampaignStatus.Deleted,
            _ => CampaignStatus.Unknown
        };

    private static AdSetStatus MapAdSetStatus(string? status) =>
        status?.ToUpperInvariant() switch
        {
            "ACTIVE" => AdSetStatus.Active,
            "PAUSED" => AdSetStatus.Paused,
            "ARCHIVED" => AdSetStatus.Archived,
            "DELETED" => AdSetStatus.Deleted,
            _ => AdSetStatus.Unknown
        };

    private static AdStatus MapAdStatus(string? status) =>
        status?.ToUpperInvariant() switch
        {
            "ACTIVE" => AdStatus.Active,
            "PAUSED" => AdStatus.Paused,
            "ARCHIVED" => AdStatus.Archived,
            "DELETED" => AdStatus.Deleted,
            "DISAPPROVED" => AdStatus.Disapproved,
            _ => AdStatus.Unknown
        };
}
