using Bunit;
using FluentAssertions;
using UnitTests.Frontend.Common;
using WebApp.Components.Shared;
using WebApp.State;

namespace UnitTests.Frontend.Components.Shared;

/// <summary>
/// Testes de componente com bUnit para o cabeçalho corporativo (<see cref="AppHeader"/>).
/// Valida a exibição do logotipo ou texto institucional, identificação do tenant e disparo do evento de menu lateral.
/// </summary>
public class AppHeaderTests : BunitTestBase
{
    /// <summary>
    /// Valida que quando o tenant possui logotipo configurado (como no branding padrão), a tag img correspondente é renderizada.
    /// </summary>
    [Fact]
    public void AppHeader_WhenLogoUrlIsPresent_ShouldRenderLogoImageWithCorrectAttributes()
    {
        // Act
        var cut = Render<AppHeader>();

        // Assert
        var logoImg = cut.Find("img.tenant-logo");
        logoImg.Should().NotBeNull();
        logoImg.GetAttribute("src").Should().Be(TenantBranding.Default.LogoUrl);
        logoImg.GetAttribute("alt").Should().Be("AdMetricsPro");
        cut.FindAll(".tenant-brand-text").Should().BeEmpty();
    }

    /// <summary>
    /// Valida que quando o logotipo for nulo ou vazio, a marca é exibida via elemento de texto estilizado com o nome do tenant.
    /// </summary>
    [Fact]
    public void AppHeader_WhenLogoUrlIsEmpty_ShouldRenderBrandTextFallback()
    {
        // Arrange
        var customBranding = new TenantBranding(
            PrimaryColor: "#10B981",
            SecondaryColor: "#064E3B",
            AccentColor: "#34D399",
            LogoUrl: null);

        var tenantWithoutLogo = new TenantState(
            TenantId: Guid.NewGuid(),
            Name: "Agência Sem Logo",
            Slug: "agencia-sem-logo",
            CustomDomain: null,
            Branding: customBranding);

        SetTenant(tenantWithoutLogo);

        // Act
        var cut = Render<AppHeader>();

        // Assert
        cut.FindAll("img.tenant-logo").Should().BeEmpty();
        var brandText = cut.Find(".tenant-brand-text");
        brandText.Should().NotBeNull();
        brandText.TextContent.Trim().Should().Be("Agência Sem Logo");
    }

    /// <summary>
    /// Valida que a badge e o avatar de usuário exibem a identificação textual e a inicial do tenant ativo.
    /// </summary>
    [Fact]
    public void AppHeader_ShouldRenderTenantBadgeAndAvatarInitial()
    {
        // Arrange
        var tenant = new TenantState(
            TenantId: Guid.NewGuid(),
            Name: "Performance Hub",
            Slug: "perf-hub",
            CustomDomain: null,
            Branding: TenantBranding.Default);

        SetTenant(tenant);

        // Act
        var cut = Render<AppHeader>();

        // Assert
        var badge = cut.Find(".tenant-badge");
        badge.TextContent.Trim().Should().Be("Performance Hub");

        var avatar = cut.Find(".user-avatar");
        avatar.TextContent.Trim().Should().Be("P");
        avatar.GetAttribute("title").Should().Be("Performance Hub");
    }

    /// <summary>
    /// Valida que ao clicar no botão mobile de menu, o EventCallback OnToggleSidebar é invocado com sucesso.
    /// </summary>
    [Fact]
    public void AppHeader_WhenToggleSidebarButtonClicked_ShouldInvokeOnToggleSidebarCallback()
    {
        // Arrange
        var callbackInvoked = false;
        var cut = Render<AppHeader>(parameters => parameters
            .Add(p => p.OnToggleSidebar, () => callbackInvoked = true));

        var toggleButton = cut.Find("button.mobile-sidebar-toggle");

        // Act
        toggleButton.Click();

        // Assert
        callbackInvoked.Should().BeTrue();
    }
}
