using BuildingBlocks.Application.Messaging;
using FluentAssertions;
using SocialPublishing.Application.Messaging;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Messaging;

/// <summary>
/// Testes da fábrica de outbox de publicação orgânica.
/// </summary>
public sealed class PublishOrganicSlotOutboxFactoryTests
{
    private static readonly DateTime FixedUtc = new(2026, 7, 1, 14, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Deve usar o routing key versionado e payload mínimo.
    /// </summary>
    [Fact]
    public void Create_DeveGerarTipoVersionadoEPayloadSemSegredos()
    {
        var tenantId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        const string key = "post:slot";

        var result = PublishOrganicSlotOutboxFactory.Create(tenantId, jobId, key, FixedUtc, FixedUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.MessageType.Should().Be(SocialPublishingIntegrationMessageTypes.PublishOrganicSlot);
        result.Value.TenantId.Should().Be(tenantId);

        var envelope = IntegrationEventEnvelopeSerializer.Deserialize(result.Value.PayloadJson);
        envelope.IsSuccess.Should().BeTrue();
        envelope.Value.Payload.GetProperty("publicationJobId").GetGuid().Should().Be(jobId);
        envelope.Value.Payload.GetProperty("claimedAtUtc").GetDateTime().Should().Be(FixedUtc);
        envelope.Value.Payload.GetProperty("idempotencyKey").GetString().Should().Be(key);
    }
}