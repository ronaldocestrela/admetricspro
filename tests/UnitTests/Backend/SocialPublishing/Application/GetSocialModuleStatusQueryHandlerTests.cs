using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using NSubstitute;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Status.DTOs;
using SocialPublishing.Application.Status.Queries.GetSocialModuleStatus;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Posts;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Application;

/// <summary>
/// Testes unitários do manipulador <see cref="GetSocialModuleStatusQueryHandler"/>.
/// </summary>
public sealed class GetSocialModuleStatusQueryHandlerTests
{
    private readonly IOrganicConnectionRepository _connections = Substitute.For<IOrganicConnectionRepository>();
    private readonly IOrganicPostRepository _posts = Substitute.For<IOrganicPostRepository>();
    private readonly GetSocialModuleStatusQueryHandler _handler;

    /// <summary>
    /// Inicializa a suíte com repositórios substituídos.
    /// </summary>
    public GetSocialModuleStatusQueryHandlerTests()
    {
        _handler = new GetSocialModuleStatusQueryHandler(_connections, _posts);
    }

    /// <summary>
    /// Workspace vazio não consulta repositórios e retorna Social.WorkspaceRequired.
    /// </summary>
    [Fact]
    public async Task Handle_QuandoWorkspaceVazio_DeveFalharSemChamarRepositorios()
    {
        var result = await _handler.Handle(new GetSocialModuleStatusQuery(Guid.Empty), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.WorkspaceRequired.Code);
        await _connections.DidNotReceive().CountByWorkspaceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _posts.DidNotReceive()
            .CountByWorkspaceAndStatusAsync(Arg.Any<Guid>(), Arg.Any<OrganicPostStatus>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Contagens de conexões e posts agendados refletem os repositórios.
    /// </summary>
    [Fact]
    public async Task Handle_QuandoWorkspaceValido_DeveRetornarContagens()
    {
        var workspaceId = Guid.NewGuid();
        _connections.CountByWorkspaceAsync(workspaceId, Arg.Any<CancellationToken>()).Returns(2);
        _posts.CountByWorkspaceAndStatusAsync(workspaceId, OrganicPostStatus.Scheduled, Arg.Any<CancellationToken>())
            .Returns(1);

        var result = await _handler.Handle(new GetSocialModuleStatusQuery(workspaceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Connections.Should().Be(2);
        result.Value.ScheduledPosts.Should().Be(1);
    }

    /// <summary>
    /// Workspace sem dados retorna zeros.
    /// </summary>
    [Fact]
    public async Task Handle_QuandoSemDados_DeveRetornarZeros()
    {
        var workspaceId = Guid.NewGuid();
        _connections.CountByWorkspaceAsync(workspaceId, Arg.Any<CancellationToken>()).Returns(0);
        _posts.CountByWorkspaceAndStatusAsync(workspaceId, OrganicPostStatus.Scheduled, Arg.Any<CancellationToken>())
            .Returns(0);

        var result = await _handler.Handle(new GetSocialModuleStatusQuery(workspaceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Connections.Should().Be(0);
        result.Value.ScheduledPosts.Should().Be(0);
    }
}
