using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Messaging;
using FluentAssertions;

namespace UnitTests.Backend.Messaging;

/// <summary>
/// Testes da fábrica de reprocessamento de dead-letter.
/// </summary>
public sealed class IntegrationDeadLetterReprocessFactoryTests
{
    /// <summary>Reprocessamento preserva correlação.</summary>
    [Fact]
    public void CreateReprocessLine_DeadLetter_SetsNewEventIdAndOriginalCorrelation()
    {
        var originalEventId = Guid.NewGuid();
        var envelopeJson = $$"""
            {
              "eventId":"{{originalEventId}}",
              "messageType":"catalog.tenant-welcome-email.v1",
              "correlationId":"{{originalEventId}}",
              "occurredOnUtc":"2026-01-01T00:00:00Z",
              "scope":"Catalog",
              "payload":{}
            }
            """;

        var dead = IntegrationOutboxMessage.Create(
            Guid.NewGuid(),
            originalEventId,
            "catalog.tenant-welcome-email.v1",
            envelopeJson,
            null,
            DateTime.UtcNow).Value;
        dead.MarkInProgress(DateTime.UtcNow);
        dead.MarkDeadLetter("Email.SendFailed");

        var result = IntegrationDeadLetterReprocessFactory.CreateReprocessLine(dead, DateTime.UtcNow);
        result.IsSuccess.Should().BeTrue();
        result.Value.EventId.Should().NotBe(originalEventId);
        result.Value.Status.Should().Be(IntegrationOutboxMessageStatus.Pending);

        var envelope = IntegrationEventEnvelopeSerializer.Deserialize(result.Value.PayloadJson, IntegrationEventScope.Catalog);
        envelope.IsSuccess.Should().BeTrue();
        envelope.Value.CorrelationId.Should().Be(originalEventId);
    }

    /// <summary>Somente dead-letter é elegível.</summary>
    [Fact]
    public void CreateReprocessLine_NonDeadLetter_Fails()
    {
        var line = IntegrationOutboxMessage.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "messaging.ping.v1",
            "{}",
            null).Value;

        IntegrationDeadLetterReprocessFactory.CreateReprocessLine(line, DateTime.UtcNow)
            .IsFailure.Should().BeTrue();
    }
}