using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Xunit;

namespace UnitTests.Backend.Messaging;

/// <summary>
/// Testes das transições de estado de <see cref="IntegrationOutboxMessage"/>.
/// </summary>
public sealed class IntegrationOutboxMessageStateTests
{
    private static readonly DateTime FixedUtc = new(2026, 11, 25, 12, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(5);

    private static IntegrationOutboxMessage CreatePending() =>
        IntegrationOutboxMessage.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "messaging.ping.v1",
            """{"ping":true}""",
            Guid.NewGuid(),
            FixedUtc).Value;

    /// <summary>
    /// Pending vencido deve permitir claim.
    /// </summary>
    [Fact]
    public void MarkInProgress_DePendingVencido_DeveTransicionar()
    {
        var message = CreatePending();

        var result = message.MarkInProgress(FixedUtc);

        result.IsSuccess.Should().BeTrue();
        message.Status.Should().Be(IntegrationOutboxMessageStatus.InProgress);
        message.InProgressSinceUtc.Should().Be(FixedUtc);
    }

    /// <summary>
    /// Pending com NextAttemptAtUtc no futuro não pode ser claimado.
    /// </summary>
    [Fact]
    public void MarkInProgress_ComBackoffNaoVencido_DeveFalhar()
    {
        var message = CreatePending();
        message.Requeue(FixedUtc.AddMinutes(10), "Messaging.PublishFailed", FixedUtc);

        var result = message.MarkInProgress(FixedUtc);

        result.IsFailure.Should().BeTrue();
        message.Status.Should().Be(IntegrationOutboxMessageStatus.Pending);
    }

    /// <summary>
    /// Published só a partir de InProgress.
    /// </summary>
    [Fact]
    public void MarkPublished_DeInProgress_DeveTransicionar()
    {
        var message = CreatePending();
        message.MarkInProgress(FixedUtc);

        var result = message.MarkPublished();

        result.IsSuccess.Should().BeTrue();
        message.Status.Should().Be(IntegrationOutboxMessageStatus.Published);
        message.InProgressSinceUtc.Should().BeNull();
        message.NextAttemptAtUtc.Should().BeNull();
    }

    /// <summary>
    /// Requeue incrementa tentativas e volta a Pending.
    /// </summary>
    [Fact]
    public void Requeue_DeInProgress_DeveReagendar()
    {
        var message = CreatePending();
        message.MarkInProgress(FixedUtc);
        var nextAttempt = FixedUtc.AddSeconds(30);

        var result = message.Requeue(nextAttempt, "Messaging.PublishFailed", FixedUtc);

        result.IsSuccess.Should().BeTrue();
        message.Status.Should().Be(IntegrationOutboxMessageStatus.Pending);
        message.AttemptCount.Should().Be(1);
        message.NextAttemptAtUtc.Should().Be(nextAttempt);
        message.LastErrorCode.Should().Be("Messaging.PublishFailed");
        message.InProgressSinceUtc.Should().BeNull();
    }

    /// <summary>
    /// Reclaim devolve InProgress antigo a Pending.
    /// </summary>
    [Fact]
    public void Reclaim_InProgressExpirado_DeveVoltarPending()
    {
        var message = CreatePending();
        var claimedAt = FixedUtc.AddMinutes(-10);
        message.MarkInProgress(claimedAt);

        var result = message.Reclaim(claimedAt.Add(Timeout), Timeout);

        result.IsSuccess.Should().BeTrue();
        message.Status.Should().Be(IntegrationOutboxMessageStatus.Pending);
        message.InProgressSinceUtc.Should().BeNull();
    }

    /// <summary>
    /// InProgress recente não é reclaimado.
    /// </summary>
    [Fact]
    public void Reclaim_InProgressRecente_DeveFalhar()
    {
        var message = CreatePending();
        message.MarkInProgress(FixedUtc);

        var result = message.Reclaim(FixedUtc.AddMinutes(1), Timeout);

        result.IsFailure.Should().BeTrue();
        message.Status.Should().Be(IntegrationOutboxMessageStatus.InProgress);
    }

    /// <summary>
    /// Dead letter a partir de Pending ou InProgress.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MarkDeadLetter_DeveTransicionar(bool claimFirst)
    {
        var message = CreatePending();
        if (claimFirst)
        {
            message.MarkInProgress(FixedUtc);
        }

        var result = message.MarkDeadLetter("Messaging.PayloadInvalid");

        result.IsSuccess.Should().BeTrue();
        message.Status.Should().Be(IntegrationOutboxMessageStatus.DeadLetter);
        message.LastErrorCode.Should().Be("Messaging.PayloadInvalid");
    }
}