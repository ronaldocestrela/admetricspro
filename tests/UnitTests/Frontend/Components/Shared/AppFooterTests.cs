using Bunit;
using FluentAssertions;
using UnitTests.Frontend.Common;
using WebApp.Components.Shared;
using WebApp.State;

namespace UnitTests.Frontend.Components.Shared;

/// <summary>
/// Testes de componente com bUnit para o rodapé da aplicação (<see cref="AppFooter"/>).
/// Valida os dados de copyright, razão social institucional e a exibição condicional White-Label do selo 'Powered by'.
/// </summary>
public class AppFooterTests : BunitTestBase
{
    /// <summary>
    /// Valida que o rodapé renderiza o ano atual e o nome corporativo ou do tenant no copyright.
    /// </summary>
    [Fact]
    public void AppFooter_ShouldRenderCurrentYearAndCompanyName()
    {
        // Act
        var cut = Render<AppFooter>();

        // Assert
        var companySpan = cut.Find(".footer-company");
        var currentYear = DateTime.UtcNow.Year.ToString();

        companySpan.TextContent.Should().Contain(currentYear);
        companySpan.TextContent.Should().Contain("AdMetricsPro");
        companySpan.TextContent.Should().Contain("Todos os direitos reservados.");
    }

    /// <summary>
    /// Valida que quando ShowPoweredBy for verdadeiro, o selo 'Powered by AdMetricsPro' é exibido no lado direito do rodapé.
    /// </summary>
    [Fact]
    public void AppFooter_WhenShowPoweredByIsTrue_ShouldRenderPoweredByTag()
    {
        // Arrange
        var brandingWithPoweredBy = new TenantBranding(
            PrimaryColor: "#2563EB",
            SecondaryColor: "#0F172A",
            AccentColor: "#38BDF8",
            ShowPoweredBy: true);

        var tenant = new TenantState(
            TenantId: Guid.NewGuid(),
            Name: "Agência Parceira",
            Slug: "agencia-parceira",
            CustomDomain: null,
            Branding: brandingWithPoweredBy);

        SetTenant(tenant);

        // Act
        var cut = Render<AppFooter>();

        // Assert
        var poweredBy = cut.Find(".powered-by-tag");
        poweredBy.Should().NotBeNull();
        poweredBy.TextContent.Should().Contain("Powered by");
        poweredBy.TextContent.Should().Contain("AdMetricsPro");
    }

    /// <summary>
    /// Valida que quando ShowPoweredBy for falso (White-Label total), a seção do selo 'Powered by' é totalmente omitida do DOM.
    /// </summary>
    [Fact]
    public void AppFooter_WhenShowPoweredByIsFalse_ShouldNotRenderPoweredByTag()
    {
        // Arrange
        var whiteLabelBranding = new TenantBranding(
            PrimaryColor: "#7C3AED",
            SecondaryColor: "#0F172A",
            AccentColor: "#A78BFA",
            CompanyName: "Enterprise White-Label",
            ShowPoweredBy: false);

        var tenant = new TenantState(
            TenantId: Guid.NewGuid(),
            Name: "Enterprise White-Label",
            Slug: "enterprise-wl",
            CustomDomain: "analytics.enterprise.com",
            Branding: whiteLabelBranding);

        SetTenant(tenant);

        // Act
        var cut = Render<AppFooter>();

        // Assert
        cut.FindAll(".powered-by-tag").Should().BeEmpty();
        var companySpan = cut.Find(".footer-company");
        companySpan.TextContent.Should().Contain("Enterprise White-Label");
    }
}
