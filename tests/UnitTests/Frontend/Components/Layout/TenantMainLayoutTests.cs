using AngleSharp.Dom;
using Bunit;
using FluentAssertions;
using UnitTests.Frontend.Common;
using WebApp.Components.Layout;
using WebApp.State;
using Xunit;

namespace UnitTests.Frontend.Components.Layout;

/// <summary>
/// Testes de componente com bUnit para o layout mestre do Tenant (<see cref="TenantMainLayout"/>).
/// Valida injeção dinâmica de CSS White-Label, logotipo customizado e fallback de iniciais, navegação e banner de trial.
/// </summary>
public sealed class TenantMainLayoutTests : BunitTestBase
{
    /// <summary>
    /// Valida que o container raiz do layout (.tenant-app-shell) renderiza as variáveis CSS inline globais do branding do inquilino.
    /// </summary>
    [Fact]
    public void TenantMainLayout_ShouldRenderDynamicWhiteLabelCssVariables()
    {
        // Act
        var cut = Render<TenantMainLayout>();

        // Assert
        var appShell = cut.Find(".tenant-app-shell");
        appShell.Should().NotBeNull();

        var styleAttribute = appShell.GetAttribute("style");
        styleAttribute.Should().NotBeNull();
        styleAttribute.Should().Contain("--tenant-primary-color: #2563EB;");
        styleAttribute.Should().Contain("--tenant-secondary-color: #0F172A;");
    }

    /// <summary>
    /// Valida que a logomarca customizada da agência é renderizada quando uma URL de imagem válida estiver presente no branding.
    /// </summary>
    [Fact]
    public void TenantMainLayout_WhenLogoUrlProvided_ShouldRenderAgencyLogo()
    {
        // Arrange
        var customBranding = new TenantBranding(
            PrimaryColor: "#4F46E5",
            SecondaryColor: "#0F172A",
            AccentColor: "#38BDF8",
            LogoUrl: "https://cdn.example.com/agencia-alfa-logo.svg",
            CompanyName: "Agência Alfa");

        var customTenant = new TenantState(
            TenantId: Guid.NewGuid(),
            Name: "Agência Alfa",
            Slug: "agencia-alfa",
            CustomDomain: null,
            Branding: customBranding);

        SetTenant(customTenant);

        // Act
        var cut = Render<TenantMainLayout>();

        // Assert
        var logoImg = cut.Find("img.tenant-brand-logo");
        logoImg.Should().NotBeNull();
        logoImg.GetAttribute("src").Should().Be("https://cdn.example.com/agencia-alfa-logo.svg");
        logoImg.GetAttribute("alt").Should().Be("Agência Alfa");
    }

    /// <summary>
    /// Valida que na ausência de logomarca gráfica, o layout renderiza o monograma/avatar elegante com as iniciais da agência.
    /// </summary>
    [Fact]
    public void TenantMainLayout_WhenNoLogoProvided_ShouldRenderInitialsFallbackBadge()
    {
        // Arrange
        var customBranding = new TenantBranding(
            PrimaryColor: "#10B981",
            SecondaryColor: "#064E3B",
            AccentColor: "#34D399",
            LogoUrl: null,
            CompanyName: "Vanguarda Digital");

        var customTenant = new TenantState(
            TenantId: Guid.NewGuid(),
            Name: "Vanguarda Digital",
            Slug: "vanguarda",
            CustomDomain: null,
            Branding: customBranding);

        SetTenant(customTenant);

        // Act
        var cut = Render<TenantMainLayout>();

        // Assert
        cut.FindAll("img.tenant-brand-logo").Should().BeEmpty();
        var monogram = cut.Find(".tenant-brand-initials");
        monogram.Should().NotBeNull();
        monogram.TextContent.Trim().Should().Be("VD");
    }

    /// <summary>
    /// Valida que o TenantMainLayout renderiza os componentes filhos essenciais: cabeçalho, sidebar, banner de trial e @Body.
    /// </summary>
    [Fact]
    public void TenantMainLayout_ShouldRenderChildComponentsAndBody()
    {
        // Arrange & Act
        var cut = Render<TenantMainLayout>(parameters => parameters
            .Add(p => p.Body, builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "id", "tenant-dashboard-content");
                builder.AddContent(2, "Conteúdo Operacional do Dashboard");
                builder.CloseElement();
            }));

        // Assert
        cut.Find(".tenant-top-header").Should().NotBeNull();
        cut.Find(".tenant-sidebar").Should().NotBeNull();
        cut.Find(".tenant-trial-banner").Should().NotBeNull();
        cut.Find("#tenant-dashboard-content").TextContent.Should().Be("Conteúdo Operacional do Dashboard");
    }

    /// <summary>
    /// Valida que o clique no botão mobile alterna a visibilidade da barra lateral.
    /// </summary>
    [Fact]
    public void TenantMainLayout_WhenMobileToggleClicked_ShouldToggleSidebar()
    {
        // Arrange
        var cut = Render<TenantMainLayout>();
        var sidebar = cut.Find(".tenant-sidebar");
        sidebar.ClassList.Should().NotContain("open");

        var toggleButton = cut.Find("button.tenant-mobile-toggle");

        // Act - Abre
        toggleButton.Click();

        // Assert
        cut.Find(".tenant-sidebar").ClassList.Should().Contain("open");

        // Act - Fecha
        toggleButton.Click();

        // Assert
        cut.Find(".tenant-sidebar").ClassList.Should().NotContain("open");
    }

    /// <summary>
    /// Valida que ao disparar OnTenantChanged no provedor de estado, o layout re-renderiza refletindo o novo tema.
    /// </summary>
    [Fact]
    public void TenantMainLayout_WhenTenantStateChanges_ShouldReRenderWithNewStyles()
    {
        // Arrange
        var cut = Render<TenantMainLayout>();

        var updatedBranding = new TenantBranding(
            PrimaryColor: "#EC4899",
            SecondaryColor: "#831843",
            AccentColor: "#F472B6",
            LogoUrl: null,
            CompanyName: "Pink Agency");

        var updatedTenant = new TenantState(
            TenantId: Guid.NewGuid(),
            Name: "Pink Agency",
            Slug: "pink-agency",
            CustomDomain: null,
            Branding: updatedBranding);

        // Act
        SetTenant(updatedTenant);

        // Assert
        var appShell = cut.Find(".tenant-app-shell");
        var style = appShell.GetAttribute("style");
        style.Should().Contain("--tenant-primary-color: #EC4899;");
        style.Should().Contain("--tenant-secondary-color: #831843;");
    }
}
