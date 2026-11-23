using FluentAssertions;
using NSubstitute;
using SocialPublishing.Application.Posts.Commands.ReviseOrganicPostCaption;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Posts;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Application;

/// <summary>
/// Testes do comando <see cref="ReviseOrganicPostCaptionCommand"/>.
/// </summary>
public sealed class ReviseOrganicPostCaptionCommandTests
{
    /// <summary>
    /// Valida permissão exigida no comando.
    /// </summary>
    [Fact]
    public void Command_DeveExigirSocialCopilotGenerate()
    {
        OrganicPostLifecycleCommandTestHost.AssertSocialCopilotPermission<ReviseOrganicPostCaptionCommand>();
    }

    /// <summary>
    /// ChangesRequested deve persistir nova legenda sem alterar status.
    /// </summary>
    [Fact]
    public async Task Handle_EmChangesRequested_DeveAtualizarLegendaEPersistir()
    {
        var host = new OrganicPostLifecycleCommandTestHost();
        var post = host.SeedDraftPost();
        post.SubmitForApproval().IsSuccess.Should().BeTrue();
        post.RequestChanges("Ajustar CTA").IsSuccess.Should().BeTrue();
        var handler = new ReviseOrganicPostCaptionCommandHandler(
            host.PostRepository,
            host.UnitOfWork,
            host.TenantContextAccessor,
            host.CurrentUserContext);

        var result = await handler.Handle(
            new ReviseOrganicPostCaptionCommand(host.WorkspaceId, post.Id, "Legenda revisada"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Caption.Should().Be("Legenda revisada");
        result.Value.Status.Should().Be(OrganicPostStatus.ChangesRequested);
        await host.UnitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Status não editável não commita.
    /// </summary>
    [Fact]
    public async Task Handle_EmApproved_DeveRetornarInvalidTransitionSemCommit()
    {
        var host = new OrganicPostLifecycleCommandTestHost();
        var post = host.SeedDraftPost();
        OrganicPostLifecycleCommandTestHost.AdvanceTo(post, OrganicPostStatus.Approved);
        var handler = new ReviseOrganicPostCaptionCommandHandler(
            host.PostRepository,
            host.UnitOfWork,
            host.TenantContextAccessor,
            host.CurrentUserContext);

        var result = await handler.Handle(
            new ReviseOrganicPostCaptionCommand(host.WorkspaceId, post.Id, "Nova legenda"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.InvalidTransition.Code);
        await host.UnitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }
}