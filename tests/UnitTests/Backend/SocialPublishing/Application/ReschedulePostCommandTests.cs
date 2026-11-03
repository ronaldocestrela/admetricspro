using FluentAssertions;
using NSubstitute;
using SocialPublishing.Application.Posts.Commands.ReschedulePost;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Posts;
using SocialPublishing.Domain.Publications;
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

    /// <summary>
    /// Reagendar Scheduled cancela job anterior e abre outro com chave nova.
    /// </summary>
    [Fact]
    public async Task Handle_Scheduled_DeveCancelarJobAnteriorEAbrirNovo()
    {
        var host = new OrganicPostLifecycleCommandTestHost();
        var post = host.SeedDraftPost();
        OrganicPostLifecycleCommandTestHost.AdvanceTo(post, OrganicPostStatus.Scheduled);
        var oldKey = PublicationJobKey.Build(post.Id, post.ScheduledAtUtc);
        var oldJob = PublicationJob.Open(
            post.Id,
            post.WorkspaceId,
            post.ScheduledAtUtc,
            host.FixedTime.UtcDateTime).Value;
        await host.PublicationJobRepository.AddAsync(oldJob, CancellationToken.None);

        var newUtc = host.FixedTime.UtcDateTime.AddDays(7);
        var handler = CreateHandler(host);
        var result = await handler.Handle(
            new ReschedulePostCommand(host.WorkspaceId, post.Id, newUtc),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        oldJob.Status.Should().Be(PublicationJobStatus.Cancelled);
        host.PublicationJobRepository.AllJobs.Should().Contain(j =>
            j.Status == PublicationJobStatus.Pending &&
            j.IdempotencyKey == PublicationJobKey.Build(post.Id, newUtc));
        host.PublicationJobRepository.AllJobs.Count(j => j.IdempotencyKey == oldKey).Should().Be(1);
    }

    /// <summary>
    /// Job InProgress bloqueia reagendamento.
    /// </summary>
    [Fact]
    public async Task Handle_ScheduledComJobInProgress_DeveRetornarPublicationJobInProgress()
    {
        var host = new OrganicPostLifecycleCommandTestHost();
        var post = host.SeedDraftPost();
        OrganicPostLifecycleCommandTestHost.AdvanceTo(post, OrganicPostStatus.Scheduled);
        var job = PublicationJob.Open(
            post.Id,
            post.WorkspaceId,
            post.ScheduledAtUtc,
            host.FixedTime.UtcDateTime).Value;
        job.MarkInProgress().IsSuccess.Should().BeTrue();
        await host.PublicationJobRepository.AddAsync(job, CancellationToken.None);

        var handler = CreateHandler(host);
        var result = await handler.Handle(
            new ReschedulePostCommand(host.WorkspaceId, post.Id, host.FixedTime.UtcDateTime.AddDays(1)),
            CancellationToken.None);

        result.Error.Code.Should().Be(SocialErrors.PublicationJobInProgress.Code);
        await host.UnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Draft reagendado não altera outbox.
    /// </summary>
    [Fact]
    public async Task Handle_Draft_NaoDeveCriarJob()
    {
        var host = new OrganicPostLifecycleCommandTestHost();
        var post = host.SeedDraftPost();
        var handler = CreateHandler(host);

        var result = await handler.Handle(
            new ReschedulePostCommand(host.WorkspaceId, post.Id, host.FixedTime.UtcDateTime.AddDays(3)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        host.PublicationJobRepository.AllJobs.Should().BeEmpty();
    }

    private static ReschedulePostCommandHandler CreateHandler(OrganicPostLifecycleCommandTestHost host) =>
        new(
            host.PostRepository,
            host.PublicationJobRepository,
            host.UnitOfWork,
            host.TenantContextAccessor,
            host.CurrentUserContext,
            host.TimeProvider);
}
