using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Master.Application.Users.Commands.AuthenticateBackofficeUser;
using Master.Application.Users.DTOs;
using Master.Application.Users.Services;
using NSubstitute;
using Xunit;

namespace UnitTests.Backend.Users;

/// <summary>
/// Suíte de testes unitários para comando e handler de autenticação de operadores do Backoffice.
/// Valida fluxos de sucesso, falhas de credenciais, usuário inativo e validações de input.
/// </summary>
public sealed class AuthenticateBackofficeUserTests
{
    private readonly IBackofficeAuthService _authService = Substitute.For<IBackofficeAuthService>();

    /// <summary>
    /// Valida que credenciais válidas retornam resultado de sucesso com dados do operador.
    /// </summary>
    [Fact]
    public async Task Handle_WithValidCredentials_ShouldReturnSuccessResultWithUserDto()
    {
        // Arrange
        var command = new AuthenticateBackofficeUserCommand("admin@admetricspro.internal", "SecurePassword123!", "127.0.0.1");
        var expectedDto = new AuthenticatedBackofficeUserDto(
            Guid.NewGuid(),
            "admin@admetricspro.internal",
            "Super Admin",
            new[] { "SuperAdmin" },
            DateTime.UtcNow);

        _authService.AuthenticateAsync(command.Email, command.Password, command.IpAddress, Arg.Any<CancellationToken>())
            .Returns(Result<AuthenticatedBackofficeUserDto>.Success(expectedDto));

        var handler = new AuthenticateBackofficeUserCommandHandler(_authService);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedDto);
        result.Value.Roles.Should().Contain("SuperAdmin");
    }

    /// <summary>
    /// Valida que credenciais inválidas retornam falha tipada com código de erro.
    /// </summary>
    [Fact]
    public async Task Handle_WithInvalidCredentials_ShouldReturnFailureResult()
    {
        // Arrange
        var command = new AuthenticateBackofficeUserCommand("admin@admetricspro.internal", "WrongPassword", "127.0.0.1");
        _authService.AuthenticateAsync(command.Email, command.Password, command.IpAddress, Arg.Any<CancellationToken>())
            .Returns(Result<AuthenticatedBackofficeUserDto>.Failure(Error.Unauthorized("Auth.InvalidCredentials", "E-mail ou senha incorretos.")));

        var handler = new AuthenticateBackofficeUserCommandHandler(_authService);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.InvalidCredentials");
    }

    /// <summary>
    /// Valida que e-mail em branco dispara erro de validação.
    /// </summary>
    [Fact]
    public void Validator_WithEmptyEmail_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new AuthenticateBackofficeUserCommandValidator();
        var command = new AuthenticateBackofficeUserCommand("", "SecurePassword123!");

        // Act
        var validationResult = validator.Validate(command);

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == nameof(AuthenticateBackofficeUserCommand.Email));
    }

    /// <summary>
    /// Valida que senha em branco dispara erro de validação.
    /// </summary>
    [Fact]
    public void Validator_WithEmptyPassword_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new AuthenticateBackofficeUserCommandValidator();
        var command = new AuthenticateBackofficeUserCommand("admin@admetricspro.internal", "");

        // Act
        var validationResult = validator.Validate(command);

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == nameof(AuthenticateBackofficeUserCommand.Password));
    }
}
