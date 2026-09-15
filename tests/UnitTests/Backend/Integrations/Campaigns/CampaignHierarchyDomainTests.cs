using BuildingBlocks.Domain.Campaigns;
using FluentAssertions;

namespace UnitTests.Backend.Integrations.Campaigns;

/// <summary>
/// Testes unitários para as entidades do modelo universal de hierarquia de campanhas
/// (Campaign -> AdSet -> Ad).
/// </summary>
public sealed class CampaignHierarchyDomainTests
{
    /// <summary>
    /// Valida que uma campanha é criada com sucesso quando todos os dados obrigatórios são válidos.
    /// </summary>
    [Fact]
    public void CreateCampaign_WithValidData_ShouldSucceed()
    {
        // Arrange
        var id = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        // Act
        var result = Campaign.Create(
            id,
            workspaceId,
            accountId,
            "MetaAds",
            "cmp_123456",
            "Campanha Black Friday - Conversão",
            CampaignStatus.Active,
            "CONVERSIONS",
            dailyBudget: 250.00m,
            lifetimeBudget: null,
            currency: "BRL",
            startDateUtc: DateTime.UtcNow.Date);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(id);
        result.Value.WorkspaceId.Should().Be(workspaceId);
        result.Value.ConnectedAdAccountId.Should().Be(accountId);
        result.Value.Platform.Should().Be("MetaAds");
        result.Value.ExternalCampaignId.Should().Be("cmp_123456");
        result.Value.Name.Should().Be("Campanha Black Friday - Conversão");
        result.Value.Status.Should().Be(CampaignStatus.Active);
        result.Value.DailyBudget.Should().Be(250.00m);
        result.Value.Currency.Should().Be("BRL");
    }

    /// <summary>
    /// Valida que a criação de campanha falha quando campos obrigatórios estão vazios ou nulos.
    /// </summary>
    /// <param name="platform">Plataforma de mídia.</param>
    /// <param name="name">Nome da campanha.</param>
    /// <param name="externalId">Identificador externo na rede.</param>
    [Theory]
    [InlineData("", "Nome Válido", "cmp_123")]
    [InlineData("   ", "Nome Válido", "cmp_123")]
    [InlineData("MetaAds", "", "cmp_123")]
    [InlineData("MetaAds", "Nome Válido", "")]
    public void CreateCampaign_WithInvalidRequiredFields_ShouldFail(string platform, string name, string externalId)
    {
        // Act
        var result = Campaign.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            platform,
            externalId,
            name,
            CampaignStatus.Active,
            "TRAFFIC");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().StartWith("Campaign.");
    }

    /// <summary>
    /// Valida que um conjunto de anúncios é criado com sucesso com parâmetros válidos.
    /// </summary>
    [Fact]
    public void CreateAdSet_WithValidData_ShouldSucceed()
    {
        // Arrange
        var id = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        // Act
        var result = AdSet.Create(
            id,
            campaignId,
            accountId,
            "adset_987654",
            "Público Lookalike 1% Compradores",
            AdSetStatus.Active,
            bidStrategy: "LOWEST_COST",
            optimizationGoal: "OFFSITE_CONVERSIONS",
            dailyBudget: 100.00m,
            targetingSummary: "{\"geo\": [\"BR\"], \"age\": [25, 45]}");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(id);
        result.Value.CampaignId.Should().Be(campaignId);
        result.Value.ConnectedAdAccountId.Should().Be(accountId);
        result.Value.ExternalAdSetId.Should().Be("adset_987654");
        result.Value.BidStrategy.Should().Be("LOWEST_COST");
    }

    /// <summary>
    /// Valida que a criação do conjunto falha quando a campanha pai é vazia.
    /// </summary>
    [Fact]
    public void CreateAdSet_WithEmptyCampaignId_ShouldFail()
    {
        // Act
        var result = AdSet.Create(
            Guid.NewGuid(),
            Guid.Empty,
            Guid.NewGuid(),
            "adset_123",
            "Conjunto Teste",
            AdSetStatus.Active);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AdSet.EmptyCampaignId");
    }

    /// <summary>
    /// Valida que um anúncio/criativo é criado com sucesso com todos os parâmetros válidos.
    /// </summary>
    [Fact]
    public void CreateAd_WithValidData_ShouldSucceed()
    {
        // Arrange
        var id = Guid.NewGuid();
        var adSetId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        // Act
        var result = Ad.Create(
            id,
            adSetId,
            campaignId,
            accountId,
            "ad_555666",
            "Anúncio Criativo Video 15s Oferta",
            AdStatus.Active,
            AdCreativeType.Video,
            headline: "Desconto Exclusivo de 30% na Semana",
            body: "Aproveite as últimas unidades disponíveis na loja.",
            destinationUrl: "https://www.lojaexemplo.com.br/promo-black",
            previewUrl: "https://cdn.lojaexemplo.com.br/thumb1.jpg",
            callToAction: "SHOP_NOW");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(id);
        result.Value.AdSetId.Should().Be(adSetId);
        result.Value.CampaignId.Should().Be(campaignId);
        result.Value.ConnectedAdAccountId.Should().Be(accountId);
        result.Value.ExternalAdId.Should().Be("ad_555666");
        result.Value.CreativeType.Should().Be(AdCreativeType.Video);
        result.Value.DestinationUrl.Should().Be("https://www.lojaexemplo.com.br/promo-black");
    }

    /// <summary>
    /// Valida que a criação de anúncio falha quando o ID externo está vazio.
    /// </summary>
    [Fact]
    public void CreateAd_WithEmptyExternalId_ShouldFail()
    {
        // Act
        var result = Ad.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "",
            "Anúncio Sem ID",
            AdStatus.Active,
            AdCreativeType.Image);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Ad.EmptyExternalAdId");
    }

    /// <summary>
    /// Valida a atualização cadastral e recálculo de carimbo de sincronização da campanha.
    /// </summary>
    [Fact]
    public void Campaign_UpdateDetails_ShouldModifyFieldsAndTimestamp()
    {
        // Arrange
        var campaign = Campaign.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "GoogleAds",
            "google_cmp_100",
            "Search - Palavras-chave Institucionais",
            CampaignStatus.Active,
            "SEARCH",
            dailyBudget: 50.00m).Value;

        var syncTime = DateTime.UtcNow.AddMinutes(5);

        // Act
        var updateResult = campaign.UpdateDetails(
            "Search - Institucional (Otimizada)",
            CampaignStatus.Paused,
            "SEARCH",
            dailyBudget: 75.00m,
            lifetimeBudget: null,
            currency: "BRL",
            startDateUtc: null,
            endDateUtc: null,
            lastSyncedAtUtc: syncTime);

        // Assert
        updateResult.IsSuccess.Should().BeTrue();
        campaign.Name.Should().Be("Search - Institucional (Otimizada)");
        campaign.Status.Should().Be(CampaignStatus.Paused);
        campaign.DailyBudget.Should().Be(75.00m);
        campaign.LastSyncedAtUtc.Should().Be(syncTime);
        campaign.UpdatedAtUtc.Should().NotBeNull();
    }
}
