using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Master.Application.Auditing;
using Master.Infrastructure.Identity;
using Master.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace UnitTests.Backend.Users;

/// <summary>
/// Testes unitários para a implementação de segurança e autenticação do Backoffice (MasterBackofficeAuthService).
/// </summary>
public sealed class MasterBackofficeAuthServiceTests
{
    private readonly UserManager<MasterUser> _userManager;
    private readonly IMasterAuditService _auditService = Substitute.For<IMasterAuditService>();
    private readonly ILogger<MasterBackofficeAuthService> _logger = Substitute.For<ILogger<MasterBackofficeAuthService>>();
    private readonly MasterBackofficeAuthService _sut;

    /// <summary>
    /// Inicializa a suíte com mocks do UserManager e serviços de auditoria.
    /// </summary>
    public MasterBackofficeAuthServiceTests()
    {
        var store = Substitute.For<IUserStore<MasterUser>>();
        _userManager = Substitute.For<UserManager<MasterUser>>(
            store, null, null, null, null, null, null, null, null);

        _sut = new MasterBackofficeAuthService(_userManager, _auditService, _logger);
    }

    /// <summary>
    /// Valida que credenciais corretas de operador ativo retornam sucesso com DTO preenchido.
    /// </summary>
    [Fact]
    public async Task AuthenticateAsync_WithValidCredentials_ShouldReturnSuccess()
    {
        // Arrange
        var user = new MasterUser("admin@admetricspro.internal", "Admin User")
        {
            Id = Guid.NewGuid(),
            IsActive = true
        };

        _userManager.FindByEmailAsync("admin@admetricspro.internal").Returns(user);
        _userManager.CheckPasswordAsync(user, "CorrectPassword").Returns(true);
        _userManager.GetRolesAsync(user).Returns(new List<string> { "SuperAdmin" });
        _userManager.UpdateAsync(user).Returns(IdentityResult.Success);

        // Act
        var result = await _sut.AuthenticateAsync("admin@admetricspro.internal", "CorrectPassword", "127.0.0.1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("admin@admetricspro.internal");
        result.Value.FullName.Should().Be("Admin User");
        result.Value.Roles.Should().Contain("SuperAdmin");
    }

    /// <summary>
    /// Valida que usuário inexistente retorna falha tipada de não autorizado.
    /// </summary>
    [Fact]
    public async Task AuthenticateAsync_WhenUserNotFound_ShouldReturnUnauthorizedFailure()
    {
        // Arrange
        _userManager.FindByEmailAsync("nonexistent@domain.com").Returns((MasterUser?)null);

        // Act
        var result = await _sut.AuthenticateAsync("nonexistent@domain.com", "SomePassword");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.InvalidCredentials");
    }

    /// <summary>
    /// Valida que senha incorreta incrementa contagem de falhas e retorna erro de credenciais.
    /// </summary>
    [Fact]
    public async Task AuthenticateAsync_WithWrongPassword_ShouldReturnUnauthorizedFailure()
    {
        // Arrange
        var user = new MasterUser("admin@admetricspro.internal", "Admin User")
        {
            IsActive = true
        };

        _userManager.FindByEmailAsync(user.Email!).Returns(user);
        _userManager.CheckPasswordAsync(user, "WrongPassword").Returns(false);

        // Act
        var result = await _sut.AuthenticateAsync(user.Email!, "WrongPassword");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.InvalidCredentials");
        await _userManager.Received(1).AccessFailedAsync(user);
    }

    /// <summary>
    /// Valida que operador inativo é impedido de efetuar login mesmo informando senha correta.
    /// </summary>
    [Fact]
    public async Task AuthenticateAsync_WhenAccountIsInactive_ShouldReturnAccountInactiveError()
    {
        // Arrange
        var user = new MasterUser("inactive@domain.com", "Inactive User")
        {
            IsActive = false
        };

        _userManager.FindByEmailAsync(user.Email!).Returns(user);

        // Act
        var result = await _sut.AuthenticateAsync(user.Email!, "AnyPassword");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.AccountInactive");
    }
}
