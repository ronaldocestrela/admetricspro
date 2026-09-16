using BuildingBlocks.Domain.Campaigns;
using FluentAssertions;

namespace UnitTests.Backend.Integrations.Campaigns;

/// <summary>
/// Testes unitários para a entidade de domínio <see cref="CampaignMetric"/>,
/// cobrindo criação, cálculos automáticos de métricas derivadas (CTR, CPC, CPM, CPA, ROAS),
/// validações de negócio e método de atualização idempotente.
/// </summary>
public sealed class CampaignMetricDomainTests
{
    /// <summary>
    /// Valida que uma métrica diária é criada com sucesso quando todos os dados são válidos.
    /// </summary>
    [Fact]
    public void Create_DailyMetric_WithValidData_ShouldSucceed()
    {
        // Arrange
        var id = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var date = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var result = CampaignMetric.Create(
            id,
            workspaceId,
            accountId,
            campaignId,
            adSetId: null,
            adId: null,
            platform: "MetaAds",
            externalCampaignId: "cmp_123",
            externalAdSetId: null,
            externalAdId: null,
            date: date,
            hour: null,
            granularity: MetricGranularity.Daily,
            spend: 500.00m,
            currency: "BRL",
            impressions: 25000,
            clicks: 1250,
            conversions: 50.0m,
            conversionValue: 2500.00m);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var metric = result.Value;
        metric.Id.Should().Be(id);
        metric.WorkspaceId.Should().Be(workspaceId);
        metric.ConnectedAdAccountId.Should().Be(accountId);
        metric.CampaignId.Should().Be(campaignId);
        metric.Platform.Should().Be("MetaAds");
        metric.ExternalCampaignId.Should().Be("cmp_123");
        metric.Date.Should().Be(date);
        metric.Hour.Should().BeNull();
        metric.Granularity.Should().Be(MetricGranularity.Daily);
        metric.Spend.Should().Be(500.00m);
        metric.Currency.Should().Be("BRL");
        metric.Impressions.Should().Be(25000);
        metric.Clicks.Should().Be(1250);
        metric.Conversions.Should().Be(50.0m);
        metric.ConversionValue.Should().Be(2500.00m);

        // Assert Derived KPIs:
        // CTR: (1250 / 25000) * 100 = 5.0%
        metric.Ctr.Should().Be(5.0m);
        // CPC: 500 / 1250 = 0.40
        metric.Cpc.Should().Be(0.40m);
        // CPM: (500 / 25000) * 1000 = 20.00
        metric.Cpm.Should().Be(20.00m);
        // CPA: 500 / 50 = 10.00
        metric.Cpa.Should().Be(10.00m);
        // ROAS: 2500 / 500 = 5.00
        metric.Roas.Should().Be(5.00m);
    }

    /// <summary>
    /// Valida que uma métrica horária é criada com sucesso quando a hora está entre 0 e 23.
    /// </summary>
    [Fact]
    public void Create_HourlyMetric_WithValidHour_ShouldSucceed()
    {
        // Arrange
        var id = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var date = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var result = CampaignMetric.Create(
            id,
            workspaceId,
            accountId,
            campaignId,
            adSetId: null,
            adId: null,
            platform: "GoogleAds",
            externalCampaignId: "cmp_google_99",
            externalAdSetId: null,
            externalAdId: null,
            date: date,
            hour: 14,
            granularity: MetricGranularity.Hourly,
            spend: 50.00m,
            currency: "USD",
            impressions: 1000,
            clicks: 40,
            conversions: 2.0m,
            conversionValue: 180.00m);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Hour.Should().Be(14);
        result.Value.Granularity.Should().Be(MetricGranularity.Hourly);
        result.Value.Spend.Should().Be(50.00m);
    }

    /// <summary>
    /// Valida que métrica horária sem hora especificada ou com hora fora de 0 a 23 é rejeitada.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData(-1)]
    [InlineData(24)]
    public void Create_HourlyMetric_WithInvalidHour_ShouldFail(int? hour)
    {
        // Arrange
        var date = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var result = CampaignMetric.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null,
            "TikTokAds",
            "cmp_tt",
            null,
            null,
            date,
            hour,
            MetricGranularity.Hourly,
            spend: 10m,
            currency: "BRL",
            impressions: 100,
            clicks: 5,
            conversions: 0,
            conversionValue: 0);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CampaignMetric.InvalidHour");
    }

    /// <summary>
    /// Valida que métricas financeiras ou de volume negativas são rejeitadas.
    /// </summary>
    [Theory]
    [InlineData(-10, 100, 10, 1)]
    [InlineData(10, -5, 10, 1)]
    [InlineData(10, 100, -2, 1)]
    [InlineData(10, 100, 10, -1)]
    public void Create_WithNegativeValues_ShouldFail(decimal spend, long impressions, long clicks, decimal conversions)
    {
        // Arrange
        var date = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var result = CampaignMetric.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null,
            "MetaAds",
            "cmp_1",
            null,
            null,
            date,
            null,
            MetricGranularity.Daily,
            spend,
            "BRL",
            impressions,
            clicks,
            conversions,
            0);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CampaignMetric.InvalidValues");
    }

    /// <summary>
    /// Valida que KPIs derivados retornam zero quando denominadores são nulos ou zerados (proteção contra divisão por zero).
    /// </summary>
    [Fact]
    public void DerivedKPIs_WhenDenominatorsAreZero_ShouldReturnZeroWithoutException()
    {
        // Arrange
        var date = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var result = CampaignMetric.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null,
            "BingAds",
            "cmp_bing",
            null,
            null,
            date,
            null,
            MetricGranularity.Daily,
            spend: 0m,
            currency: "BRL",
            impressions: 0,
            clicks: 0,
            conversions: 0m,
            conversionValue: 0m);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var metric = result.Value;
        metric.Ctr.Should().Be(0m);
        metric.Cpc.Should().Be(0m);
        metric.Cpm.Should().Be(0m);
        metric.Cpa.Should().Be(0m);
        metric.Roas.Should().Be(0m);
    }

    /// <summary>
    /// Valida que a atualização de métricas (<see cref="CampaignMetric.UpdateMetrics"/>) sobrescreve valores e recalcula KPIs.
    /// </summary>
    [Fact]
    public void UpdateMetrics_WithUpdatedData_ShouldUpdateValuesAndRecalculateKPIs()
    {
        // Arrange
        var date = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc);
        var metric = CampaignMetric.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null,
            "MetaAds",
            "cmp_1",
            null,
            null,
            date,
            null,
            MetricGranularity.Daily,
            spend: 100m,
            currency: "BRL",
            impressions: 1000,
            clicks: 50,
            conversions: 5m,
            conversionValue: 300m).Value;

        var syncTime = DateTime.UtcNow;

        // Act - Re-execução da ingestão com novos dados consolidados
        var updateResult = metric.UpdateMetrics(
            newSpend: 150m,
            newImpressions: 1500,
            newClicks: 75,
            newConversions: 8m,
            newConversionValue: 600m,
            syncTimeUtc: syncTime);

        // Assert
        updateResult.IsSuccess.Should().BeTrue();
        metric.Spend.Should().Be(150m);
        metric.Impressions.Should().Be(1500);
        metric.Clicks.Should().Be(75);
        metric.Conversions.Should().Be(8m);
        metric.ConversionValue.Should().Be(600m);
        metric.SyncedAtUtc.Should().Be(syncTime);
        metric.UpdatedAtUtc.Should().Be(syncTime);

        // Recalculated KPIs:
        metric.Ctr.Should().Be(5.0m);
        metric.Cpc.Should().Be(2.0m);
        metric.Roas.Should().Be(4.0m);
    }
}
