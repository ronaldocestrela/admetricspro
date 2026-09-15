using BuildingBlocks.Domain.Campaigns;
using FluentAssertions;
using Integrations.Infrastructure.Campaigns.Mappers;

namespace UnitTests.Backend.Integrations.Campaigns;

/// <summary>
/// Testes unitários para o conversor de hierarquia da Microsoft Advertising / Bing Ads API v13.
/// </summary>
public sealed class BingAdsHierarchyMapperTests
{
    private readonly BingAdsHierarchyMapper _mapper = new();

    /// <summary>
    /// Valida o mapeamento de campanhas, grupos e anúncios da Microsoft Advertising API.
    /// </summary>
    [Fact]
    public void Map_WithValidBingPayloads_ShouldProduceUnifiedHierarchy()
    {
        // Arrange
        var campaignsJson = """
        [
          {
            "Id": 601234567,
            "Name": "Bing Search - B2B Consultoria",
            "Status": "Active",
            "DailyBudget": 120.0,
            "BudgetType": "DailyBudgetStandard",
            "CampaignType": "Search"
          }
        ]
        """;

        var adGroupsJson = """
        [
          {
            "Id": 701234567,
            "CampaignId": 601234567,
            "Name": "Grupo Consultoria Estratégica",
            "Status": "Active",
            "StartDate": { "Year": 2026, "Month": 2, "Day": 1 }
          }
        ]
        """;

        var adsJson = """
        [
          {
            "Id": 801234567,
            "AdGroupId": 701234567,
            "Type": "ResponsiveSearch",
            "Status": "Active",
            "FinalUrls": [ "https://www.consultoriaexemplo.com.br" ],
            "Headlines": [ { "Text": "Consultoria Empresarial de Alto Impacto" } ],
            "Descriptions": [ { "Text": "Agende uma sessão diagnóstica com nossos especialistas." } ]
          }
        ]
        """;

        // Act
        var result = _mapper.Map(campaignsJson, adGroupsJson, adsJson, "BRL");

        // Assert
        result.IsSuccess.Should().BeTrue();
        var hierarchy = result.Value;

        hierarchy.Campaigns.Should().HaveCount(1);
        var campaign = hierarchy.Campaigns[0];
        campaign.ExternalCampaignId.Should().Be("601234567");
        campaign.Name.Should().Be("Bing Search - B2B Consultoria");
        campaign.Status.Should().Be(CampaignStatus.Active);
        campaign.DailyBudget.Should().Be(120.0m);

        hierarchy.AdSets.Should().HaveCount(1);
        var adSet = hierarchy.AdSets[0];
        adSet.ExternalAdSetId.Should().Be("701234567");
        adSet.Status.Should().Be(AdSetStatus.Active);

        hierarchy.Ads.Should().HaveCount(1);
        var ad = hierarchy.Ads[0];
        ad.ExternalAdId.Should().Be("801234567");
        ad.Status.Should().Be(AdStatus.Active);
        ad.CreativeType.Should().Be(AdCreativeType.ResponsiveSearch);
        ad.Headline.Should().Be("Consultoria Empresarial de Alto Impacto");
        ad.Body.Should().Be("Agende uma sessão diagnóstica com nossos especialistas.");
        ad.DestinationUrl.Should().Be("https://www.consultoriaexemplo.com.br");
    }

    /// <summary>
    /// Valida que entradas vazias não geram exceções.
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
