using System.Text.Json;
using BuildingBlocks.Domain.Campaigns;
using BuildingBlocks.Domain.Primitives;
using Integrations.Domain.Campaigns.Mappers;
using Integrations.Domain.Campaigns.Models;

namespace Integrations.Infrastructure.Campaigns.Mappers;

/// <summary>
/// Implementação concreta do conversor de hierarquia para a Google Ads API (searchStream).
/// </summary>
public sealed class GoogleAdsHierarchyMapper : IGoogleAdsHierarchyMapper
{
    /// <inheritdoc />
    public Result<UnifiedCampaignHierarchy> Map(
        string? googleRowsJson,
        string defaultCurrency = "BRL")
    {
        var campaignsDict = new Dictionary<string, UnifiedCampaignItem>(StringComparer.OrdinalIgnoreCase);
        var adSetsDict = new Dictionary<string, UnifiedAdSetItem>(StringComparer.OrdinalIgnoreCase);
        var adsList = new List<UnifiedAdItem>();

        if (string.IsNullOrWhiteSpace(googleRowsJson))
        {
            return Result<UnifiedCampaignHierarchy>.Success(
                new UnifiedCampaignHierarchy(Array.Empty<UnifiedCampaignItem>(), Array.Empty<UnifiedAdSetItem>(), Array.Empty<UnifiedAdItem>()));
        }

        try
        {
            using var doc = JsonDocument.Parse(googleRowsJson);
            var root = doc.RootElement;
            var rows = root.ValueKind == JsonValueKind.Array ? root.EnumerateArray() : Enumerable.Empty<JsonElement>();

            foreach (var row in rows)
            {
                string? campaignId = null;

                // 1. Process Campaign
                if (row.TryGetProperty("campaign", out var cmpElem))
                {
                    campaignId = cmpElem.TryGetProperty("id", out var pCmpId) ? pCmpId.GetString() : null;
                    var cmpName = cmpElem.TryGetProperty("name", out var pCmpName) ? pCmpName.GetString() : null;
                    var cmpStatus = cmpElem.TryGetProperty("status", out var pCmpStatus) ? pCmpStatus.GetString() : null;
                    var channelType = cmpElem.TryGetProperty("advertisingChannelType", out var pChan) ? pChan.GetString() ?? string.Empty : string.Empty;

                    decimal? dailyBudget = null;
                    if (row.TryGetProperty("campaignBudget", out var bgtElem) &&
                        bgtElem.TryGetProperty("amountMicros", out var pMicros) &&
                        long.TryParse(pMicros.GetString(), out var microsVal))
                    {
                        dailyBudget = microsVal / 1_000_000m;
                    }

                    if (!string.IsNullOrWhiteSpace(campaignId) && !string.IsNullOrWhiteSpace(cmpName) && !campaignsDict.ContainsKey(campaignId))
                    {
                        campaignsDict[campaignId] = new UnifiedCampaignItem(
                            campaignId,
                            cmpName,
                            MapGoogleStatusToCampaignStatus(cmpStatus),
                            channelType,
                            dailyBudget,
                            null,
                            defaultCurrency,
                            null,
                            null);
                    }
                }

                // 2. Process AdGroup
                string? adGroupId = null;
                if (row.TryGetProperty("adGroup", out var agElem))
                {
                    adGroupId = agElem.TryGetProperty("id", out var pAgId) ? pAgId.GetString() : null;
                    var agName = agElem.TryGetProperty("name", out var pAgName) ? pAgName.GetString() : null;
                    var agStatus = agElem.TryGetProperty("status", out var pAgStatus) ? pAgStatus.GetString() : null;

                    if (!string.IsNullOrWhiteSpace(adGroupId) && !string.IsNullOrWhiteSpace(campaignId) && !string.IsNullOrWhiteSpace(agName) && !adSetsDict.ContainsKey(adGroupId))
                    {
                        adSetsDict[adGroupId] = new UnifiedAdSetItem(
                            adGroupId,
                            campaignId,
                            agName,
                            MapGoogleStatusToAdSetStatus(agStatus),
                            null,
                            null,
                            null,
                            null,
                            null,
                            null,
                            null);
                    }
                }

                // 3. Process AdGroupAd
                if (row.TryGetProperty("adGroupAd", out var agaElem) && agaElem.TryGetProperty("ad", out var adElem))
                {
                    var adId = adElem.TryGetProperty("id", out var pAdId) ? pAdId.GetString() : null;
                    var adName = adElem.TryGetProperty("name", out var pAdName) ? pAdName.GetString() ?? $"Ad {adId}" : $"Ad {adId}";
                    var adStatus = agaElem.TryGetProperty("status", out var pAdStatus) ? pAdStatus.GetString() : null;
                    var adTypeStr = adElem.TryGetProperty("type", out var pType) ? pType.GetString() : null;

                    string? headline = null;
                    string? body = null;
                    string? destinationUrl = null;

                    if (adElem.TryGetProperty("finalUrls", out var fUrls) && fUrls.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var u in fUrls.EnumerateArray())
                        {
                            destinationUrl = u.GetString();
                            if (!string.IsNullOrWhiteSpace(destinationUrl))
                            {
                                break;
                            }
                        }
                    }

                    if (adElem.TryGetProperty("responsiveSearchAd", out var rsa))
                    {
                        if (rsa.TryGetProperty("headlines", out var hList) && hList.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var h in hList.EnumerateArray())
                            {
                                if (h.TryGetProperty("text", out var t))
                                {
                                    headline = t.GetString();
                                    break;
                                }
                            }
                        }

                        if (rsa.TryGetProperty("descriptions", out var dList) && dList.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var d in dList.EnumerateArray())
                            {
                                if (d.TryGetProperty("text", out var dt))
                                {
                                    body = dt.GetString();
                                    break;
                                }
                            }
                        }
                    }

                    var creativeType = adTypeStr switch
                    {
                        "RESPONSIVE_SEARCH_AD" => AdCreativeType.ResponsiveSearch,
                        "IMAGE_AD" => AdCreativeType.Image,
                        "VIDEO_AD" => AdCreativeType.Video,
                        _ => AdCreativeType.Text
                    };

                    if (!string.IsNullOrWhiteSpace(adId) && !string.IsNullOrWhiteSpace(adGroupId))
                    {
                        adsList.Add(new UnifiedAdItem(
                            adId,
                            adGroupId,
                            campaignId ?? string.Empty,
                            adName,
                            MapGoogleStatusToAdStatus(adStatus),
                            creativeType,
                            headline,
                            body,
                            destinationUrl,
                            null,
                            null));
                    }
                }
            }
        }
        catch
        {
            // Tratamento resiliente
        }

        return Result<UnifiedCampaignHierarchy>.Success(
            new UnifiedCampaignHierarchy(
                campaignsDict.Values.ToList(),
                adSetsDict.Values.ToList(),
                adsList));
    }

    private static CampaignStatus MapGoogleStatusToCampaignStatus(string? status) =>
        status?.ToUpperInvariant() switch
        {
            "ENABLED" => CampaignStatus.Active,
            "PAUSED" => CampaignStatus.Paused,
            "REMOVED" => CampaignStatus.Deleted,
            _ => CampaignStatus.Unknown
        };

    private static AdSetStatus MapGoogleStatusToAdSetStatus(string? status) =>
        status?.ToUpperInvariant() switch
        {
            "ENABLED" => AdSetStatus.Active,
            "PAUSED" => AdSetStatus.Paused,
            "REMOVED" => AdSetStatus.Deleted,
            _ => AdSetStatus.Unknown
        };

    private static AdStatus MapGoogleStatusToAdStatus(string? status) =>
        status?.ToUpperInvariant() switch
        {
            "ENABLED" => AdStatus.Active,
            "PAUSED" => AdStatus.Paused,
            "REMOVED" => AdStatus.Deleted,
            _ => AdStatus.Unknown
        };
}
