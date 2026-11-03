using FluentAssertions;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Publications;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Domain;

/// <summary>
/// Testes do agregado <see cref="PublicationJob"/>.
/// </summary>
public sealed class PublicationJobTests
{
    private static readonly DateTime CreatedAt = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime SlotUtc = new(2026, 7, 1, 14, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Open deve produzir Pending e chave estável.
    /// </summary>
    [Fact]
    public void Open_DeveProduzirPendingComChaveIdempotencia()
    {
        var postId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var expectedKey = PublicationJobKey.Build(postId, SlotUtc);

        var result = PublicationJob.Open(postId, workspaceId, SlotUtc, CreatedAt);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(PublicationJobStatus.Pending);
        result.Value.IdempotencyKey.Should().Be(expectedKey);
        result.Value.CanBeClaimed.Should().BeTrue();
    }

    /// <summary>
    /// Cancel seguido de Open gera chave nova; job anterior não é reivindicável.
    /// </summary>
    [Fact]
    public void CancelEOpen_DeveInvalidarClaimDoJobAnterior()
    {
        var postId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var first = PublicationJob.Open(postId, workspaceId, SlotUtc, CreatedAt).Value;
        first.Cancel().IsSuccess.Should().BeTrue();
        first.CanBeClaimed.Should().BeFalse();

        var newSlot = SlotUtc.AddDays(1);
        var second = PublicationJob.Open(postId, workspaceId, newSlot, CreatedAt.AddHours(1)).Value;
        second.IdempotencyKey.Should().NotBe(first.IdempotencyKey);
        second.CanBeClaimed.Should().BeTrue();
    }

    /// <summary>
    /// Succeeded não pode ser reivindicado.
    /// </summary>
    [Fact]
    public void Succeeded_NaoDeveSerReivindicavel()
    {
        var job = PublicationJob.Open(Guid.NewGuid(), Guid.NewGuid(), SlotUtc, CreatedAt).Value;
        job.MarkInProgress().IsSuccess.Should().BeTrue();
        job.MarkSucceeded().IsSuccess.Should().BeTrue();

        job.CanBeClaimed.Should().BeFalse();
        job.MarkInProgress().Error.Code.Should().Be(SocialErrors.PublicationJobNotClaimable.Code);
    }

    /// <summary>
    /// Transição inválida retorna Result.Failure.
    /// </summary>
    [Fact]
    public void MarkDeadLetter_EmPending_DeveFalhar()
    {
        var job = PublicationJob.Open(Guid.NewGuid(), Guid.NewGuid(), SlotUtc, CreatedAt).Value;

        job.MarkDeadLetter().Error.Code.Should().Be(SocialErrors.PublicationJobInvalidTransition.Code);
    }
}
