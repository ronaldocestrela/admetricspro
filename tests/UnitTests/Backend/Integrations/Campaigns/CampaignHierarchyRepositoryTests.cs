using BuildingBlocks.Domain.Campaigns;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.Persistence;
using FluentAssertions;
using Integrations.Domain.Campaigns.Models;
using Integrations.Infrastructure.Campaigns.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace UnitTests.Backend.Integrations.Campaigns;

/// <summary>
/// Testes unitários para o repositório de hierarquia estrutural de campanhas (<see cref="CampaignHierarchyRepository"/>).
/// </summary>
public sealed class CampaignHierarchyRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TenantDbContext _dbContext;
    private readonly ITenantDbContextAccessor _contextAccessor;
    private readonly CampaignHierarchyRepository _repository;

    /// <summary>
    /// Inicializa a base SQLite em memória e o repositório sob teste.
    /// </summary>
    public CampaignHierarchyRepositoryTests()
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

        _repository = new CampaignHierarchyRepository(_contextAccessor);
    }

    /// <summary>
    /// Valida que a inserção inicial de hierarquia persiste campanhas, conjuntos e anúncios com integridade relacional.
    /// </summary>
    [Fact]
    public async Task UpsertHierarchyBatchAsync_WhenNewHierarchy_ShouldInsertAllLevels()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var syncTime = DateTime.UtcNow;

        var hierarchy = new UnifiedCampaignHierarchy(
            new List<UnifiedCampaignItem>
            {
                new("cmp_1", "Campanha 1", CampaignStatus.Active, "SALES", 100m, null, "BRL", null, null)
            },
            new List<UnifiedAdSetItem>
            {
                new("set_1", "cmp_1", "Conjunto 1", AdSetStatus.Active, "LOWEST_COST", "CONVERSIONS", 100m, null, null, null, null)
            },
            new List<UnifiedAdItem>
            {
                new("ad_1", "set_1", "cmp_1", "Anúncio 1", AdStatus.Active, AdCreativeType.Image, "Título", "Texto", "https://site.com", null, "SHOP_NOW")
            });

        // Act
        var result = await _repository.UpsertHierarchyBatchAsync(
            workspaceId,
            accountId,
            "MetaAds",
            hierarchy,
            syncTime);

        await _dbContext.SaveChangesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(3); // 1 campanha + 1 conjunto + 1 anúncio

        var campaigns = await _repository.GetCampaignsByWorkspaceAsync(workspaceId);
        campaigns.Should().HaveCount(1);
        campaigns[0].AdSets.Should().HaveCount(1);
        campaigns[0].AdSets.First().Ads.Should().HaveCount(1);
    }

    /// <summary>
    /// Valida que uma re-sincronização da mesma hierarquia atualiza os registros existentes sem duplicar linhas.
    /// </summary>
    [Fact]
    public async Task UpsertHierarchyBatchAsync_WhenReSynced_ShouldUpdateExistingWithoutDuplication()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var syncTime1 = DateTime.UtcNow.AddHours(-1);

        var hierarchy1 = new UnifiedCampaignHierarchy(
            new List<UnifiedCampaignItem>
            {
                new("cmp_10", "Campanha Original", CampaignStatus.Active, "LEADS", 50m, null, "BRL", null, null)
            },
            new List<UnifiedAdSetItem>
            {
                new("set_10", "cmp_10", "Conjunto Original", AdSetStatus.Active, null, null, 50m, null, null, null, null)
            },
            new List<UnifiedAdItem>
            {
                new("ad_10", "set_10", "cmp_10", "Ad Original", AdStatus.Active, AdCreativeType.Text, "T1", "B1", "https://site.com", null, null)
            });

        await _repository.UpsertHierarchyBatchAsync(workspaceId, accountId, "GoogleAds", hierarchy1, syncTime1);
        await _dbContext.SaveChangesAsync();

        var syncTime2 = DateTime.UtcNow;
        var hierarchy2 = new UnifiedCampaignHierarchy(
            new List<UnifiedCampaignItem>
            {
                new("cmp_10", "Campanha Atualizada", CampaignStatus.Paused, "LEADS", 80m, null, "BRL", null, null)
            },
            new List<UnifiedAdSetItem>
            {
                new("set_10", "cmp_10", "Conjunto Atualizado", AdSetStatus.Paused, null, null, 80m, null, null, null, null)
            },
            new List<UnifiedAdItem>
            {
                new("ad_10", "set_10", "cmp_10", "Ad Atualizado", AdStatus.Paused, AdCreativeType.Text, "T2", "B2", "https://site2.com", null, null)
            });

        // Act
        var result = await _repository.UpsertHierarchyBatchAsync(workspaceId, accountId, "GoogleAds", hierarchy2, syncTime2);
        await _dbContext.SaveChangesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();

        var campaigns = await _repository.GetCampaignsByWorkspaceAsync(workspaceId);
        campaigns.Should().HaveCount(1);
        campaigns[0].Name.Should().Be("Campanha Atualizada");
        campaigns[0].Status.Should().Be(CampaignStatus.Paused);
        campaigns[0].DailyBudget.Should().Be(80m);
        campaigns[0].AdSets.First().Name.Should().Be("Conjunto Atualizado");
        campaigns[0].AdSets.First().Ads.First().Name.Should().Be("Ad Atualizado");
    }

    /// <summary>
    /// Valida que a consulta de campanha por ID carrega a árvore completa com AdSets e Ads associados.
    /// </summary>
    [Fact]
    public async Task GetCampaignWithHierarchyByIdAsync_ShouldLoadFullTree()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var syncTime = DateTime.UtcNow;

        var hierarchy = new UnifiedCampaignHierarchy(
            new List<UnifiedCampaignItem>
            {
                new("cmp_99", "Campanha Detalhada", CampaignStatus.Active, "TRAFFIC", 120m, null, "BRL", null, null)
            },
            new List<UnifiedAdSetItem>
            {
                new("set_99", "cmp_99", "Conjunto Detalhado", AdSetStatus.Active, null, null, 120m, null, null, null, null)
            },
            new List<UnifiedAdItem>
            {
                new("ad_99", "set_99", "cmp_99", "Ad Detalhado", AdStatus.Active, AdCreativeType.Video, "Headline", "Body", "https://dest.com", null, null)
            });

        await _repository.UpsertHierarchyBatchAsync(workspaceId, accountId, "TikTokAds", hierarchy, syncTime);
        await _dbContext.SaveChangesAsync();

        var campaign = (await _repository.GetCampaignsByWorkspaceAsync(workspaceId)).First();

        // Act
        var loaded = await _repository.GetCampaignWithHierarchyByIdAsync(campaign.Id);

        // Assert
        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(campaign.Id);
        loaded.AdSets.Should().HaveCount(1);
        loaded.AdSets.First().Ads.Should().HaveCount(1);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}
