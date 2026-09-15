using AngleSharp.Dom;
using Bunit;
using FluentAssertions;
using UnitTests.Frontend.Common;
using WebApp.Components.Shared;
using WebApp.State;
using Xunit;

namespace UnitTests.Frontend.Components.Shared;

/// <summary>
/// Testes de componente com bUnit para <see cref="BrandLogo"/>.
/// </summary>
public sealed class BrandLogoTests : BunitTestBase
{
    /// <summary>
    /// Valida que quando não há logotipo customizado cadastrado, o componente renderiza o fallback com ícone e nome da empresa.
    /// </summary>
    [Fact]
    public void BrandLogo_WhenNoCustomLogo_ShouldRenderFallbackWithCompanyName()
    {
        // Arrange
        var customBranding = new TenantBranding(
            PrimaryColor: "#2563EB",
            SecondaryColor: "#0F172A",
            AccentColor: "#38BDF8",
            LogoUrl: null,
            DarkLogoUrl: null,
            CompanyName: "AdMetricsPro");

        SetTenant(TenantState.Default with { Branding = customBranding });

        // Act
        var cut = Render<BrandLogo>(parameters => parameters
            .Add(p => p.ShowText, true));

        // Assert
        cut.Find(".brand-logo-fallback").Should().NotBeNull();
        cut.Find(".brand-logo-text").TextContent.Should().Contain("AdMetricsPro");
    }

    /// <summary>
    /// Valida que quando o logotipo escuro está configurado e DarkTheme é verdadeiro, renderiza a tag img com a URL correta.
    /// </summary>
    [Fact]
    public void BrandLogo_WhenDarkLogoConfigured_ShouldRenderDarkLogoImg()
    {
        // Arrange
        var customBranding = new TenantBranding(
            PrimaryColor: "#10B981",
            SecondaryColor: "#1E293B",
            AccentColor: "#38BDF8",
            LogoUrl: "https://cdn.example.com/logo-light.png",
            DarkLogoUrl: "https://cdn.example.com/logo-dark.png",
            CompanyName: "Agência Vanguarda");

        var customTenant = new TenantState(
            TenantId: Guid.NewGuid(),
            Name: "Agência Vanguarda",
            Slug: "agencia-vanguarda",
            CustomDomain: null,
            Branding: customBranding);

        SetTenant(customTenant);

        // Act
        var cut = Render<BrandLogo>(parameters => parameters
            .Add(p => p.DarkTheme, true));

        // Assert
        var img = cut.Find("img.brand-logo-image");
        img.Should().NotBeNull();
        img.GetAttribute("src").Should().Be("https://cdn.example.com/logo-dark.png");
        img.GetAttribute("alt").Should().Be("Agência Vanguarda");
    }
}
