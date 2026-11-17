using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using NSubstitute;
using Tenants.Application.Users.DTOs;
using UnitTests.Frontend.Common;
using WebApp.Components.Pages.Settings;
using WebApp.Models;
using Xunit;

namespace UnitTests.Frontend.Components.Pages;

/// <summary>
/// Testes bUnit da página <see cref="TeamSettingsPage"/>.
/// </summary>
public sealed class TeamSettingsPageTests : BunitTestBase
{
  /// <summary>
  /// Valida renderização do formulário para Owner.
  /// </summary>
  [Fact]
  public void Render_WithOwnerRole_ShouldShowInviteForm()
  {
    TenantSessionStateProvider.SetSession(new Tenants.Application.Auth.DTOs.AuthenticatedTenantUserDto(
      "token", "Bearer", 3600, Guid.NewGuid(), "owner@agencia.com", "Owner", TenantRole.Owner.ToString(),
      Guid.NewGuid(), "vanguarda", null));

    TenantTeamClientService.GetUsersAsync(Arg.Any<CancellationToken>())
      .Returns(Result<IReadOnlyList<TenantUserDto>>.Success(Array.Empty<TenantUserDto>()));

    var cut = Render<TeamSettingsPage>();

    cut.Markup.Should().Contain("Convidar colaborador");
  }

  /// <summary>
  /// Valida envio do convite via cliente HTTP.
  /// </summary>
  [Fact]
  public async Task InviteSubmit_WithValidData_ShouldCallApi()
  {
    TenantSessionStateProvider.SetSession(new Tenants.Application.Auth.DTOs.AuthenticatedTenantUserDto(
      "token", "Bearer", 3600, Guid.NewGuid(), "owner@agencia.com", "Owner", TenantRole.Owner.ToString(),
      Guid.NewGuid(), "vanguarda", null));

    TenantTeamClientService.GetUsersAsync(Arg.Any<CancellationToken>())
      .Returns(Result<IReadOnlyList<TenantUserDto>>.Success(Array.Empty<TenantUserDto>()));

    TenantTeamClientService.InviteUserAsync(Arg.Any<InviteTenantUserModel>(), Arg.Any<CancellationToken>())
      .Returns(Result<InviteTenantUserResultDto>.Success(new InviteTenantUserResultDto(Guid.NewGuid(), true)));

    var cut = Render<TeamSettingsPage>();
    cut.Find("#invite-name").Change("Novo Colaborador");
    cut.Find("#invite-email").Change("novo@agencia.com");
    cut.Find("form.invite-form").Submit();

    await TenantTeamClientService.Received(1).InviteUserAsync(
      Arg.Is<InviteTenantUserModel>(m => m.Email == "novo@agencia.com"),
      Arg.Any<CancellationToken>());
  }
}