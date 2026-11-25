using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Messaging.Persistence;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Master.Application.Billing.Dunning;
using Master.Application.Messaging;
using Master.Application.Repositories;
using Master.Domain.Tenants;
using Master.Domain.Tenants.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace UnitTests.Backend.Dunning;

/// <summary>
/// Unit tests for <see cref="DunningEngineService"/>.
/// </summary>
public sealed class DunningEngineServiceTests
{
    private readonly ITenantRepository _tenantRepository = Substitute.For<ITenantRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly ILogger<DunningEngineService> _logger = Substitute.For<ILogger<DunningEngineService>>();
    private readonly IMessagingFeatureGate _messagingGate = Substitute.For<IMessagingFeatureGate>();
    private readonly ICatalogIntegrationOutboxWriter _catalogOutbox = Substitute.For<ICatalogIntegrationOutboxWriter>();
    private readonly DateTime _referenceUtc = new(2026, 9, 3, 12, 0, 0, DateTimeKind.Utc);

    private DunningEngineService CreateService() => new(
        _tenantRepository,
        _unitOfWork,
        _publisher,
        _logger,
        _messagingGate,
        _catalogOutbox);

    /// <summary>
    /// Verifies that when no tenants require evaluation, an empty summary is returned without commits or events.
    /// </summary>
    [Fact]
    public async Task ProcessDunningCycleAsync_ShouldReturnEmptySummary_WhenNoTenantsRequireEvaluation()
    {
        _messagingGate.IsEnabled.Returns(false);
        _tenantRepository.GetTenantsForDunningEvaluationAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Tenant>());

        var result = await CreateService().ProcessDunningCycleAsync(_referenceUtc, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var summary = result.Value;
        summary.EvaluatedCount.Should().Be(0);
        summary.TransitionsCount.Should().Be(0);
        summary.SuspendedCount.Should().Be(0);
        summary.UnchangedCount.Should().Be(0);

        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
        await _catalogOutbox.DidNotReceive().AddAsync(Arg.Any<IntegrationOutboxMessage>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that when tenants are overdue, appropriate transitions are evaluated, saved, and published.
    /// </summary>
    [Fact]
    public async Task ProcessDunningCycleAsync_ShouldEvaluateTenantsAndCommitChanges_WhenTransitionsOccur()
    {
        _messagingGate.IsEnabled.Returns(false);

        var tenant1 = Tenant.Create("Agencia D4", "12345678000191", "agencia-d4", adminEmail: "d4@example.com").Value;
        tenant1.MarkPaymentOverdue(_referenceUtc.AddDays(-4));

        var tenant2 = Tenant.Create("Agencia D15", "12345678000192", "agencia-d15", adminEmail: "d15@example.com").Value;
        tenant2.MarkPaymentOverdue(_referenceUtc.AddDays(-15));

        var tenant3 = Tenant.Create("Agencia D1", "12345678000193", "agencia-d1", adminEmail: "d1@example.com").Value;
        tenant3.MarkPaymentOverdue(_referenceUtc.AddDays(-1));

        _tenantRepository.GetTenantsForDunningEvaluationAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Tenant> { tenant1, tenant2, tenant3 });

        var result = await CreateService().ProcessDunningCycleAsync(_referenceUtc, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var summary = result.Value;
        summary.EvaluatedCount.Should().Be(3);
        summary.TransitionsCount.Should().Be(2);
        summary.SuspendedCount.Should().Be(1);
        summary.UnchangedCount.Should().Be(1);

        tenant1.DunningStage.Should().Be(DunningStage.AutomationsDisabled);
        tenant2.DunningStage.Should().Be(DunningStage.LoginBlocked);
        tenant2.Status.Should().Be(TenantStatus.Suspended);
        tenant3.DunningStage.Should().Be(DunningStage.None);

        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _publisher.Received(2).Publish(
            Arg.Any<DomainEventNotification<TenantGracePeriodExceededEvent>>(),
            Arg.Any<CancellationToken>());
        await _catalogOutbox.DidNotReceive().AddAsync(Arg.Any<IntegrationOutboxMessage>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Com mensageria ligada, transições D+3, D+7 e D+14 enfileiram três outboxes no mesmo commit.
    /// </summary>
    [Fact]
    public async Task ProcessDunningCycleAsync_WhenMessagingEnabled_ShouldEnqueueStageEmailsOnTransitions()
    {
        _messagingGate.IsEnabled.Returns(true);
        _catalogOutbox.AddAsync(Arg.Any<IntegrationOutboxMessage>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var tenantD4 = Tenant.Create("D4", "12345678000191", "d4", adminEmail: "d4@example.com").Value;
        tenantD4.MarkPaymentOverdue(_referenceUtc.AddDays(-4));

        var tenantD8 = Tenant.Create("D8", "12345678000192", "d8", adminEmail: "d8@example.com").Value;
        tenantD8.MarkPaymentOverdue(_referenceUtc.AddDays(-8));

        var tenantD15 = Tenant.Create("D15", "12345678000193", "d15", adminEmail: "d15@example.com").Value;
        tenantD15.MarkPaymentOverdue(_referenceUtc.AddDays(-15));

        _tenantRepository.GetTenantsForDunningEvaluationAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Tenant> { tenantD4, tenantD8, tenantD15 });

        var result = await CreateService().ProcessDunningCycleAsync(_referenceUtc, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TransitionsCount.Should().Be(3);

        await _catalogOutbox.Received(3).AddAsync(
            Arg.Is<IntegrationOutboxMessage>(m => m.MessageType == CatalogIntegrationMessageTypes.DunningStageEmail),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Carência D+1 não grava outbox de e-mail.
    /// </summary>
    [Fact]
    public async Task ProcessDunningCycleAsync_WhenGracePeriod_ShouldNotEnqueueEmail()
    {
        _messagingGate.IsEnabled.Returns(true);

        var tenant = Tenant.Create("D1", "12345678000190", "d1", adminEmail: "d1@example.com").Value;
        tenant.MarkPaymentOverdue(_referenceUtc.AddDays(-1));

        _tenantRepository.GetTenantsForDunningEvaluationAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Tenant> { tenant });

        var result = await CreateService().ProcessDunningCycleAsync(_referenceUtc, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TransitionsCount.Should().Be(0);

        await _catalogOutbox.DidNotReceive().AddAsync(Arg.Any<IntegrationOutboxMessage>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Segunda execução com estágios já persistidos não enfileira novamente.
    /// </summary>
    [Fact]
    public async Task ProcessDunningCycleAsync_WhenNoNewTransitions_ShouldNotEnqueueOutbox()
    {
        _messagingGate.IsEnabled.Returns(true);

        var tenant = Tenant.Create("D4", "12345678000190", "d4", adminEmail: "d4@example.com").Value;
        tenant.MarkPaymentOverdue(_referenceUtc.AddDays(-4));
        tenant.EvaluateDunningStage(_referenceUtc);

        _tenantRepository.GetTenantsForDunningEvaluationAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Tenant> { tenant });

        var result = await CreateService().ProcessDunningCycleAsync(_referenceUtc, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TransitionsCount.Should().Be(0);

        await _catalogOutbox.DidNotReceive().AddAsync(Arg.Any<IntegrationOutboxMessage>(), Arg.Any<CancellationToken>());
    }
}