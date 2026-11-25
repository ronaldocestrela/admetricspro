using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Messaging.Persistence;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Campaigns;
using BuildingBlocks.Domain.Campaigns.Events;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using BuildingBlocks.Infrastructure.MultiTenancy;
using FluentAssertions;
using Integrations.Application.Campaigns.Commands.SyncCampaignMetrics;
using Integrations.Application.Campaigns.Queries.GetCampaignMetrics;
using Integrations.Application.Persistence;
using Integrations.Domain.Campaigns;
using Integrations.Domain.Campaigns.Models;
using Integrations.Domain.Campaigns.Sync;
using Integrations.Domain.OAuth;
using MediatR;
using NSubstitute;

namespace UnitTests.Backend.Integrations.Campaigns;

/// <summary>
/// Testes unitários para o manipulador do comando <see cref="SyncCampaignMetricsCommandHandler"/>
/// e consulta <see cref="GetCampaignMetricsQueryHandler"/>.
/// </summary>
public sealed class SyncCampaignMetricsCommandHandlerTests
{
    private readonly ICampaignMetricsRepository _metricsRepository = Substitute.For<ICampaignMetricsRepository>();
    private readonly ICampaignHierarchyRepository _hierarchyRepository = Substitute.For<ICampaignHierarchyRepository>();
    private readonly IOAuthTokenVaultRepository _tokenVaultRepository = Substitute.For<IOAuthTokenVaultRepository>();
    private readonly IOAuthEncryptionService _encryptionService = Substitute.For<IOAuthEncryptionService>();
    private readonly ICampaignMetricsSyncDispatcher _syncDispatcher = Substitute.For<ICampaignMetricsSyncDispatcher>();
    private readonly IIntegrationsUnitOfWork _unitOfWork = Substitute.For<IIntegrationsUnitOfWork>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly ITenantContextAccessor _tenantContextAccessor = Substitute.For<ITenantContextAccessor>();
    private readonly IMessagingFeatureGate _messagingFeatureGate = Substitute.For<IMessagingFeatureGate>();
    private readonly IIntegrationOutboxWriter _outboxWriter = Substitute.For<IIntegrationOutboxWriter>();

    private readonly SyncCampaignMetricsCommandHandler _handler;

    /// <summary>
    /// Inicializa os mocks e o handler sob teste.
    /// </summary>
    public SyncCampaignMetricsCommandHandlerTests()
    {
        _messagingFeatureGate.IsEnabled.Returns(false);
        _handler = new SyncCampaignMetricsCommandHandler(
            _metricsRepository,
            _hierarchyRepository,
            _tokenVaultRepository,
            _encryptionService,
            _syncDispatcher,
            _unitOfWork,
            _publisher,
            _tenantContextAccessor,
            _messagingFeatureGate,
            _outboxWriter);
    }

    /// <summary>
    /// Valida que requisição com WorkspaceId vazio retorna erro de validação.
    /// </summary>
    [Fact]
    public async Task Handle_WhenWorkspaceIdEmpty_ShouldReturnValidationError()
    {
        // Arrange
        var command = new SyncCampaignMetricsCommand(
            Guid.Empty,
            null,
            null,
            DateTime.UtcNow.AddDays(-7),
            DateTime.UtcNow,
            MetricGranularity.Daily);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SyncCampaignMetrics.EmptyWorkspaceId");
    }

    /// <summary>
    /// Valida que período onde data inicial é maior que data final é rejeitado.
    /// </summary>
    [Fact]
    public async Task Handle_WhenStartDateGreaterThanEndDate_ShouldReturnValidationError()
    {
        // Arrange
        var command = new SyncCampaignMetricsCommand(
            Guid.NewGuid(),
            null,
            null,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(-1),
            MetricGranularity.Daily);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SyncCampaignMetrics.InvalidDateRange");
    }

    /// <summary>
    /// Valida que quando uma conta específica não é encontrada, retorna NotFound.
    /// </summary>
    [Fact]
    public async Task Handle_WhenSpecificAccountNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        _hierarchyRepository.GetAccountsForSyncAsync(workspaceId, accountId, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ConnectedAdAccount>>(Array.Empty<ConnectedAdAccount>()));

        var command = new SyncCampaignMetricsCommand(
            workspaceId,
            accountId,
            null,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow,
            MetricGranularity.Daily);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("SyncCampaignMetrics.AccountNotFound");
    }

    /// <summary>
    /// Valida que conta demo orquestra busca, mapeamento relacional, upsert atômico e disparo de evento in-memory.
    /// </summary>
    [Fact]
    public async Task Handle_WhenDemoAccount_ShouldFetchMetricsAndUpsertAndPublishEvent()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var start = DateTime.UtcNow.AddDays(-2).Date;
        var end = DateTime.UtcNow.Date;

        var account = ConnectedAdAccount.Create(
            accountId,
            workspaceId,
            "MetaAds",
            "act_demo_123",
            "Conta Demonstração",
            "BRL",
            isDemo: true).Value;

        _hierarchyRepository.GetAccountsForSyncAsync(workspaceId, accountId, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ConnectedAdAccount>>(new[] { account }));

        var fetchedMetrics = new List<UnifiedMetricItem>
        {
            new("cmp_1", null, null, start, null, 150m, 3000, 120, 5m, 450m, "BRL"),
            new("cmp_1", null, null, end, null, 200m, 4000, 160, 8m, 720m, "BRL")
        };

        _syncDispatcher.DispatchAsync(account, null, start, end, MetricGranularity.Daily, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<IReadOnlyList<UnifiedMetricItem>>.Success(fetchedMetrics)));

        _hierarchyRepository.GetCampaignsByWorkspaceAsync(workspaceId, accountId, "MetaAds", null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Campaign>>(Array.Empty<Campaign>()));

        _metricsRepository.UpsertMetricsBatchAsync(workspaceId, Arg.Any<IReadOnlyList<CampaignMetric>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<int>.Success(2)));

        var command = new SyncCampaignMetricsCommand(
            workspaceId,
            accountId,
            null,
            start,
            end,
            MetricGranularity.Daily);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalAccountsProcessed.Should().Be(1);
        result.Value.TotalRecordsIngested.Should().Be(2);
        result.Value.TotalSpend.Should().Be(350m);
        result.Value.TotalImpressions.Should().Be(7000);
        result.Value.TotalClicks.Should().Be(280);
        result.Value.TotalConversions.Should().Be(13m);
        result.Value.TotalConversionValue.Should().Be(1170m);

        // Verifica disparo do evento in-memory
        await _publisher.Received(1).Publish(
            Arg.Is<CampaignMetricsSyncedEvent>(e =>
                e.WorkspaceId == workspaceId &&
                e.ConnectedAdAccountId == accountId &&
                e.TotalRecordsIngested == 2 &&
                e.TotalSpend == 350m),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que a consulta <see cref="GetCampaignMetricsQueryHandler"/> retorna os registros mapeados com KPIs calculados.
    /// </summary>
    [Fact]
    public async Task QueryHandler_WhenCalled_ShouldReturnCalculatedKpis()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var date = DateTime.UtcNow.Date;

        var metric = CampaignMetric.Create(
            Guid.NewGuid(),
            workspaceId,
            accountId,
            campaignId,
            null,
            null,
            "MetaAds",
            "cmp_1",
            null,
            null,
            date,
            null,
            MetricGranularity.Daily,
            spend: 500m,
            currency: "BRL",
            impressions: 20000,
            clicks: 1000,
            conversions: 50m,
            conversionValue: 2500m).Value;

        _metricsRepository.GetMetricsAsync(workspaceId, date, date, MetricGranularity.Daily, null, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result<IReadOnlyList<CampaignMetric>>>(Result<IReadOnlyList<CampaignMetric>>.Success(new[] { metric })));

        var queryHandler = new GetCampaignMetricsQueryHandler(_metricsRepository);
        var query = new GetCampaignMetricsQuery(workspaceId, date, date, MetricGranularity.Daily);

        // Act
        var result = await queryHandler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        var dto = result.Value.Single();
        dto.Spend.Should().Be(500m);
        dto.Ctr.Should().Be(5.0m);
        dto.Cpc.Should().Be(0.50m);
        dto.Cpm.Should().Be(25.00m);
        dto.Cpa.Should().Be(10.00m);
        dto.Roas.Should().Be(5.00m);
    }

    /// <summary>
    /// Com mensageria ligada, grava outbox e não publica in-memory.
    /// </summary>
    [Fact]
    public async Task Handle_WhenMessagingEnabled_EnqueuesOutboxAndDoesNotPublish()
    {
        _messagingFeatureGate.IsEnabled.Returns(true);
        var tenantId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        _tenantContextAccessor.TenantContext = TenantContext.Create(tenantId, null, TenantResolutionSource.None);

        var account = ConnectedAdAccount.Create(accountId, workspaceId, "MetaAds", "act_1", "Conta", "BRL", isDemo: true).Value;
        var start = DateTime.UtcNow.AddDays(-2).Date;
        var end = DateTime.UtcNow.Date;

        _hierarchyRepository.GetAccountsForSyncAsync(workspaceId, accountId, null, Arg.Any<CancellationToken>())
            .Returns(new[] { account });

        var fetchedMetrics = new List<UnifiedMetricItem>
        {
            new("cmp_1", null, null, start, null, 100m, 1000, 50, 1m, 10m, "BRL"),
        };

        _syncDispatcher.DispatchAsync(account, null, start, end, MetricGranularity.Daily, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<UnifiedMetricItem>>.Success(fetchedMetrics));

        _hierarchyRepository.GetCampaignsByWorkspaceAsync(workspaceId, accountId, "MetaAds", null, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Campaign>());

        _metricsRepository.UpsertMetricsBatchAsync(workspaceId, Arg.Any<IReadOnlyList<CampaignMetric>>(), Arg.Any<CancellationToken>())
            .Returns(Result<int>.Success(1));

        _outboxWriter.AddAsync(Arg.Any<BuildingBlocks.Domain.Messaging.IntegrationOutboxMessage>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var command = new SyncCampaignMetricsCommand(workspaceId, accountId, null, start, end, MetricGranularity.Daily);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _outboxWriter.Received(1).AddAsync(Arg.Any<BuildingBlocks.Domain.Messaging.IntegrationOutboxMessage>(), Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().Publish(Arg.Any<CampaignMetricsSyncedEvent>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tenant ausente com mensageria ligada falha antes de persistir métricas.
    /// </summary>
    [Fact]
    public async Task Handle_WhenMessagingEnabledWithoutTenant_ReturnsTenantRequired()
    {
        _messagingFeatureGate.IsEnabled.Returns(true);
        _tenantContextAccessor.TenantContext.Returns((TenantContext?)null);

        var workspaceId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var account = ConnectedAdAccount.Create(accountId, workspaceId, "MetaAds", "act_1", "Conta", "BRL", isDemo: true).Value;
        var start = DateTime.UtcNow.AddDays(-2).Date;
        var end = DateTime.UtcNow.Date;

        _hierarchyRepository.GetAccountsForSyncAsync(workspaceId, accountId, null, Arg.Any<CancellationToken>())
            .Returns(new[] { account });

        _syncDispatcher.DispatchAsync(account, null, start, end, MetricGranularity.Daily, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<UnifiedMetricItem>>.Success(
                [new UnifiedMetricItem("cmp_1", null, null, start, null, 100m, 1000, 50, 1m, 10m, "BRL")]));

        _hierarchyRepository.GetCampaignsByWorkspaceAsync(workspaceId, accountId, "MetaAds", null, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Campaign>());

        var command = new SyncCampaignMetricsCommand(workspaceId, accountId, null, start, end, MetricGranularity.Daily);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Messaging.TenantRequired");
        await _metricsRepository.DidNotReceive()
            .UpsertMetricsBatchAsync(Arg.Any<Guid>(), Arg.Any<IReadOnlyList<CampaignMetric>>(), Arg.Any<CancellationToken>());
    }
}
