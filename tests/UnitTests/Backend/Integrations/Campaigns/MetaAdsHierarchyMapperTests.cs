using BuildingBlocks.Domain.Campaigns;
using FluentAssertions;
using Integrations.Infrastructure.Campaigns.Mappers;

namespace UnitTests.Backend.Integrations.Campaigns;

/// <summary>
/// Testes unitários para o conversor de hierarquia da Meta Ads Graph API v21.0.
/// </summary>
public sealed class MetaAdsHierarchyMapperTests
{
    private readonly MetaAdsHierarchyMapper _mapper = new();

    /// <summary>
    /// Valida que dados válidos retornados da Meta Graph API são mapeados com conversão correta de centavos e status.
    /// </summary>
    [Fact]
    public void Map_WithValidMetaPayloads_ShouldProduceUnifiedHierarchy()
    {
        // Arrange
        var campaignsJson = """
        {
          "data": [
            {
              "id": "23851029384",
              "name": "Campanha Conversão E-commerce",
              "status": "ACTIVE",
              "objective": "OUTCOME_SALES",
              "daily_budget": "15000",
              "start_time": "2026-03-01T00:00:00+0000"
            }
          ]
        }
        """;

        var adSetsJson = """
        {
          "data": [
            {
              "id": "23851029390",
              "campaign_id": "23851029384",
              "name": "Conjunto Lookalike 1%",
              "status": "ACTIVE",
              "bid_strategy": "LOWEST_COST_WITHOUT_CAP",
              "optimization_goal": "OFFSITE_CONVERSIONS",
              "daily_budget": "15000",
              "targeting": { "geo_locations": { "countries": ["BR"] } }
            }
          ]
        }
        """;

        var adsJson = """
        {
          "data": [
            {
              "id": "23851029395",
              "adset_id": "23851029390",
              "campaign_id": "23851029384",
              "name": "Criativo Oferta Especial - Imagem",
              "status": "ACTIVE",
              "creative": {
                "id": "cr_1001",
                "name": "Criativo Imagem Desconto",
                "title": "Super Desconto de 50%",
                "body": "Compre agora antes que acabe o estoque.",
                "image_url": "https://cdn.exemplo.com/ad1.jpg",
                "object_story_spec": {
                  "link_data": {
                    "link": "https://www.loja.com.br/promo",
                    "call_to_action": { "type": "SHOP_NOW" }
                  }
                }
              }
            }
          ]
        }
        """;

        // Act
        var result = _mapper.Map(campaignsJson, adSetsJson, adsJson, "BRL");

        // Assert
        result.IsSuccess.Should().BeTrue();
        var hierarchy = result.Value;

        hierarchy.Campaigns.Should().HaveCount(1);
        var campaign = hierarchy.Campaigns[0];
        campaign.ExternalCampaignId.Should().Be("23851029384");
        campaign.Name.Should().Be("Campanha Conversão E-commerce");
        campaign.Status.Should().Be(CampaignStatus.Active);
        campaign.DailyBudget.Should().Be(150.00m); // 15000 cents = 150.00 BRL
        campaign.Currency.Should().Be("BRL");

        hierarchy.AdSets.Should().HaveCount(1);
        var adSet = hierarchy.AdSets[0];
        adSet.ExternalAdSetId.Should().Be("23851029390");
        adSet.ExternalCampaignId.Should().Be("23851029384");
        adSet.Status.Should().Be(AdSetStatus.Active);
        adSet.DailyBudget.Should().Be(150.00m);

        hierarchy.Ads.Should().HaveCount(1);
        var ad = hierarchy.Ads[0];
        ad.ExternalAdId.Should().Be("23851029395");
        ad.ExternalAdSetId.Should().Be("23851029390");
        ad.Status.Should().Be(AdStatus.Active);
        ad.CreativeType.Should().Be(AdCreativeType.Image);
        ad.Headline.Should().Be("Super Desconto de 50%");
        ad.Body.Should().Be("Compre agora antes que acabe o estoque.");
        ad.DestinationUrl.Should().Be("https://www.loja.com.br/promo");
        ad.CallToAction.Should().Be("SHOP_NOW");
    }

    /// <summary>
    /// Valida que a passagem de strings vazias ou nulas não gera exceções e retorna estrutura vazia.
    /// </summary>
    [Fact]
    public void Map_WithEmptyInputs_ShouldReturnEmptyHierarchySuccessfully()
    {
        // Act
        var result = _mapper.Map(null, null, null, "BRL");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Campaigns.Should().BeEmpty();
        result.Value.AdSets.Should().BeEmpty();
        result.Value.Ads.Should().BeEmpty();
    }
}
