using BackofficeApp.Components.Shared;
using Bunit;
using FluentAssertions;
using UnitTests.Frontend.Common;
using Xunit;

namespace UnitTests.Frontend.Components;

/// <summary>
/// Testes de componente bUnit para o crachá de perfil do usuário (UserProfileBadge).
/// </summary>
public sealed class UserProfileBadgeTests : BunitTestBase
{
    /// <summary>
    /// Valida que usuário não autenticado visualiza o link para login.
    /// </summary>
    [Fact]
    public void UserProfileBadge_WhenUnauthenticated_ShouldRenderLoginLink()
    {
        // Arrange
        this.AddAuthorization();

        // Act
        var cut = Render<UserProfileBadge>();

        // Assert
        var link = cut.Find("a.btn-logout");
        link.TextContent.Should().Contain("Entrar");
        link.GetAttribute("href").Should().Be("/login");
    }

    /// <summary>
    /// Valida que usuário autenticado como SuperAdmin visualiza seu nome, badge de role e botão de logout.
    /// </summary>
    [Fact]
    public void UserProfileBadge_WhenAuthenticatedAsSuperAdmin_ShouldRenderUserInfoAndLogout()
    {
        // Arrange
        var authContext = this.AddAuthorization();
        authContext.SetAuthorized("Ronaldo Estrela");
        authContext.SetRoles("SuperAdmin");

        // Act
        var cut = Render<UserProfileBadge>();

        // Assert
        cut.Find(".user-name").TextContent.Should().Be("Ronaldo Estrela");
        cut.Find(".user-role").TextContent.Should().Be("SUPER ADMIN");
        cut.Find("a.btn-logout").TextContent.Should().Contain("Sair");
    }
}
