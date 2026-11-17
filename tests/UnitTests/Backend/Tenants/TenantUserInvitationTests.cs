using BuildingBlocks.Domain.Tenants;
using FluentAssertions;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Testes unitários da entidade <see cref="TenantUserInvitation"/>.
/// </summary>
public sealed class TenantUserInvitationTests
{
  /// <summary>
  /// Valida criação de convite com dados válidos.
  /// </summary>
  [Fact]
  public void Create_WithValidData_ShouldSucceed()
  {
    var id = Guid.NewGuid();
    var userId = Guid.NewGuid();
    var expires = DateTime.UtcNow.AddHours(24);

    var result = TenantUserInvitation.Create(id, userId, new string('a', 64), expires);

    result.IsSuccess.Should().BeTrue();
    result.Value.IsOpen.Should().BeTrue();
  }

  /// <summary>
  /// Valida aceite de convite aberto.
  /// </summary>
  [Fact]
  public void Accept_WhenOpen_ShouldMarkAccepted()
  {
    var invitation = TenantUserInvitation.Create(
      Guid.NewGuid(),
      Guid.NewGuid(),
      new string('b', 64),
      DateTime.UtcNow.AddHours(1)).Value;

    var accept = invitation.Accept();

    accept.IsSuccess.Should().BeTrue();
    invitation.AcceptedAtUtc.Should().NotBeNull();
    invitation.IsOpen.Should().BeFalse();
  }

  /// <summary>
  /// Valida rejeição de convite expirado.
  /// </summary>
  [Fact]
  public void Accept_WhenExpired_ShouldFail()
  {
    var invitation = TenantUserInvitation.Create(
      Guid.NewGuid(),
      Guid.NewGuid(),
      new string('c', 64),
      DateTime.UtcNow.AddHours(1)).Value;

    typeof(TenantUserInvitation).GetProperty(nameof(TenantUserInvitation.ExpiresAtUtc))!
      .SetValue(invitation, DateTime.UtcNow.AddMinutes(-5));

    var accept = invitation.Accept();

    accept.IsFailure.Should().BeTrue();
    accept.Error.Code.Should().Be("TenantUserInvitation.Expired");
  }
}