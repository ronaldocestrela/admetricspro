using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Messaging;
using FluentAssertions;
using Xunit;

namespace UnitTests.Backend.Messaging;

/// <summary>
/// Testes de <see cref="UserInitiatedJobOutboxFactory"/>.
/// </summary>
public sealed class UserInitiatedJobOutboxFactoryTests
{
    private static readonly DateTime FixedUtc = new(2026, 11, 25, 16, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Create deve produzir outbox operacional com tenant e job.
    /// </summary>
    [Fact]
    public void Create_ComParametrosValidos_DeveProduzirOutbox()
    {
        var tenantId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var result = UserInitiatedJobOutboxFactory.Create(
            tenantId,
            jobId,
            eventId,
            UserInitiatedJobIntegrationMessageTypes.PostImageGenerate,
            FixedUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.MessageType.Should().Be(UserInitiatedJobIntegrationMessageTypes.PostImageGenerate);
        result.Value.TenantId.Should().Be(tenantId);
        result.Value.EventId.Should().Be(eventId);
        result.Value.Status.Should().Be(IntegrationOutboxMessageStatus.Pending);
        result.Value.PayloadJson.Should().Contain(jobId.ToString());
    }
}