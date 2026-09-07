using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using NSubstitute;
using Tenants.Application.Auth.Commands.AuthenticateTenantUser;
using Tenants.Application.Auth.DTOs;
using Tenants.Application.Auth.Services;
using Xunit;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Testes unitários para o manipulador MediatR de autenticação de inquilinos (AuthenticateTenantUserCommandHandler).
/// </summary>
public sealed class AuthenticateTenantUserCommandHandlerTests
{
    private readonly ITenantAuthService _authService = Substitute.For<ITenantAuthService>();
    private readonly AuthenticateTenantUserCommandHandler _handler;

    /// <summary>
    /// Inicializa a suíte com mock de <see cref="ITenantAuthService"/>.
    /// </summary>
    public AuthenticateTenantUserCommandHandlerTests()
    {
        _handler = new AuthenticateTenantUserCommandHandler(_authService);
    }

    /// <summary>
    /// Valida que requisição com credenciais válidas retorna sucesso com DTO retornado pelo serviço.
    /// </summary>
    [Fact]
    public async Task Handle_WithValidCredentials_ShouldReturnSuccessWithDto()
    {
        // Arrange
        var command = new AuthenticateTenantUserCommand(
            "gestor@vanguarda.com.br",
            "SenhaForte123!",
            "vanguarda",
            null,
            "127.0.0.1");

        var expectedDto = new AuthenticatedTenantUserDto(
            AccessToken: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.dummy",
            TokenType: "Bearer",
            ExpiresIn: 28800,
            UserId: Guid.NewGuid(),
            Email: "gestor@vanguarda.com.br",
            FullName: "Gestor Vanguarda",
            Role: "Owner",
            TenantId: Guid.NewGuid(),
            Subdomain: "vanguarda",
            Branding: new TenantBrandingDto("Vanguarda Digital", "#1E40AF", "#F59E0B", null, null, null));

        _authService.AuthenticateAsync(
            command.Email,
            command.Password,
            command.Subdomain,
            command.TenantId,
            command.IpAddress,
            Arg.Any<CancellationToken>())
            .Returns(Result<AuthenticatedTenantUserDto>.Success(expectedDto));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedDto);
        result.Value.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Value.Role.Should().Be("Owner");
    }

    /// <summary>
    /// Valida que quando o serviço de autenticação retorna falha, o handler propaga o erro tipado.
    /// </summary>
    [Fact]
    public async Task Handle_WhenServiceFails_ShouldPropagateFailureResult()
    {
        // Arrange
        var command = new AuthenticateTenantUserCommand(
            "gestor@vanguarda.com.br",
            "SenhaErrada",
            "vanguarda");

        var expectedError = Error.Unauthorized("Auth.InvalidCredentials", "E-mail ou senha incorretos.");

        _authService.AuthenticateAsync(
            command.Email,
            command.Password,
            command.Subdomain,
            command.TenantId,
            command.IpAddress,
            Arg.Any<CancellationToken>())
            .Returns(Result<AuthenticatedTenantUserDto>.Failure(expectedError));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.InvalidCredentials");
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
    }
}
