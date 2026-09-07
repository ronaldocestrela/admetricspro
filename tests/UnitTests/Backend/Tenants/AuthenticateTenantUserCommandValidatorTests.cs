using FluentAssertions;
using Tenants.Application.Auth.Commands.AuthenticateTenantUser;
using Xunit;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Testes unitários para o validador de comando de autenticação de inquilino (AuthenticateTenantUserCommandValidator).
/// </summary>
public sealed class AuthenticateTenantUserCommandValidatorTests
{
    private readonly AuthenticateTenantUserCommandValidator _validator = new();

    /// <summary>
    /// Valida que um comando com dados corretos passa na validação.
    /// </summary>
    [Fact]
    public void Validate_WithValidCommand_ShouldPassValidation()
    {
        // Arrange
        var command = new AuthenticateTenantUserCommand("gestor@vanguarda.com.br", "SenhaForte123!", "vanguarda");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Valida que e-mail vazio ou em branco falha na validação.
    /// </summary>
    /// <param name="invalidEmail">E-mail inválido a testar.</param>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WithEmptyEmail_ShouldHaveValidationError(string? invalidEmail)
    {
        // Arrange
        var command = new AuthenticateTenantUserCommand(invalidEmail!, "SenhaForte123!");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AuthenticateTenantUserCommand.Email));
    }

    /// <summary>
    /// Valida que formato incorreto de e-mail falha na validação.
    /// </summary>
    [Theory]
    [InlineData("email-invalido")]
    [InlineData("email@")]
    [InlineData("@dominio.com")]
    public void Validate_WithInvalidEmailFormat_ShouldHaveValidationError(string invalidEmail)
    {
        // Arrange
        var command = new AuthenticateTenantUserCommand(invalidEmail, "SenhaForte123!");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AuthenticateTenantUserCommand.Email));
    }

    /// <summary>
    /// Valida que senha vazia ou menor que 6 caracteres falha na validação.
    /// </summary>
    /// <param name="invalidPassword">Senha inválida a testar.</param>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("12345")]
    public void Validate_WithInvalidPassword_ShouldHaveValidationError(string? invalidPassword)
    {
        // Arrange
        var command = new AuthenticateTenantUserCommand("gestor@vanguarda.com.br", invalidPassword!);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AuthenticateTenantUserCommand.Password));
    }
}
