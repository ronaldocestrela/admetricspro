using FluentAssertions;
using Tenants.Application.Users.Services;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Testes da política de senha de colaboradores.
/// </summary>
public sealed class TenantPasswordPolicyTests
{
  /// <summary>
  /// Valida senha forte.
  /// </summary>
  [Fact]
  public void Validate_WithStrongPassword_ShouldSucceed()
  {
    var result = TenantPasswordPolicy.Validate("SenhaForte@9");
    result.IsSuccess.Should().BeTrue();
  }

  /// <summary>
  /// Valida rejeição de senha fraca.
  /// </summary>
  [Fact]
  public void Validate_WithWeakPassword_ShouldFail()
  {
    var result = TenantPasswordPolicy.Validate("fraca");
    result.IsFailure.Should().BeTrue();
    result.Error.Code.Should().Be("TenantUser.WeakPassword");
  }
}