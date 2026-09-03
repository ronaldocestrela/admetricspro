using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Persistence;
using FluentAssertions;
using Master.Application.Billing.Dunning;
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
    private readonly DateTime _referenceUtc = new(2026, 9, 3, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Verifies that when no tenants require evaluation, an empty summary is returned without commits or events.
    /// </summary>
    [Fact]
    public async Task ProcessDunningCycleAsync_ShouldReturnEmptySummary_WhenNoTenantsRequireEvaluation()
    {
        // Arrange
        _tenantRepository.GetTenantsForDunningEvaluationAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Tenant>());

        var service = new DunningEngineService(_tenantRepository, _unitOfWork, _publisher, _logger);

        // Act
        var result = await service.ProcessDunningCycleAsync(_referenceUtc, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var summary = result.Value;
        summary.EvaluatedCount.Should().Be(0);
        summary.TransitionsCount.Should().Be(0);
        summary.SuspendedCount.Should().Be(0);
        summary.UnchangedCount.Should().Be(0);

        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that when tenants are overdue, appropriate transitions are evaluated, saved, and published.
    /// </summary>
    [Fact]
    public async Task ProcessDunningCycleAsync_ShouldEvaluateTenantsAndCommitChanges_WhenTransitionsOccur()
    {
        // Arrange
        var tenant1 = Tenant.Create("Agencia D4", "12345678000191", "agencia-d4").Value;
        tenant1.MarkPaymentOverdue(_referenceUtc.AddDays(-4));

        var tenant2 = Tenant.Create("Agencia D15", "12345678000192", "agencia-d15").Value;
        tenant2.MarkPaymentOverdue(_referenceUtc.AddDays(-15));

        var tenant3 = Tenant.Create("Agencia D1", "12345678000193", "agencia-d1").Value;
        tenant3.MarkPaymentOverdue(_referenceUtc.AddDays(-1));

        var tenantsList = new List<Tenant> { tenant1, tenant2, tenant3 };

        _tenantRepository.GetTenantsForDunningEvaluationAsync(Arg.Any<CancellationToken>())
            .Returns(tenantsList);

        var service = new DunningEngineService(_tenantRepository, _unitOfWork, _publisher, _logger);

        // Act
        var result = await service.ProcessDunningCycleAsync(_referenceUtc, CancellationToken.None);

        // Assert
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
    }
}
