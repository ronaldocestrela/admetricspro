using BuildingBlocks.Domain.Campaigns;
using FluentAssertions;
using Integrations.Infrastructure.Campaigns.Mappers;

namespace UnitTests.Backend.Integrations.Campaigns;

/// <summary>
/// Testes unitários para o conversor de hierarquia da TikTok Marketing API v1.3.
/// </summary>
public sealed class TikTokAdsHierarchyMapperTests
{
    private readonly TikTokAdsHierarchyMapper _mapper = new();

    /// <summary>
    /// Valida o mapeamento de campanhas, grupos e anúncios da TikTok Marketing API v1.3.
    /// </summary>
    [Fact]
    public void Map_WithValidTikTokPayloads_ShouldProduceUnifiedHierarchy()
    {
        // Arrange
        var campaignsJson = """
        {
          "data": {
            "list": [
              {
                "campaign_id": "180123456789",
                "campaign_name": "TikTok Spark Ads - Viralização",
                "operation_status": "ENABLE",
                "objective_type": "TRAFFIC",
                "budget": 200.0,
                "budget_mode": "BUDGET_MODE_DAY"
              }
            ]
          }
        }
        """;

        var adGroupsJson = """
        {
          "data": {
            "list": [
              {
                "adgroup_id": "180987654321",
                "campaign_id": "180123456789",
                "adgroup_name": "Grupo Jovens 18-24 Anos",
                "operation_status": "ENABLE",
                "bid_type": "BID_TYPE_NO_BID",
                "optimize_goal": "CLICK",
                "budget": 200.0
              }
            ]
          }
        }
        """;

        var adsJson = """
        {
          "data": {
            "list": [
              {
                "ad_id": "180555444333",
                "adgroup_id": "180987654321",
                "campaign_id": "180123456789",
                "ad_name": "Vídeo Criador Influencer 01",
                "operation_status": "ENABLE",
                "ad_text": "Olha o que acabou de chegar! Use o cupom TIKTOK10",
                "landing_page_url": "https://www.tiktokpromo.com.br/compre",
                "call_to_action": "SHOP_NOW"
              }
            ]
          }
        }
        """;

        // Act
        var result = _mapper.Map(campaignsJson, adGroupsJson, adsJson, "BRL");

        // Assert
        result.IsSuccess.Should().BeTrue();
        var hierarchy = result.Value;

        hierarchy.Campaigns.Should().HaveCount(1);
        var campaign = hierarchy.Campaigns[0];
        campaign.ExternalCampaignId.Should().Be("180123456789");
        campaign.Name.Should().Be("TikTok Spark Ads - Viralização");
        campaign.Status.Should().Be(CampaignStatus.Active);
        campaign.DailyBudget.Should().Be(200.0m);

        hierarchy.AdSets.Should().HaveCount(1);
        var adSet = hierarchy.AdSets[0];
        adSet.ExternalAdSetId.Should().Be("180987654321");
        adSet.Status.Should().Be(AdSetStatus.Active);

        hierarchy.Ads.Should().HaveCount(1);
        var ad = hierarchy.Ads[0];
        ad.ExternalAdId.Should().Be("180555444333");
        ad.Status.Should().Be(AdStatus.Active);
        ad.CreativeType.Should().Be(AdCreativeType.Video);
        ad.Body.Should().Be("Olha o que acabou de chegar! Use o cupom TIKTOK10");
        ad.DestinationUrl.Should().Be("https://www.tiktokpromo.com.br/compre");
        ad.CallToAction.Should().Be("SHOP_NOW");
    }

    /// <summary>
    /// Valida que entradas vazias não causam exceção.
    /// </summary>
    [Fact]
    public void Map_WithEmptyInputs_ShouldReturnEmptyHierarchy()
    {
        // Act
        var result = _mapper.Map(null, null, null, "BRL");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Campaigns.Should().BeEmpty();
    }
}
