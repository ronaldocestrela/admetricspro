using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Messaging;
using FluentAssertions;
using NSubstitute;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Jobs.Commands.AcceptGeneratePostImage;
using SocialPublishing.Domain.Posts;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Application;

/// <summary>
/// Testes de aceite assíncrono de geração de imagem (Fase 8).
/// </summary>
public sealed class AcceptGeneratePostImageCommandTests
{
    private readonly ISocialUserInitiatedJobCommitter _committer = Substitute.For<ISocialUserInitiatedJobCommitter>();

    /// <summary>
    /// Post aprovado deve gravar job Accepted sem invocar gerador de imagem.
    /// </summary>
    [Fact]
    public async Task Handle_ComPostAprovado_DeveGravarJobSemChamarIa()
    {
        var host = new OrganicPostLifecycleCommandTestHost();
        var post = host.SeedDraftPost();
        OrganicPostLifecycleCommandTestHost.AdvanceTo(post, OrganicPostStatus.Approved);

        _committer.CommitAcceptedJobAsync(
                Arg.Any<UserInitiatedJob>(),
                Arg.Any<IntegrationOutboxMessage>(),
                Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result.Success());

        var handler = new AcceptGeneratePostImageCommandHandler(
            host.PostRepository,
            _committer,
            host.TenantContextAccessor,
            host.CurrentUserContext,
            host.TimeProvider);

        var result = await handler.Handle(
            new AcceptGeneratePostImageCommand(host.WorkspaceId, post.Id, "tom escuro"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(UserInitiatedJobStatus.Accepted);
        result.Value.Kind.Should().Be(UserInitiatedJobKind.PostImageGenerate);
        await _committer.Received(1).CommitAcceptedJobAsync(
            Arg.Is<UserInitiatedJob>(j => j.Status == UserInitiatedJobStatus.Accepted),
            Arg.Is<IntegrationOutboxMessage>(m =>
                m.MessageType == UserInitiatedJobIntegrationMessageTypes.PostImageGenerate),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Post em Draft deve falhar sem gravar job.
    /// </summary>
    [Fact]
    public async Task Handle_EmDraft_NaoDeveGravarJob()
    {
        var host = new OrganicPostLifecycleCommandTestHost();
        var post = host.SeedDraftPost();

        var handler = new AcceptGeneratePostImageCommandHandler(
            host.PostRepository,
            _committer,
            host.TenantContextAccessor,
            host.CurrentUserContext,
            host.TimeProvider);

        var result = await handler.Handle(
            new AcceptGeneratePostImageCommand(host.WorkspaceId, post.Id, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _committer.DidNotReceive().CommitAcceptedJobAsync(
            Arg.Any<UserInitiatedJob>(),
            Arg.Any<IntegrationOutboxMessage>(),
            Arg.Any<CancellationToken>());
    }
}