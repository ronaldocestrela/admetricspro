using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Xunit;

namespace UnitTests.Backend.Messaging;

/// <summary>
/// Testes da entidade <see cref="IntegrationInboxMessage"/>.
/// </summary>
public sealed class IntegrationInboxMessageTests
{
    private static readonly Guid InboxId = Guid.Parse("b2c3d4e5-f6a7-4890-b123-456789abcdef");
    private static readonly Guid EventId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
    private const string ConsumerName = "messaging.ping";

    /// <summary>
    /// Create válido produz registro com instante de processamento.
    /// </summary>
    [Fact]
    public void Create_Valido_DeveProduzirRegistro()
    {
        var processedAt = new DateTime(2026, 11, 25, 18, 0, 0, DateTimeKind.Utc);

        var result = IntegrationInboxMessage.Create(InboxId, EventId, ConsumerName, processedAt);

        result.IsSuccess.Should().BeTrue();
        var row = result.Value;
        row.Id.Should().Be(InboxId);
        row.EventId.Should().Be(EventId);
        row.ConsumerName.Should().Be(ConsumerName);
        row.ProcessedAtUtc.Should().Be(processedAt);
    }

    /// <summary>
    /// EventId vazio deve falhar.
    /// </summary>
    [Fact]
    public void Create_ComEventIdVazio_DeveFalhar()
    {
        var result = IntegrationInboxMessage.Create(
            InboxId,
            Guid.Empty,
            ConsumerName,
            DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    /// <summary>
    /// Nome de consumidor em branco deve falhar.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_ComConsumidorEmBranco_DeveFalhar(string consumerName)
    {
        var result = IntegrationInboxMessage.Create(
            InboxId,
            EventId,
            consumerName,
            DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }
}