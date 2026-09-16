using Automations.Domain.SafetyGuards;
using Automations.Infrastructure.SafetyGuards;
using BuildingBlocks.Application.Campaigns.Commands;
using BuildingBlocks.Domain.Automations.SafetyGuards;
using BuildingBlocks.Domain.Campaigns;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.Persistence;
using FluentAssertions;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace UnitTests.Backend.Automations;

/// <summary>
/// Testes unitários para o serviço OverspendingGuard (Subfase 4.2.1 - TDD).
/// Valida a trava de segurança que pausa imediatamente campanhas com gasto diário superior a 120% do orçamento.
/// </summary>
public sealed class OverspendingGuardTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly ISecurityAlertNotifier _alertNotifier = Substitute.For<ISecurityAlertNotifier>();
    private readonly ITenantDbContextAccessor _contextAccessor = Substitute.For<ITenantDbContextAccessor>();
    private readonly TenantDbContext _dbContext;
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _connectedAccountId = Guid.NewGuid();

    /// <summary>
    /// Inicializa o contexto SQLite em memória e mocks para a suíte de testes.
    /// </summary>
    public OverspendingGuardTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new TenantDbContext(options);
        _dbContext.Database.EnsureCreated();

        _contextAccessor.GetDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<TenantDbContext>.Success(_dbContext)));

        _sender.Send(Arg.Any<PauseCampaignCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        _alertNotifier.DispatchAlertAsync(Arg.Any<SafetyAlertPayload>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyDictionary<SafetyAlertChannel, bool>>.Success(
                new Dictionary<SafetyAlertChannel, bool>
                {
                    { SafetyAlertChannel.Slack, true },
                    { SafetyAlertChannel.Email, true }
                })));
    }

    /// <summary>
    /// Libera recursos do banco de dados em memória e conexão SQLite.
    /// </summary>
    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    private Campaign CreateCampaign(
        Guid id,
        string name,
        CampaignStatus status,
        decimal? dailyBudget,
        string platform = "MetaAds")
    {
        var campaignResult = Campaign.Create(
            id,
            _workspaceId,
            _connectedAccountId,
            platform,
            $"ext_{id}",
            name,
            status,
            "Conversions",
            dailyBudget,
            null,
            "BRL",
            DateTime.UtcNow.AddDays(-10),
            null);

        return campaignResult.Value;
    }

    private void AddMetric(Guid campaignId, decimal spend, string platform = "MetaAds")
    {
        var metricResult = CampaignMetric.Create(
            Guid.NewGuid(),
            _workspaceId,
            _connectedAccountId,
            campaignId,
            null,
            null,
            platform,
            $"ext_{campaignId}",
            null,
            null,
            DateTime.UtcNow.Date,
            null,
            MetricGranularity.Daily,
            spend,
            "BRL",
            1000,
            50,
            2m,
            100m);

        _dbContext.CampaignMetrics.Add(metricResult.Value);
    }

    /// <summary>
    /// Não deve disparar pausa nem alerta se o gasto diário estiver dentro do limite normal (ex: 80% do orçamento).
    /// </summary>
    [Fact]
    public async Task CheckAndMitigateOverspendingAsync_ShouldNotPause_WhenSpendIsUnderThreshold()
    {
        // Arrange: Orçamento de R$ 100,00 e gasto de R$ 80,00 (80% <= 120%)
        var campaignId = Guid.NewGuid();
        var campaign = CreateCampaign(campaignId, "Campanha Normal", CampaignStatus.Active, 100m);
        _dbContext.Campaigns.Add(campaign);
        AddMetric(campaignId, 80m);
        await _dbContext.SaveChangesAsync();

        var guard = new OverspendingGuard(_contextAccessor, _sender, _alertNotifier);

        // Act
        var result = await guard.CheckAndMitigateOverspendingAsync(_workspaceId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.EvaluatedCampaignsCount.Should().Be(1);
        result.Value.ViolatedCampaignsCount.Should().Be(0);
        result.Value.PausedCampaignsCount.Should().Be(0);
        result.Value.Incidents.Should().BeEmpty();

        await _sender.DidNotReceive().Send(Arg.Any<PauseCampaignCommand>(), Arg.Any<CancellationToken>());
        await _alertNotifier.DidNotReceive().DispatchAlertAsync(Arg.Any<SafetyAlertPayload>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Não deve disparar pausa nem alerta se o gasto for exatamente 120% do orçamento (limite exato).
    /// </summary>
    [Fact]
    public async Task CheckAndMitigateOverspendingAsync_ShouldNotPause_WhenSpendIsExactlyAt120Percent()
    {
        // Arrange: Orçamento de R$ 100,00 e gasto de R$ 120,00
        var campaignId = Guid.NewGuid();
        var campaign = CreateCampaign(campaignId, "Campanha Limite", CampaignStatus.Active, 100m);
        _dbContext.Campaigns.Add(campaign);
        AddMetric(campaignId, 120m);
        await _dbContext.SaveChangesAsync();

        var guard = new OverspendingGuard(_contextAccessor, _sender, _alertNotifier);

        // Act
        var result = await guard.CheckAndMitigateOverspendingAsync(_workspaceId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ViolatedCampaignsCount.Should().Be(0);
        await _sender.DidNotReceive().Send(Arg.Any<PauseCampaignCommand>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Deve disparar pausa imediata e alarme multi-canal quando o gasto superar 120% (ex: 125%).
    /// </summary>
    [Fact]
    public async Task CheckAndMitigateOverspendingAsync_ShouldPauseAndAlert_WhenSpendExceeds120Percent()
    {
        // Arrange: Orçamento de R$ 100,00 e gasto diário de R$ 125,00 (125% > 120%)
        var campaignId = Guid.NewGuid();
        var campaign = CreateCampaign(campaignId, "Campanha Estourada", CampaignStatus.Active, 100m);
        _dbContext.Campaigns.Add(campaign);
        AddMetric(campaignId, 125m);
        await _dbContext.SaveChangesAsync();

        var guard = new OverspendingGuard(_contextAccessor, _sender, _alertNotifier);

        // Act
        var result = await guard.CheckAndMitigateOverspendingAsync(_workspaceId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.EvaluatedCampaignsCount.Should().Be(1);
        result.Value.ViolatedCampaignsCount.Should().Be(1);
        result.Value.PausedCampaignsCount.Should().Be(1);
        result.Value.Incidents.Should().HaveCount(1);

        var incident = result.Value.Incidents[0];
        incident.GuardType.Should().Be(SafetyGuardType.Overspending);
        incident.TargetEntityId.Should().Be(campaignId);
        incident.CurrentSpend.Should().Be(125m);
        incident.DailyBudget.Should().Be(100m);

        // Verifica despacho do comando PauseCampaignCommand via MediatR
        await _sender.Received(1).Send(
            Arg.Is<PauseCampaignCommand>(cmd =>
                cmd.WorkspaceId == _workspaceId &&
                cmd.CampaignId == campaignId &&
                cmd.Reason!.Contains("120%")),
            Arg.Any<CancellationToken>());

        // Verifica despacho do alarme multi-canal
        await _alertNotifier.Received(1).DispatchAlertAsync(
            Arg.Is<SafetyAlertPayload>(p =>
                p.WorkspaceId == _workspaceId &&
                p.GuardType == SafetyGuardType.Overspending &&
                p.TargetEntityId == campaignId &&
                p.Severity == SafetyAlertSeverity.Critical),
            Arg.Any<CancellationToken>());

        // Verifica persistência do incidente no TenantDbContext
        _dbContext.SafetyGuardIncidents.Should().ContainSingle(i => i.TargetEntityId == campaignId);
    }

    /// <summary>
    /// Deve ignorar com segurança campanhas sem DailyBudget configurado (null ou zero).
    /// </summary>
    [Fact]
    public async Task CheckAndMitigateOverspendingAsync_ShouldIgnoreCampaignsWithoutDailyBudget()
    {
        // Arrange: Campanha com orçamento nulo
        var campaignId1 = Guid.NewGuid();
        var campaign1 = CreateCampaign(campaignId1, "Sem Budget", CampaignStatus.Active, null);

        // Campanha com orçamento zero
        var campaignId2 = Guid.NewGuid();
        var campaign2 = CreateCampaign(campaignId2, "Budget Zero", CampaignStatus.Active, 0m);

        _dbContext.Campaigns.AddRange(campaign1, campaign2);
        AddMetric(campaignId1, 50m);
        AddMetric(campaignId2, 50m);
        await _dbContext.SaveChangesAsync();

        var guard = new OverspendingGuard(_contextAccessor, _sender, _alertNotifier);

        // Act
        var result = await guard.CheckAndMitigateOverspendingAsync(_workspaceId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.EvaluatedCampaignsCount.Should().Be(0);
        result.Value.ViolatedCampaignsCount.Should().Be(0);
        await _sender.DidNotReceive().Send(Arg.Any<PauseCampaignCommand>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Não deve tentar pausar novamente campanhas que já estejam em status Paused.
    /// </summary>
    [Fact]
    public async Task CheckAndMitigateOverspendingAsync_ShouldNotPause_WhenCampaignAlreadyPaused()
    {
        var campaignId = Guid.NewGuid();
        var campaign = CreateCampaign(campaignId, "Já Pausada", CampaignStatus.Paused, 100m);
        _dbContext.Campaigns.Add(campaign);
        AddMetric(campaignId, 150m);
        await _dbContext.SaveChangesAsync();

        var guard = new OverspendingGuard(_contextAccessor, _sender, _alertNotifier);

        // Act
        var result = await guard.CheckAndMitigateOverspendingAsync(_workspaceId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.EvaluatedCampaignsCount.Should().Be(0);
        await _sender.DidNotReceive().Send(Arg.Any<PauseCampaignCommand>(), Arg.Any<CancellationToken>());
    }
}
