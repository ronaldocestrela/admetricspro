using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Infrastructure.MultiTenancy;
using FluentAssertions;
using NSubstitute;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Connections.Commands.RevokeOrganicConnection;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Connections;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Application;

/// <summary>
/// Testes do comando <see cref="RevokeOrganicConnectionCommandHandler"/>.
/// </summary>
public sealed class RevokeOrganicConnectionCommandTests
{
    private readonly IOrganicConnectionRepository _repository = Substitute.For<IOrganicConnectionRepository>();
    private readonly ISocialPublishingUnitOfWork _unitOfWork = Substitute.For<ISocialPublishingUnitOfWork>();
    private readonly ITenantContextAccessor _tenantContextAccessor = Substitute.For<ITenantContextAccessor>();

    /// <summary>
    /// Inicializa tenant resolvido por padrão.
    /// </summary>
    public RevokeOrganicConnectionCommandTests()
    {
        _tenantContextAccessor.TenantContext.Returns(
            TenantContext.Create(Guid.NewGuid(), null, TenantResolutionSource.Header));
    }

    /// <summary>
    /// Revoga conexão ativa, limpa ciphertext e persiste.
    /// </summary>
    [Fact]
    public async Task Handle_ComConexaoAtiva_DeveRevogarEliminarCiphertextEPersistir()
    {
        var workspaceId = Guid.NewGuid();
        var connection = OrganicConnection.Create(
            workspaceId,
            OrganicPlatform.FacebookPage,
            "page-1",
            "Page Oficial",
            "scope",
            "cipher-live").Value;

        _repository.GetByIdAsync(connection.Id, Arg.Any<CancellationToken>()).Returns(connection);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new RevokeOrganicConnectionCommand(workspaceId, connection.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        connection.Status.Should().Be(OrganicConnectionStatus.Revoked);
        connection.EncryptedAccessToken.Should().Be(OrganicConnection.RevokedCredentialMarker);
        await _repository.Received(1).UpdateAsync(connection, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Segunda revogação é idempotente e ainda persiste estado consistente.
    /// </summary>
    [Fact]
    public async Task Handle_SegundaRevogacao_DeveSerIdempotente()
    {
        var workspaceId = Guid.NewGuid();
        var connection = OrganicConnection.Create(
            workspaceId,
            OrganicPlatform.TikTok,
            "tt-1",
            "TikTok",
            "scope",
            "cipher").Value;
        connection.Revoke().IsSuccess.Should().BeTrue();

        _repository.GetByIdAsync(connection.Id, Arg.Any<CancellationToken>()).Returns(connection);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new RevokeOrganicConnectionCommand(workspaceId, connection.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        connection.Status.Should().Be(OrganicConnectionStatus.Revoked);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Conexão inexistente retorna ConnectionNotFound.
    /// </summary>
    [Fact]
    public async Task Handle_ComIdInexistente_DeveRetornarConnectionNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((OrganicConnection?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new RevokeOrganicConnectionCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.ConnectionNotFound.Code);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Conexão de outro workspace retorna ConnectionNotFound.
    /// </summary>
    [Fact]
    public async Task Handle_ComWorkspaceDivergente_DeveRetornarConnectionNotFound()
    {
        var connection = OrganicConnection.Create(
            Guid.NewGuid(),
            OrganicPlatform.Instagram,
            "ig-2",
            "Outro",
            "scope",
            "cipher").Value;

        _repository.GetByIdAsync(connection.Id, Arg.Any<CancellationToken>()).Returns(connection);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new RevokeOrganicConnectionCommand(Guid.NewGuid(), connection.Id),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.ConnectionNotFound.Code);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    private RevokeOrganicConnectionCommandHandler CreateHandler() =>
        new(_repository, _unitOfWork, _tenantContextAccessor);
}
