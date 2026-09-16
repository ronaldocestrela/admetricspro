using BuildingBlocks.Domain.Campaigns;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.Persistence;
using FluentAssertions;
using Integrations.Domain.Campaigns;
using Integrations.Infrastructure.Campaigns.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace UnitTests.Backend.Integrations.Campaigns;

/// <summary>
/// Testes unitários para o repositório de métricas analíticas de campanhas (<see cref="CampaignMetricsRepository"/>),
/// validando o ciclo TDD para idempotência estrita (item 2.3.1), upsert atômico em lote (item 2.3.2)
/// e consultas filtradas por período e granularidade.
/// </summary>
public sealed class CampaignMetricsRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TenantDbContext _dbContext;
    private readonly ITenantDbContextAccessor _contextAccessor;
    private readonly CampaignMetricsRepository _repository;

    /// <summary>
    /// Inicializa a base SQLite em memória com esquema completo do TenantDbContext.
    /// </summary>
    public CampaignMetricsRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new TenantDbContext(options);
        _dbContext.Database.EnsureCreated();

        _contextAccessor = Substitute.For<ITenantDbContextAccessor>();
        _contextAccessor.GetDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<TenantDbContext>.Success(_dbContext)));

        _repository = new CampaignMetricsRepository(_contextAccessor);
    }

    private async Task<Campaign> SeedCampaignAsync(
        Guid workspaceId,
        Guid accountId,
        string externalCampaignId,
        string platform = "MetaAds")
    {
        var campaign = Campaign.Create(
            Guid.NewGuid(),
            workspaceId,
            accountId,
            platform,
            externalCampaignId,
            "Campanha de Teste",
            CampaignStatus.Active,
            "CONVERSIONS",
            100m,
            null,
            "BRL",
            DateTime.UtcNow.AddDays(-30)).Value;

        await _dbContext.Campaigns.AddAsync(campaign);
        await _dbContext.SaveChangesAsync();

        return campaign;
    }

    /// <summary>
    /// Valida que a primeira ingestão de métricas diárias e horárias persiste com sucesso todos os registros.
    /// </summary>
    [Fact]
    public async Task UpsertMetricsBatchAsync_WhenNewMetrics_ShouldInsertAllRecords()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var campaign = await SeedCampaignAsync(workspaceId, accountId, "cmp_100");
        var date = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc);

        var metric1 = CampaignMetric.Create(
            Guid.NewGuid(),
            workspaceId,
            accountId,
            campaign.Id,
            null,
            null,
            "MetaAds",
            "cmp_100",
            null,
            null,
            date,
            null,
            MetricGranularity.Daily,
            spend: 300.00m,
            currency: "BRL",
            impressions: 10000,
            clicks: 500,
            conversions: 20m,
            conversionValue: 1200.00m).Value;

        var metric2 = CampaignMetric.Create(
            Guid.NewGuid(),
            workspaceId,
            accountId,
            campaign.Id,
            null,
            null,
            "MetaAds",
            "cmp_100",
            null,
            null,
            date,
            10,
            MetricGranularity.Hourly,
            spend: 50.00m,
            currency: "BRL",
            impressions: 1500,
            clicks: 80,
            conversions: 3m,
            conversionValue: 200.00m).Value;

        var batch = new List<CampaignMetric> { metric1, metric2 };

        // Act
        var result = await _repository.UpsertMetricsBatchAsync(workspaceId, batch);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2);

        var saved = await _dbContext.CampaignMetrics.ToListAsync();
        saved.Should().HaveCount(2);
        saved.Should().Contain(m => m.Granularity == MetricGranularity.Daily && m.Spend == 300.00m);
        saved.Should().Contain(m => m.Granularity == MetricGranularity.Hourly && m.Hour == 10 && m.Spend == 50.00m);
    }

    /// <summary>
    /// Validação estrita do item 2.3.1: re-execuções da sincronização de uma mesma data NÃO devem duplicar
    /// registros de métricas (Spend, Impressions, Clicks, Conversions), mas sim atualizar os valores existentes com integridade.
    /// </summary>
    [Fact]
    public async Task UpsertMetricsBatchAsync_WhenResyncingSameDate_ShouldBeStrictlyIdempotentAndNotDuplicate()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var campaign = await SeedCampaignAsync(workspaceId, accountId, "cmp_google_1", "GoogleAds");
        var date = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc);

        var initialMetric = CampaignMetric.Create(
            Guid.NewGuid(),
            workspaceId,
            accountId,
            campaign.Id,
            null,
            null,
            "GoogleAds",
            "cmp_google_1",
            null,
            null,
            date,
            null,
            MetricGranularity.Daily,
            spend: 200.00m,
            currency: "BRL",
            impressions: 5000,
            clicks: 250,
            conversions: 10m,
            conversionValue: 800.00m,
            syncedAtUtc: DateTime.UtcNow.AddHours(-3)).Value;

        // Ingestão inicial
        var initialResult = await _repository.UpsertMetricsBatchAsync(workspaceId, new[] { initialMetric });
        initialResult.IsSuccess.Should().BeTrue();
        (await _dbContext.CampaignMetrics.CountAsync()).Should().Be(1);

        // Act - Segunda ingestão simulada (reprocessamento de D-0 com conversões tardias atualizadas da rede)
        var updatedMetric = CampaignMetric.Create(
            Guid.NewGuid(), // Novo Guid transitório gerado no lote
            workspaceId,
            accountId,
            campaign.Id,
            null,
            null,
            "GoogleAds",
            "cmp_google_1",
            null,
            null,
            date,
            null,
            MetricGranularity.Daily,
            spend: 250.00m, // Aumento de spend finalizado
            currency: "BRL",
            impressions: 6200, // Mais impressões capturadas
            clicks: 310, // Mais cliques
            conversions: 15m, // Conversão tardia atribuída
            conversionValue: 1250.00m,
            syncedAtUtc: DateTime.UtcNow).Value;

        var resyncResult = await _repository.UpsertMetricsBatchAsync(workspaceId, new[] { updatedMetric });

        // Assert
        resyncResult.IsSuccess.Should().BeTrue();

        // Ponto Crítico de Idempotência: a contagem continua EXATAMENTE 1 registro, sem duplicatas!
        var allMetrics = await _dbContext.CampaignMetrics.ToListAsync();
        allMetrics.Should().HaveCount(1, "re-execução de sincronização na mesma data não pode duplicar linhas");

        var persisted = allMetrics.Single();
        persisted.Spend.Should().Be(250.00m);
        persisted.Impressions.Should().Be(6200);
        persisted.Clicks.Should().Be(310);
        persisted.Conversions.Should().Be(15m);
        persisted.ConversionValue.Should().Be(1250.00m);
        persisted.UpdatedAtUtc.Should().NotBeNull();

        // Validar que os KPIs derivados refletem os novos números
        persisted.Roas.Should().Be(5.00m); // 1250 / 250
    }

    /// <summary>
    /// Valida que a consulta filtrada por período e granularidade retorna apenas os registros correspondentes.
    /// </summary>
    [Fact]
    public async Task GetMetricsAsync_WithFilters_ShouldReturnFilteredResults()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var campaign1 = await SeedCampaignAsync(workspaceId, accountId, "c1", "MetaAds");
        var campaign2 = await SeedCampaignAsync(workspaceId, accountId, "c2", "MetaAds");

        var day1 = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var day2 = new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc);
        var day3 = new DateTime(2026, 9, 3, 0, 0, 0, DateTimeKind.Utc);

        var metrics = new List<CampaignMetric>
        {
            CampaignMetric.Create(Guid.NewGuid(), workspaceId, accountId, campaign1.Id, null, null, "MetaAds", "c1", null, null, day1, null, MetricGranularity.Daily, 100m, "BRL", 1000, 50, 2m, 200m).Value,
            CampaignMetric.Create(Guid.NewGuid(), workspaceId, accountId, campaign1.Id, null, null, "MetaAds", "c1", null, null, day2, null, MetricGranularity.Daily, 120m, "BRL", 1200, 60, 3m, 300m).Value,
            CampaignMetric.Create(Guid.NewGuid(), workspaceId, accountId, campaign2.Id, null, null, "MetaAds", "c2", null, null, day2, null, MetricGranularity.Daily, 80m, "BRL", 800, 40, 1m, 100m).Value,
            CampaignMetric.Create(Guid.NewGuid(), workspaceId, accountId, campaign1.Id, null, null, "MetaAds", "c1", null, null, day3, null, MetricGranularity.Daily, 150m, "BRL", 1500, 70, 4m, 400m).Value
        };

        await _repository.UpsertMetricsBatchAsync(workspaceId, metrics);

        // Act - Filtrar apenas Campanha 1 entre day1 e day2
        var result = await _repository.GetMetricsAsync(
            workspaceId,
            startDateUtc: day1,
            endDateUtc: day2,
            granularity: MetricGranularity.Daily,
            campaignId: campaign1.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().OnlyContain(m => m.CampaignId == campaign1.Id);
        result.Value.Should().Contain(m => m.Date == day1);
        result.Value.Should().Contain(m => m.Date == day2);
    }

    /// <summary>
    /// Libera recursos da conexão SQLite em memória.
    /// </summary>
    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}
