using BuildingBlocks.Application.Messaging;
using FluentAssertions;
using Master.Application.Messaging;
using Master.Domain.Tenants;
using Xunit;

namespace UnitTests.Backend.Messaging;

/// <summary>
/// Garante EventIds determinísticos para idempotência de enfileiramento.
/// </summary>
public sealed class CatalogIntegrationStableEventIdsTests
{
    /// <summary>
    /// Mesmas entradas produzem o mesmo EventId para trial.
    /// </summary>
    [Fact]
    public void ForTrialNotice_ShouldBeDeterministic()
    {
        var tenantId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");

        var first = CatalogIntegrationStableEventIds.ForTrialNotice(tenantId, TrialNoticeType.TrialReminder7Days);
        var second = CatalogIntegrationStableEventIds.ForTrialNotice(tenantId, TrialNoticeType.TrialReminder7Days);

        first.Should().Be(second);
        first.Should().NotBe(
            CatalogIntegrationStableEventIds.ForTrialNotice(tenantId, TrialNoticeType.TrialReminder3Days));
    }

    /// <summary>
    /// Mesmas entradas produzem o mesmo EventId para dunning.
    /// </summary>
    [Fact]
    public void ForDunningStage_ShouldBeDeterministic()
    {
        var tenantId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
        var due = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

        var first = CatalogIntegrationStableEventIds.ForDunningStage(tenantId, DunningStage.ReportsBlocked, due);
        var second = CatalogIntegrationStableEventIds.ForDunningStage(tenantId, DunningStage.ReportsBlocked, due);

        first.Should().Be(second);
        first.Should().NotBe(
            CatalogIntegrationStableEventIds.ForDunningStage(tenantId, DunningStage.LoginBlocked, due));
    }

    /// <summary>
    /// Factory de trial usa o routing key esperado.
    /// </summary>
    [Fact]
    public void TrialNoticeOutboxFactory_ProducesExpectedMessageTypeAndStableEventId()
    {
        var tenantId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
        var occurred = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);

        var line = CatalogIntegrationOutboxFactory.CreateTrialNoticeOutbox(
            tenantId,
            TrialNoticeType.TrialExpired,
            tenantId,
            occurred);

        line.IsSuccess.Should().BeTrue();
        line.Value.MessageType.Should().Be(CatalogIntegrationMessageTypes.TrialNoticeEmail);
        line.Value.EventId.Should().Be(
            CatalogIntegrationStableEventIds.ForTrialNotice(tenantId, TrialNoticeType.TrialExpired));
    }
}