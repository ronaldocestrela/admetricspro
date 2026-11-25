using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Xunit;

namespace UnitTests.Backend.Messaging;

/// <summary>
/// Testes da política pura de ack <see cref="IntegrationResultDisposition"/>.
/// </summary>
public sealed class IntegrationResultDispositionTests
{
    /// <summary>
    /// Sucesso deve produzir Acknowledge.
    /// </summary>
    [Fact]
    public void ForResult_Sucesso_DeveAcknowledge()
    {
        var disposition = IntegrationResultDisposition.ForResult(Result.Success(), alreadyInInbox: false);

        disposition.Action.Should().Be(IntegrationMessageDispositionAction.Acknowledge);
    }

    /// <summary>
    /// Validation, NotFound, Conflict e Unauthorized devem dar ack sem retry.
    /// </summary>
    [Theory]
    [InlineData(ErrorType.Validation)]
    [InlineData(ErrorType.NotFound)]
    [InlineData(ErrorType.Conflict)]
    [InlineData(ErrorType.Unauthorized)]
  [InlineData(ErrorType.Forbidden)]
    public void ForResult_ErrosPermanentes_DevemAcknowledge(ErrorType errorType)
    {
        var error = new Error("Test.Permanent", "permanent", errorType);
        var disposition = IntegrationResultDisposition.ForResult(Result.Failure(error), alreadyInInbox: false);

        disposition.Action.Should().Be(IntegrationMessageDispositionAction.Acknowledge);
    }

    /// <summary>
    /// Failure genérico sem marcador transitório deve dar ack (falha de negócio, sem retry).
    /// </summary>
    [Fact]
    public void ForResult_FailureSemMarcadorTransitório_DeveAcknowledge()
    {
        var error = Error.Failure("Business.Rule", "regra violada");
        var disposition = IntegrationResultDisposition.ForResult(Result.Failure(error), alreadyInInbox: false);

        disposition.Action.Should().Be(IntegrationMessageDispositionAction.Acknowledge);
    }

    /// <summary>
    /// Falha transitória explícita deve reagendar via outbox.
    /// </summary>
    [Fact]
    public void ForResult_FalhaTransitoriaExplicita_DeveReschedule()
    {
        var retryAt = DateTimeOffset.UtcNow.AddMinutes(5);
        var error = new TestTransientError("Rate limited", retryAt);
        var disposition = IntegrationResultDisposition.ForResult(Result.Failure(error), alreadyInInbox: false);

        disposition.Action.Should().Be(IntegrationMessageDispositionAction.Reschedule);
        disposition.NextAttemptAtUtc.Should().Be(retryAt);
    }

    /// <summary>
    /// EventId já na inbox deve dar ack imediato, mesmo com falha transitória.
    /// </summary>
    [Fact]
    public void ForResult_InboxDuplicada_DeveAcknowledgeMesmoComFalhaTransitoria()
    {
        var error = new TestTransientError("Rate limited", DateTimeOffset.UtcNow.AddMinutes(1));
        var disposition = IntegrationResultDisposition.ForResult(Result.Failure(error), alreadyInInbox: true);

        disposition.Action.Should().Be(IntegrationMessageDispositionAction.Acknowledge);
    }

    /// <summary>
    /// Exceção inesperada deve solicitar retry no transporte.
    /// </summary>
    [Fact]
    public void ForUnexpectedFailure_DeveRetryTransport()
    {
        var disposition = IntegrationResultDisposition.ForUnexpectedFailure();

        disposition.Action.Should().Be(IntegrationMessageDispositionAction.RetryTransport);
    }

    private sealed record TestTransientError(string Description, DateTimeOffset? NextAttemptAtUtc)
        : Error("Test.Transient", Description, ErrorType.Failure), ITransientIntegrationFailure;
}