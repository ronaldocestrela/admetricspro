using FluentAssertions;
using NSubstitute;
using SocialPublishing.Application.Posts.Commands.ReschedulePost;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Posts;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Application;

/// <summary>
/// Testes do comando <see cref="ReschedulePostCommand"/>.
/// </summary>
public sealed class ReschedulePostCommandTests
{
    /// <summary>
    /// Valida permissão exigida.
    /// </summary>
    [Fact]
    public void Command_DeveExigirSocialCopilotGenerate()
    {
        OrganicPostLifecycleCommandTestHost.AssertSocialCopilotPermission<ReschedulePostCommand>();
    }

    /// <summary>
    /// Reagendamento válido grava revisão e commita.
    /// </summary>
    [Theory]
    [InlineData(OrganicPostStatus.Draft)]
    [InlineData(OrganicPostStatus.PendingApproval)]
    [InlineData(OrganicPostStatus.Approved)]
    [InlineData(OrganicPostStatus.Scheduled)]
    public async Task Handle_NosStatusPermitidos_DeveGravarRevisao(OrganicPostStatus status)
    {
        var host = new OrganicPostLifecycleCommandTestHost();
        var post = host.SeedDraftPost();
        OrganicPostLifecycleCommandTestHost.AdvanceTo(post, status);
        var previous = post.ScheduledAtUtc;
        var newUtc = host.FixedTime.UtcDateTime.AddDays(7);
        var handler = CreateHandler(host);

        var result = await handler.Handle(
            new ReschedulePostCommand(host.WorkspaceId, post.Id, newUtc),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ScheduledAtUtc.Should().Be(newUtc);
        post.ScheduleRevisions.Should().ContainSingle();
        post.ScheduleRevisions[0].PreviousScheduledAtUtc.Should().Be(previous);
        post.ScheduleRevisions[0].RevisedByUserId.Should().Be(host.UserId);
        await host.UnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Publishing retorna SlotNotReschedulable.
    /// </summary>
    [Theory]
    [InlineData(OrganicPostStatus.Publishing)]
    [InlineData(OrganicPostStatus.Published)]
    [InlineData(OrganicPostStatus.Cancelled)]
    public async Task Handle_EmStatusBloqueado_DeveRetornarSlotNotReschedulable(OrganicPostStatus status)
    {
        var host = new OrganicPostLifecycleCommandTestHost();
        var post = host.SeedDraftPost();
        if (status == OrganicPostStatus.Cancelled)
        {
            post.Cancel().IsSuccess.Should().BeTrue();
        }
        else
        {
            OrganicPostLifecycleCommandTestHost.AdvanceTo(post, status);
        }

        var handler = CreateHandler(host);
        var result = await handler.Handle(
            new ReschedulePostCommand(host.WorkspaceId, post.Id, host.FixedTime.UtcDateTime.AddDays(1)),
            CancellationToken.None);

        result.Error.Code.Should().Be(SocialErrors.SlotNotReschedulable.Code);
        await host.UnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Rejected e Failed retornam InvalidTransition.
    /// </summary>
    [Theory]
    [InlineData(OrganicPostStatus.Rejected)]
    [InlineData(OrganicPostStatus.Failed)]
    public async Task Handle_EmStatusTerminalEditorial_DeveRetornarInvalidTransition(OrganicPostStatus status)
    {
        var host = new OrganicPostLifecycleCommandTestHost();
        var post = host.SeedDraftPost();
        if (status == OrganicPostStatus.Rejected)
        {
            OrganicPostLifecycleCommandTestHost.AdvanceTo(post, OrganicPostStatus.PendingApproval);
            post.Reject().IsSuccess.Should().BeTrue();
        }
        else
        {
            OrganicPostLifecycleCommandTestHost.AdvanceTo(post, OrganicPostStatus.Publishing);
            post.MarkFailed().IsSuccess.Should().BeTrue();
        }

        var handler = CreateHandler(host);
        var result = await handler.Handle(
            new ReschedulePostCommand(host.WorkspaceId, post.Id, host.FixedTime.UtcDateTime.AddDays(1)),
            CancellationToken.None);

        result.Error.Code.Should().Be(SocialErrors.InvalidTransition.Code);
    }

    /// <summary>
    /// Scheduled no passado retorna ScheduleInThePast.
    /// </summary>
    [Fact]
    public async Task Handle_ScheduledNoPassado_DeveRetornarScheduleInThePast()
    {
        var host = new OrganicPostLifecycleCommandTestHost();
        var post = host.SeedDraftPost();
        OrganicPostLifecycleCommandTestHost.AdvanceTo(post, OrganicPostStatus.Scheduled);
        var past = host.FixedTime.UtcDateTime.AddDays(-1);
        var handler = CreateHandler(host);

        var result = await handler.Handle(
            new ReschedulePostCommand(host.WorkspaceId, post.Id, past),
            CancellationToken.None);

        result.Error.Code.Should().Be(SocialErrors.ScheduleInThePast.Code);
        post.ScheduleRevisions.Should().BeEmpty();
    }

    /// <summary>
    /// Draft aceita horário no passado.
    /// </summary>
    [Fact]
    public async Task Handle_DraftNoPassado_DeveTerSucesso()
    {
        var host = new OrganicPostLifecycleCommandTestHost();
        var post = host.SeedDraftPost();
        var past = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var handler = CreateHandler(host);

        var result = await handler.Handle(
            new ReschedulePostCommand(host.WorkspaceId, post.Id, past),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ScheduledAtUtc.Should().Be(past);
    }

    private static ReschedulePostCommandHandler CreateHandler(OrganicPostLifecycleCommandTestHost host) =>
        new(
            host.PostRepository,
            host.UnitOfWork,
            host.TenantContextAccessor,
            host.CurrentUserContext,
            host.TimeProvider);
}
