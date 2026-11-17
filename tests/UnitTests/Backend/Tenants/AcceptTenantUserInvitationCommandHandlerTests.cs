using BuildingBlocks.Domain.Primitives;
using NSubstitute;
using Tenants.Application.Users.Commands.AcceptTenantUserInvitation;
using Tenants.Application.Users.DTOs;
using Tenants.Application.Users.Services;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Testes do manipulador de aceite de convite.
/// </summary>
public sealed class AcceptTenantUserInvitationCommandHandlerTests
{
  private readonly ITenantInvitationPublicGateway _gateway = Substitute.For<ITenantInvitationPublicGateway>();

  /// <summary>
  /// Valida erro quando senhas não coincidem.
  /// </summary>
  [Fact]
  public async Task Handle_WhenPasswordsMismatch_ShouldReturnValidationError()
  {
    var handler = new AcceptTenantUserInvitationCommandHandler(_gateway);
    var command = new AcceptTenantUserInvitationCommand("token", "agencia", "SenhaForte@9", "Outra@9");

    var result = await handler.Handle(command, CancellationToken.None);

    Assert.True(result.IsFailure);
    Assert.Equal("TenantUser.PasswordMismatch", result.Error.Code);
    await _gateway.DidNotReceive().AcceptAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
  }

  /// <summary>
  /// Valida delegação ao gateway público.
  /// </summary>
  [Fact]
  public async Task Handle_WithValidPassword_ShouldDelegateToGateway()
  {
    _gateway.AcceptAsync("token", "agencia", "SenhaForte@9", Arg.Any<CancellationToken>())
      .Returns(Result.Success());

    var handler = new AcceptTenantUserInvitationCommandHandler(_gateway);
    var command = new AcceptTenantUserInvitationCommand("token", "agencia", "SenhaForte@9", "SenhaForte@9");

    var result = await handler.Handle(command, CancellationToken.None);

    Assert.True(result.IsSuccess);
    await _gateway.Received(1).AcceptAsync("token", "agencia", "SenhaForte@9", Arg.Any<CancellationToken>());
  }
}