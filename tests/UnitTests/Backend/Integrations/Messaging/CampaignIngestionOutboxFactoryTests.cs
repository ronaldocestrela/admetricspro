using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Campaigns;
using FluentAssertions;
using Integrations.Application.Messaging;

namespace UnitTests.Backend.Integrations.Messaging;

/// <summary>
/// Testes do factory de outbox de ingestão.
/// </summary>
public sealed class CampaignIngestionOutboxFactoryTests
{
    /// <summary>
    /// Tipos versionados de métricas e hierarquia.
    /// </summary>
    [Fact]
    public void Create_ShouldUseStableMessageTypes()
    {
        var tenantId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var syncTime = DateTime.UtcNow;

        var metrics = CampaignIngestionOutboxFactory.CreateMetricsSynced(
            tenantId, workspaceId, accountId, "MetaAds", MetricGranularity.Daily, syncTime.AddDays(-1), syncTime, syncTime);

        var hierarchy = CampaignIngestionOutboxFactory.CreateHierarchySynced(
            tenantId, workspaceId, accountId, "MetaAds", syncTime);

        metrics.Value.MessageType.Should().Be(IntegrationsIntegrationMessageTypes.CampaignMetricsSynced);
        hierarchy.Value.MessageType.Should().Be(IntegrationsIntegrationMessageTypes.CampaignHierarchySynced);
        metrics.Value.TenantId.Should().Be(tenantId);
        hierarchy.Value.TenantId.Should().Be(tenantId);
    }
}