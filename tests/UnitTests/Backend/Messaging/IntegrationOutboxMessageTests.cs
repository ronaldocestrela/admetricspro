using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Xunit;

namespace UnitTests.Backend.Messaging;

/// <summary>
/// Testes da entidade <see cref="IntegrationOutboxMessage"/>.
/// </summary>
public sealed class IntegrationOutboxMessageTests
{
    private static readonly Guid MessageId = Guid.Parse("a1b2c3d4-e5f6-4789-a012-3456789abcde");
    private static readonly Guid EventId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
    private static readonly Guid? TenantId = Guid.Parse("2f8b3c1a-9d4e-4b7c-a1e2-3f4d5e6a7b8c");
    private const string MessageType = "integrations.campaign-metrics-synced.v1";
    private const string PayloadJson = """{"platform":"Meta"}""";

    /// <summary>
    /// Create válido produz linha Pending com contadores zerados.
    /// </summary>
    [Fact]
    public void Create_Valido_DeveProduzirPending()
    {
        var result = IntegrationOutboxMessage.Create(
            MessageId,
            EventId,
            MessageType,
            PayloadJson,
            TenantId);

        result.IsSuccess.Should().BeTrue();
        var message = result.Value;
        message.Id.Should().Be(MessageId);
        message.EventId.Should().Be(EventId);
        message.MessageType.Should().Be(MessageType);
        message.PayloadJson.Should().Be(PayloadJson);
        message.TenantId.Should().Be(TenantId);
        message.Status.Should().Be(IntegrationOutboxMessageStatus.Pending);
        message.AttemptCount.Should().Be(0);
        message.NextAttemptAtUtc.Should().BeNull();
        message.RowVersion.Should().BeEmpty();
    }

    /// <summary>
    /// EventId vazio deve falhar com validação.
    /// </summary>
    [Fact]
    public void Create_ComEventIdVazio_DeveFalhar()
    {
        var result = IntegrationOutboxMessage.Create(
            MessageId,
            Guid.Empty,
            MessageType,
            PayloadJson,
            TenantId);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    /// <summary>
    /// Tipo em branco deve falhar com validação.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_ComTipoEmBranco_DeveFalhar(string messageType)
    {
        var result = IntegrationOutboxMessage.Create(
            MessageId,
            EventId,
            messageType,
            PayloadJson,
            TenantId);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }
}