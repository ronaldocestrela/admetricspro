using BuildingBlocks.Domain.Campaigns;
using FluentAssertions;
using Integrations.Infrastructure.Campaigns.Mappers;

namespace UnitTests.Backend.Integrations.Campaigns;

/// <summary>
/// Testes unitários para o conversor de hierarquia da Google Ads API.
/// </summary>
public sealed class GoogleAdsHierarchyMapperTests
{
    private readonly GoogleAdsHierarchyMapper _mapper = new();

    /// <summary>
    /// Valida que linhas da consulta GAQL do Google Ads são mapeadas com conversão de micros para unidades e extração de RSA.
    /// </summary>
    [Fact]
    public void Map_WithValidGoogleAdsRows_ShouldProduceUnifiedHierarchy()
    {
        // Arrange
        var rowsJson = """
        [
          {
            "campaign": {
              "resourceName": "customers/123/campaigns/999111",
              "id": "999111",
              "name": "Search - Leads Qualificados B2B",
              "status": "ENABLED",
              "advertisingChannelType": "SEARCH",
              "campaignBudget": "customers/123/campaignBudgets/888",
              "startDate": "2026-01-15"
            },
            "campaignBudget": {
              "amountMicros": "80000000"
            },
            "adGroup": {
              "resourceName": "customers/123/adGroups/777222",
              "id": "777222",
              "name": "Grupo - Software Gestão",
              "status": "ENABLED",
              "type": "SEARCH_STANDARD"
            },
            "adGroupAd": {
              "resourceName": "customers/123/adGroupAds/777222~555333",
              "ad": {
                "id": "555333",
                "name": "Anúncio Responsivo de Pesquisa V1",
                "type": "RESPONSIVE_SEARCH_AD",
                "finalUrls": [ "https://www.softwareexemplo.com.br/contato" ],
                "responsiveSearchAd": {
                  "headlines": [ { "text": "Melhor Sistema de Gestão" } ],
                  "descriptions": [ { "text": "Aumente a produtividade do seu time hoje." } ]
                }
              },
              "status": "ENABLED"
            }
          }
        ]
        """;

        // Act
        var result = _mapper.Map(rowsJson, "BRL");

        // Assert
        result.IsSuccess.Should().BeTrue();
        var hierarchy = result.Value;

        hierarchy.Campaigns.Should().HaveCount(1);
        var campaign = hierarchy.Campaigns[0];
        campaign.ExternalCampaignId.Should().Be("999111");
        campaign.Name.Should().Be("Search - Leads Qualificados B2B");
        campaign.Status.Should().Be(CampaignStatus.Active);
        campaign.DailyBudget.Should().Be(80.00m); // 80.000.000 micros = 80.00 BRL
        campaign.Objective.Should().Be("SEARCH");

        hierarchy.AdSets.Should().HaveCount(1);
        var adSet = hierarchy.AdSets[0];
        adSet.ExternalAdSetId.Should().Be("777222");
        adSet.ExternalCampaignId.Should().Be("999111");
        adSet.Status.Should().Be(AdSetStatus.Active);

        hierarchy.Ads.Should().HaveCount(1);
        var ad = hierarchy.Ads[0];
        ad.ExternalAdId.Should().Be("555333");
        ad.ExternalAdSetId.Should().Be("777222");
        ad.Status.Should().Be(AdStatus.Active);
        ad.CreativeType.Should().Be(AdCreativeType.ResponsiveSearch);
        ad.Headline.Should().Be("Melhor Sistema de Gestão");
        ad.Body.Should().Be("Aumente a produtividade do seu time hoje.");
        ad.DestinationUrl.Should().Be("https://www.softwareexemplo.com.br/contato");
    }

    /// <summary>
    /// Valida que input nulo ou vazio não lança exceção e resulta em hierarquia vazia.
    /// </summary>
    [Fact]
    public void Map_WithNullRows_ShouldReturnEmptyHierarchy()
    {
        // Act
        var result = _mapper.Map(null, "BRL");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Campaigns.Should().BeEmpty();
    }
}
