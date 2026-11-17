using FluentAssertions;
using NSubstitute;
using SocialPublishing.Application.Posts.Commands.SubmitPostForApproval;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Posts;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Application;

/// <summary>
/// Testes do comando <see cref="SubmitPostForApprovalCommand"/>.
/// </summary>
public sealed class SubmitPostForApprovalCommandTests
{
    /// <summary>
    /// Valida permissão exigida no comando.
    /// </summary>
    [Fact]
    public void Command_DeveExigirSocialCopilotGenerate()
    {
        OrganicPostLifecycleCommandTestHost.AssertSocialCopilotPermission<SubmitPostForApprovalCommand>();
    }

    /// <summary>
    /// Draft deve ir para PendingApproval com commit.
    /// </summary>
    [Fact]
    public async Task Handle_EmDraft_DeveTransicionarEPersistir()
    {
        var host = new OrganicPostLifecycleCommandTestHost();
        var post = host.SeedDraftPost();
        var handler = new SubmitPostForApprovalCommandHandler(
            host.PostRepository,
            host.UnitOfWork,
            host.TenantContextAccessor,
            host.CurrentUserContext);

        var result = await handler.Handle(
            new SubmitPostForApprovalCommand(host.WorkspaceId, post.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(OrganicPostStatus.PendingApproval);
        await host.UnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Transição ilegal não commita.
    /// </summary>
    [Fact]
    public async Task Handle_EmApproved_DeveRetornarInvalidTransitionSemCommit()
    {
        var host = new OrganicPostLifecycleCommandTestHost();
        var post = host.SeedDraftPost();
        OrganicPostLifecycleCommandTestHost.AdvanceTo(post, OrganicPostStatus.Approved);
        var handler = new SubmitPostForApprovalCommandHandler(
            host.PostRepository,
            host.UnitOfWork,
            host.TenantContextAccessor,
            host.CurrentUserContext);

        var result = await handler.Handle(
            new SubmitPostForApprovalCommand(host.WorkspaceId, post.Id),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.InvalidTransition.Code);
        await host.UnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Workspace vazio falha antes do repositório.
    /// </summary>
    [Fact]
    public async Task Handle_ComWorkspaceVazio_NaoAcessaRepositorio()
    {
        var host = new OrganicPostLifecycleCommandTestHost();
        var handler = new SubmitPostForApprovalCommandHandler(
            host.PostRepository,
            host.UnitOfWork,
            host.TenantContextAccessor,
            host.CurrentUserContext);

        var result = await handler.Handle(
            new SubmitPostForApprovalCommand(Guid.Empty, Guid.NewGuid()),
            CancellationToken.None);

        result.Error.Code.Should().Be(SocialErrors.WorkspaceRequired.Code);
        await host.UnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Post ausente ou de outro workspace retorna PostNotFound.
    /// </summary>
    [Fact]
    public async Task Handle_ComPostDeOutroWorkspace_DeveRetornarPostNotFound()
    {
        var host = new OrganicPostLifecycleCommandTestHost();
        var post = host.SeedDraftPost(Guid.NewGuid());
        var handler = new SubmitPostForApprovalCommandHandler(
            host.PostRepository,
            host.UnitOfWork,
            host.TenantContextAccessor,
            host.CurrentUserContext);

        var result = await handler.Handle(
            new SubmitPostForApprovalCommand(host.WorkspaceId, post.Id),
            CancellationToken.None);

        result.Error.Code.Should().Be(SocialErrors.PostNotFound.Code);
    }
}
