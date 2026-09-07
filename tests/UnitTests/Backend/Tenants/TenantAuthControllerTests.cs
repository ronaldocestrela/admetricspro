using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Tenants.Application.Auth.Commands.AuthenticateTenantUser;
using Tenants.Application.Auth.DTOs;
using WebApi.Controllers.v1;
using WebApi.Models;
using Xunit;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Testes unitários para o controlador Web API de autenticação de inquilinos (<see cref="TenantAuthController"/>).
/// Valida mapeamentos HTTP para sucesso (200), validação (422), não autorizado (401) e requisição inválida (400).
/// </summary>
public sealed class TenantAuthControllerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly TenantAuthController _controller;

    /// <summary>
    /// Inicializa a suíte de testes com instâncias simuladas de contexto HTTP e mediador.
    /// </summary>
    public TenantAuthControllerTests()
    {
        _controller = new TenantAuthController(_sender)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    /// <summary>
    /// Valida que requisição com credenciais válidas retorna status 200 OK com Result de sucesso.
    /// </summary>
    [Fact]
    public async Task Login_WithValidRequest_ShouldReturnOkWithResult()
    {
        // Arrange
        var request = new TenantLoginApiRequest("gestor@vanguarda.com.br", "SenhaForte123!", "vanguarda");
        var expectedDto = new AuthenticatedTenantUserDto(
            AccessToken: "jwt_token_exemplo",
            TokenType: "Bearer",
            ExpiresIn: 28800,
            UserId: Guid.NewGuid(),
            Email: request.Email,
            FullName: "Gestor Vanguarda",
            Role: "Owner",
            TenantId: Guid.NewGuid(),
            Subdomain: "vanguarda",
            Branding: null);

        _sender.Send(Arg.Any<AuthenticateTenantUserCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<AuthenticatedTenantUserDto>.Success(expectedDto));

        // Act
        var actionResult = await _controller.Login(request, CancellationToken.None);

        // Assert
        var okResult = actionResult.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(StatusCodes.Status200OK);

        var result = okResult.Value as Result<AuthenticatedTenantUserDto>;
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedDto);
    }

    /// <summary>
    /// Valida que erro de credenciais inválidas retorna status 401 Unauthorized.
    /// </summary>
    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new TenantLoginApiRequest("gestor@vanguarda.com.br", "SenhaErrada", "vanguarda");
        _sender.Send(Arg.Any<AuthenticateTenantUserCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<AuthenticatedTenantUserDto>.Failure(
                Error.Unauthorized("Auth.InvalidCredentials", "E-mail ou senha incorretos.")));

        // Act
        var actionResult = await _controller.Login(request, CancellationToken.None);

        // Assert
        var unauthorizedResult = actionResult.Result as UnauthorizedObjectResult;
        unauthorizedResult.Should().NotBeNull();
        unauthorizedResult!.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    /// <summary>
    /// Valida que erro de validação de input retorna status 422 UnprocessableEntity.
    /// </summary>
    [Fact]
    public async Task Login_WithValidationError_ShouldReturnUnprocessableEntity()
    {
        // Arrange
        var request = new TenantLoginApiRequest("email-invalido", "", "vanguarda");
        _sender.Send(Arg.Any<AuthenticateTenantUserCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<AuthenticatedTenantUserDto>.Failure(
                Error.Validation("Validation.Error", "E-mail inválido.")));

        // Act
        var actionResult = await _controller.Login(request, CancellationToken.None);

        // Assert
        var unprocessableResult = actionResult.Result as UnprocessableEntityObjectResult;
        unprocessableResult.Should().NotBeNull();
        unprocessableResult!.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
    }

    /// <summary>
    /// Valida que envio de corpo nulo retorna status 400 BadRequest.
    /// </summary>
    [Fact]
    public async Task Login_WithNullRequest_ShouldReturnBadRequest()
    {
        // Act
        var actionResult = await _controller.Login(null!, CancellationToken.None);

        // Assert
        var badRequestResult = actionResult.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    /// <summary>
    /// Valida que GetPublicBranding com subdomínio válido retorna status 200 OK com Result de sucesso.
    /// </summary>
    [Fact]
    public async Task GetPublicBranding_WithValidSubdomain_ShouldReturnOkWithResult()
    {
        // Arrange
        var expectedDto = new TenantPublicBrandingDto(
            TenantId: Guid.NewGuid(),
            CompanyName: "Agência Vanguarda",
            Subdomain: "vanguarda",
            CustomDomain: null,
            PrimaryColor: "#1E40AF",
            SecondaryColor: "#F59E0B",
            LogoUrl: null,
            IsActive: true);

        _sender.Send(Arg.Any<global::Tenants.Application.Auth.Queries.GetTenantPublicBranding.GetTenantPublicBrandingQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<TenantPublicBrandingDto>.Success(expectedDto));

        // Act
        var actionResult = await _controller.GetPublicBranding("vanguarda", CancellationToken.None);

        // Assert
        var okResult = actionResult.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(StatusCodes.Status200OK);

        var result = okResult.Value as Result<TenantPublicBrandingDto>;
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedDto);
    }

    /// <summary>
    /// Valida que GetPublicBranding para inquilino inexistente retorna status 404 NotFound.
    /// </summary>
    [Fact]
    public async Task GetPublicBranding_WhenNotFound_ShouldReturnNotFound()
    {
        // Arrange
        _sender.Send(Arg.Any<global::Tenants.Application.Auth.Queries.GetTenantPublicBranding.GetTenantPublicBrandingQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<TenantPublicBrandingDto>.Failure(
                Error.NotFound("Tenant.NotFound", "Inquilino não localizado.")));

        // Act
        var actionResult = await _controller.GetPublicBranding("inexistente", CancellationToken.None);

        // Assert
        var notFoundResult = actionResult.Result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    /// <summary>
    /// Valida que GetPublicBranding com subdomínio vazio retorna status 400 BadRequest.
    /// </summary>
    [Fact]
    public async Task GetPublicBranding_WhenSubdomainEmpty_ShouldReturnBadRequest()
    {
        // Act
        var actionResult = await _controller.GetPublicBranding("", CancellationToken.None);

        // Assert
        var badRequestResult = actionResult.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }
}

