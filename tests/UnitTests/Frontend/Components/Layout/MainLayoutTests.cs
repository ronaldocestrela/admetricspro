using AngleSharp.Dom;
using Bunit;
using FluentAssertions;
using UnitTests.Frontend.Common;
using WebApp.Components.Layout;
using WebApp.State;

namespace UnitTests.Frontend.Components.Layout;

/// <summary>
/// Testes de componente com bUnit para o layout mestre da aplicação (<see cref="MainLayout"/>).
/// Valida a injeção dinâmica de CSS customizado (White-Label), renderização de componentes filhos e reatividade a mudanças de tenant.
/// </summary>
public class MainLayoutTests : BunitTestBase
{
    /// <summary>
    /// Valida que o container raiz do layout (.app-shell) renderiza as variáveis CSS inline de acordo com o branding padrão do tenant.
    /// </summary>
    [Fact]
    public void MainLayout_ShouldRenderShellWithDefaultTenantCssVariables()
    {
        // Act
        var cut = Render<MainLayout>();

        // Assert
        var appShell = cut.Find(".app-shell");
        appShell.Should().NotBeNull();

        var styleAttribute = appShell.GetAttribute("style");
        styleAttribute.Should().NotBeNull();
        styleAttribute.Should().Contain("--tenant-primary: #2563EB;");
        styleAttribute.Should().Contain("--tenant-secondary: #0F172A;");
        styleAttribute.Should().Contain("--tenant-accent: #38BDF8;");
    }

    /// <summary>
    /// Valida que o MainLayout renderiza os componentes filhos essenciais (Header, Sidebar, Footer e Body).
    /// </summary>
    [Fact]
    public void MainLayout_ShouldRenderChildComponentsAndBody()
    {
        // Arrange & Act
        var cut = Render<MainLayout>(parameters => parameters
            .Add(p => p.Body, builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "id", "test-body-content");
                builder.AddContent(2, "Conteúdo de Teste do Body");
                builder.CloseElement();
            }));

        // Assert
        cut.Find(".app-header").Should().NotBeNull();
        cut.Find(".app-sidebar").Should().NotBeNull();
        cut.Find(".app-footer").Should().NotBeNull();
        cut.Find("#test-body-content").TextContent.Should().Be("Conteúdo de Teste do Body");
    }

    /// <summary>
    /// Valida que ao alterar o tenant ativo via provedor de estado, o layout re-renderiza refletindo o novo branding White-Label.
    /// </summary>
    [Fact]
    public void MainLayout_WhenTenantStateChanges_ShouldReRenderWithUpdatedStyles()
    {
        // Arrange
        var cut = Render<MainLayout>();

        var customBranding = new TenantBranding(
            PrimaryColor: "#9333EA",
            SecondaryColor: "#1E1B4B",
            AccentColor: "#C084FC",
            LogoUrl: "https://example.com/logo.svg",
            CompanyName: "Empresa Customizada");

        var updatedTenant = new TenantState(
            TenantId: Guid.NewGuid(),
            Name: "Empresa Customizada",
            Slug: "empresa-customizada",
            CustomDomain: "app.custom.com",
            Branding: customBranding);

        // Act
        SetTenant(updatedTenant);

        // Assert
        var appShell = cut.Find(".app-shell");
        var styleAttribute = appShell.GetAttribute("style");
        styleAttribute.Should().Contain("--tenant-primary: #9333EA;");
        styleAttribute.Should().Contain("--tenant-secondary: #1E1B4B;");
        styleAttribute.Should().Contain("--tenant-accent: #C084FC;");
    }

    /// <summary>
    /// Valida que ao clicar no botão de alternância do cabeçalho, a barra lateral alterna entre aberta e fechada.
    /// </summary>
    [Fact]
    public void MainLayout_WhenSidebarToggled_ShouldToggleSidebarState()
    {
        // Arrange
        var cut = Render<MainLayout>();
        var sidebar = cut.Find(".app-sidebar");
        sidebar.ClassList.Should().NotContain("open");

        var toggleButton = cut.Find("button.mobile-sidebar-toggle");

        // Act - Abre a barra lateral
        toggleButton.Click();

        // Assert
        cut.Find(".app-sidebar").ClassList.Should().Contain("open");

        // Act - Fecha a barra lateral
        toggleButton.Click();

        // Assert
        cut.Find(".app-sidebar").ClassList.Should().NotContain("open");
    }
}
