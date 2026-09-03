using Bunit;
using FluentAssertions;
using UnitTests.Frontend.Common;
using WebApp.Components.Shared;

namespace UnitTests.Frontend.Components.Shared;

/// <summary>
/// Testes de componente com bUnit para a barra lateral de navegação (<see cref="AppSidebar"/>).
/// Valida a alternância de estado de abertura/fechamento e a renderização dos links de navegação dos módulos.
/// </summary>
public class AppSidebarTests : BunitTestBase
{
    /// <summary>
    /// Valida que quando o parâmetro IsOpen for falso, a barra lateral não contém a classe CSS 'open'.
    /// </summary>
    [Fact]
    public void AppSidebar_WhenIsOpenIsFalse_ShouldNotHaveOpenClass()
    {
        // Act
        var cut = Render<AppSidebar>(parameters => parameters
            .Add(p => p.IsOpen, false));

        // Assert
        var aside = cut.Find("aside.app-sidebar");
        aside.ClassList.Should().NotContain("open");
    }

    /// <summary>
    /// Valida que quando o parâmetro IsOpen for verdadeiro, a barra lateral inclui a classe CSS 'open'.
    /// </summary>
    [Fact]
    public void AppSidebar_WhenIsOpenIsTrue_ShouldHaveOpenClass()
    {
        // Act
        var cut = Render<AppSidebar>(parameters => parameters
            .Add(p => p.IsOpen, true));

        // Assert
        var aside = cut.Find("aside.app-sidebar");
        aside.ClassList.Should().Contain("open");
    }

    /// <summary>
    /// Valida que todos os links de navegação dos módulos fundamentais da aplicação estão presentes na barra lateral.
    /// </summary>
    [Fact]
    public void AppSidebar_ShouldRenderStandardNavigationLinks()
    {
        // Act
        var cut = Render<AppSidebar>();

        // Assert
        var navLinks = cut.FindAll("a.nav-link-item");
        navLinks.Should().HaveCount(7);

        var linkTexts = navLinks.Select(link => link.TextContent.Trim()).ToList();
        linkTexts.Should().Contain("Dashboard Geral");
        linkTexts.Should().Contain("Workspaces");
        linkTexts.Should().Contain("Campanhas & Mídia");
        linkTexts.Should().Contain("Regras & Automações");
        linkTexts.Should().Contain("Relatórios Cross-Platform");
        linkTexts.Should().Contain("Configurações White-Label");
        linkTexts.Should().Contain("Saúde das APIs (Rate Limits)");
    }
}
