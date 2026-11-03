using FluentAssertions;
using NSubstitute;
using SocialPublishing.Application.Posts.Commands.ApprovePost;
using SocialPublishing.Application.Posts.Commands.RejectPost;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Posts;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Application;

/// <summary>
/// Testes dos comandos <see cref="ApprovePostCommand"/> e <see cref="RejectPostCommand"/>.
/// </summary>
public sealed class ApprovePostCommandTests
{
    /// <summary>
    /// Valida permissão do approve.
    /// </summary>
    [Fact]
    public void ApproveCommand_DeveExigirSocialCopilotGenerate()
    {
        OrganicPostLifecycleCommandTestHost.AssertSocialCopilotPermission<ApprovePostCommand>();
    }

    /// <summary>
    /// PendingApproval deve ir para Approved sem agendar.
    /// </summary>
    [Fact]
    public async Task Approve_EmPendingApproval_DeveIrParaApproved()
    {
        var host = new OrganicPostLifecycleCommandTestHost();
        var post = host.SeedDraftPost();
        OrganicPostLifecycleCommandTestHost.AdvanceTo(post, OrganicPostStatus.PendingApproval);
        var handler = new ApprovePostCommandHandler(
            host.PostRepository,
            host.UnitOfWork,
            host.TenantContextAccessor,
            host.CurrentUserContext);

        var result = await handler.Handle(
            new ApprovePostCommand(host.WorkspaceId, post.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(OrganicPostStatus.Approved);
        post.Status.Should().Be(OrganicPostStatus.Approved);
        await host.UnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Approve a partir de Draft é inválido.
    /// </summary>
    [Fact]
    public async Task Approve_EmDraft_DeveRetornarInvalidTransition()
    {
        var host = new OrganicPostLifecycleCommandTestHost();
        var post = host.SeedDraftPost();
        var handler = new ApprovePostCommandHandler(
            host.PostRepository,
            host.UnitOfWork,
            host.TenantContextAccessor,
            host.CurrentUserContext);

        var result = await handler.Handle(
            new ApprovePostCommand(host.WorkspaceId, post.Id),
            CancellationToken.None);

        result.Error.Code.Should().Be(SocialErrors.InvalidTransition.Code);
    }

    /// <summary>
    /// Segundo approve falha.
    /// </summary>
    [Fact]
    public async Task Approve_SegundaVez_DeveRetornarInvalidTransition()
    {
        var host = new OrganicPostLifecycleCommandTestHost();
        var post = host.SeedDraftPost();
        OrganicPostLifecycleCommandTestHost.AdvanceTo(post, OrganicPostStatus.Approved);
        var handler = new ApprovePostCommandHandler(
            host.PostRepository,
            host.UnitOfWork,
            host.TenantContextAccessor,
            host.CurrentUserContext);

        var result = await handler.Handle(
            new ApprovePostCommand(host.WorkspaceId, post.Id),
            CancellationToken.None);

        result.Error.Code.Should().Be(SocialErrors.InvalidTransition.Code);
    }

    /// <summary>
    /// Reject em PendingApproval vai para Rejected.
    /// </summary>
    [Fact]
    public async Task Reject_EmPendingApproval_DeveIrParaRejected()
    {
        var host = new OrganicPostLifecycleCommandTestHost();
        var post = host.SeedDraftPost();
        OrganicPostLifecycleCommandTestHost.AdvanceTo(post, OrganicPostStatus.PendingApproval);
        var handler = new RejectPostCommandHandler(
            host.PostRepository,
            host.UnitOfWork,
            host.TenantContextAccessor,
            host.CurrentUserContext);

        var result = await handler.Handle(
            new RejectPostCommand(host.WorkspaceId, post.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(OrganicPostStatus.Rejected);
    }

    /// <summary>
    /// Reject fora de PendingApproval falha.
    /// </summary>
    [Fact]
    public async Task Reject_EmDraft_DeveRetornarInvalidTransition()
    {
        var host = new OrganicPostLifecycleCommandTestHost();
        var post = host.SeedDraftPost();
        var handler = new RejectPostCommandHandler(
            host.PostRepository,
            host.UnitOfWork,
            host.TenantContextAccessor,
            host.CurrentUserContext);

        var result = await handler.Handle(
            new RejectPostCommand(host.WorkspaceId, post.Id),
            CancellationToken.None);

        result.Error.Code.Should().Be(SocialErrors.InvalidTransition.Code);
    }
}
