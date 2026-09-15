using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Integrations.Application.OAuth.Commands.HandleOAuthCallback;
using Integrations.Application.OAuth.Commands.InitiateOAuthFlow;
using Integrations.Application.OAuth.Commands.RefreshExpiringTokens;
using Integrations.Application.OAuth.Commands.RevokeOAuthConnection;
using Integrations.Application.OAuth.DTOs;
using Integrations.Application.OAuth.Queries.GetOAuthConnectionsStatus;
using Integrations.Domain.OAuth;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using WebApi.Controllers.v1;

namespace UnitTests.Backend.Integrations.OAuth;

/// <summary>
/// Testes unitários para o controlador de API <see cref="OAuthIntegrationsController"/>.
/// </summary>
public sealed class OAuthIntegrationsControllerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly OAuthIntegrationsController _controller;

    /// <summary>
    /// Inicializa o controlador com o mediador mockado.
    /// </summary>
    public OAuthIntegrationsControllerTests()
    {
        _controller = new OAuthIntegrationsController(_sender);
    }

    /// <summary>
    /// Valida que initiate-url retorna 200 OK com a URL de autorização.
    /// </summary>
    [Fact]
    public async Task InitiateOAuthFlow_ComParametrosValidos_DeveRetornarOk()
    {
        // Arrange
        var expected = new OAuthAuthorizationUrlDto("https://auth.meta.com", "state_abc");
        _sender.Send(Arg.Any<InitiateOAuthFlowCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<OAuthAuthorizationUrlDto>.Success(expected));

        // Act
        var actionResult = await _controller.InitiateOAuthFlow(Guid.NewGuid(), OAuthPlatform.MetaAds, "https://cb");

        // Assert
        var okResult = actionResult.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var result = okResult.Value as Result<OAuthAuthorizationUrlDto>;
        result.Should().NotBeNull();
        result!.Value.AuthorizationUrl.Should().Be("https://auth.meta.com");
    }

    /// <summary>
    /// Valida que callback bem-sucedido retorna 200 OK com status da conexão.
    /// </summary>
    [Fact]
    public async Task HandleOAuthCallback_ComSucesso_DeveRetornarOk()
    {
        // Arrange
        var expected = new OAuthConnectionStatusDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            OAuthPlatform.GoogleAds,
            "cust_1",
            "Conta Google",
            OAuthConnectionStatus.Active,
            "ads",
            DateTime.UtcNow.AddHours(1),
            false,
            DateTime.UtcNow,
            null);

        _sender.Send(Arg.Any<HandleOAuthCallbackCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<OAuthConnectionStatusDto>.Success(expected));

        // Act
        var actionResult = await _controller.HandleOAuthCallback("code_123", "state_123", "https://cb");

        // Assert
        var okResult = actionResult.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    /// <summary>
    /// Valida que refresh de tokens retorna contagem de conexões renovadas.
    /// </summary>
    [Fact]
    public async Task RefreshExpiringTokens_DeveRetornarOkComTotalRenovado()
    {
        // Arrange
        _sender.Send(Arg.Any<RefreshExpiringTokensCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<int>.Success(3));

        // Act
        var actionResult = await _controller.RefreshExpiringTokens(72);

        // Assert
        var okResult = actionResult.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var result = okResult.Value as Result<int>;
        result!.Value.Should().Be(3);
    }

    /// <summary>
    /// Valida que consulta de status retorna 200 OK.
    /// </summary>
    [Fact]
    public async Task GetConnectionsStatus_DeveRetornarOkComLista()
    {
        // Arrange
        _sender.Send(Arg.Any<GetOAuthConnectionsStatusQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<OAuthConnectionStatusDto>>.Success(new List<OAuthConnectionStatusDto>()));

        // Act
        var actionResult = await _controller.GetConnectionsStatus(Guid.NewGuid());

        // Assert
        var okResult = actionResult.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    /// <summary>
    /// Valida revogação de credencial retornando 200 OK.
    /// </summary>
    [Fact]
    public async Task RevokeConnection_DeveRetornarOk()
    {
        // Arrange
        _sender.Send(Arg.Any<RevokeOAuthConnectionCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var actionResult = await _controller.RevokeConnection(Guid.NewGuid(), OAuthPlatform.BingAds);

        // Assert
        var okResult = actionResult.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }
}
