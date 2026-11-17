using FluentAssertions;
using NSubstitute;
using SocialPublishing.Application.Posts.Commands.ApprovePost;
using SocialPublishing.Application.Posts.Commands.SchedulePost;
using SocialPublishing.Application.Posts.Commands.SubmitPostForApproval;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Posts;
using SocialPublishing.Domain.Publications;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Application;

/// <summary>
/// Testes do comando <see cref="SchedulePostCommand"/>.
/// </summary>
public sealed class SchedulePostCommandTests
{
    /// <summary>
    /// Valida permissão exigida.
    /// </summary>
    [Fact]
    public void Command_DeveExigirSocialCopilotGenerate()
    {
        OrganicPostLifecycleCommandTestHost.AssertSocialCopilotPermission<SchedulePostCommand>();
    }

    /// <summary>
    /// Fluxo submit → approve → schedule cria job Pending e um commit.
    /// </summary>
    [Fact]
    public async Task Handle_FluxoCompleto_DeveCriarJobPendingEUmCommit()
    {
        var host = new OrganicPostLifecycleCommandTestHost();
        var post = host.SeedDraftPost();
        await new SubmitPostForApprovalCommandHandler(
            host.PostRepository,
            host.UnitOfWork,
            host.TenantContextAccessor,
            host.CurrentUserContext)
            .Handle(new SubmitPostForApprovalCommand(host.WorkspaceId, post.Id), CancellationToken.None);
        await new ApprovePostCommandHandler(
            host.PostRepository,
            host.UnitOfWork,
            host.TenantContextAccessor,
            host.CurrentUserContext)
            .Handle(new ApprovePostCommand(host.WorkspaceId, post.Id), CancellationToken.None);
        host.PublicationJobRepository.AllJobs.Should().BeEmpty();

        var scheduleHandler = CreateScheduleHandler(host);
        var result = await scheduleHandler.Handle(
            new SchedulePostCommand(host.WorkspaceId, post.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(OrganicPostStatus.Scheduled);
        host.PublicationJobRepository.AllJobs.Should().ContainSingle();
        host.PublicationJobRepository.AllJobs[0].Status.Should().Be(PublicationJobStatus.Pending);
        host.PublicationJobRepository.AllJobs[0].IdempotencyKey
            .Should().Be(PublicationJobKey.Build(post.Id, post.ScheduledAtUtc));
        await host.UnitOfWork.Received(3).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Approve sozinho não cria job.
    /// </summary>
    [Fact]
    public void Approve_Sozinho_NaoCriaJob()
    {
        var host = new OrganicPostLifecycleCommandTestHost();
        var post = host.SeedDraftPost();
        OrganicPostLifecycleCommandTestHost.AdvanceTo(post, OrganicPostStatus.Approved);

        host.PublicationJobRepository.AllJobs.Should().BeEmpty();
    }

    /// <summary>
    /// Schedule a partir de Draft retorna InvalidTransition sem job.
    /// </summary>
    [Fact]
    public async Task Handle_EmDraft_DeveRetornarInvalidTransitionSemJob()
    {
        var host = new OrganicPostLifecycleCommandTestHost();
        var post = host.SeedDraftPost();
        var handler = CreateScheduleHandler(host);

        var result = await handler.Handle(
            new SchedulePostCommand(host.WorkspaceId, post.Id),
            CancellationToken.None);

        result.Error.Code.Should().Be(SocialErrors.InvalidTransition.Code);
        host.PublicationJobRepository.AllJobs.Should().BeEmpty();
        await host.UnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    private static SchedulePostCommandHandler CreateScheduleHandler(OrganicPostLifecycleCommandTestHost host) =>
        new(
            host.PostRepository,
            host.PublicationJobRepository,
            host.UnitOfWork,
            host.TenantContextAccessor,
            host.CurrentUserContext,
            host.TimeProvider);
}
