using Bunit;
using FluentAssertions;
using UnitTests.Frontend.Common;
using WebApp.Components.Layout;
using Xunit;

namespace UnitTests.Frontend.Components.Layout;

/// <summary>
/// Testes unitários com bUnit para a barra de navegação lateral operacional do inquilino (<see cref="TenantSidebar"/>).
/// Valida links obrigatórios (Visão Geral, Clientes, Times, Integrações de Anúncios e Configurações White-Label) e responsividade móvel.
/// </summary>
public sealed class TenantSidebarTests : BunitTestBase
{
    /// <summary>
    /// Valida que a barra lateral renderiza todos os 5 itens de navegação operacional definidos no roadmap da Subfase 3.1.
    /// </summary>
    [Fact]
    public void TenantSidebar_ShouldRenderAllRequiredNavigationLinks()
    {
        // Act
        var cut = Render<TenantSidebar>();

        // Assert
        var links = cut.FindAll("a.nav-link-item");
        links.Should().HaveCountGreaterThanOrEqualTo(5);

        // 1. Visão Geral (Dashboard)
        var dashboardLink = cut.Find("a[href='dashboard']");
        dashboardLink.TextContent.Should().Contain("Visão Geral");

        // 2. Clientes (Workspaces)
        var workspacesLink = cut.Find("a[href='workspaces']");
        workspacesLink.TextContent.Should().Contain("Clientes");

        // 3. Times (Squads)
        var squadsLink = cut.Find("a[href='squads']");
        squadsLink.TextContent.Should().Contain("Times");

        // 4. Integrações de Anúncios
        var integrationsLink = cut.Find("a[href='integrations']");
        integrationsLink.TextContent.Should().Contain("Integrações de Anúncios");

        // 5. Configurações White-Label
        var whiteLabelLink = cut.Find("a[href='settings/white-label']");
        whiteLabelLink.TextContent.Should().Contain("Configurações White-Label");
    }

    /// <summary>
    /// Valida que a classe CSS 'open' é aplicada quando o parâmetro IsOpen for verdadeiro.
    /// </summary>
    [Fact]
    public void TenantSidebar_WhenIsOpenTrue_ShouldApplyOpenClass()
    {
        // Act
        var cut = Render<TenantSidebar>(parameters => parameters
            .Add(p => p.IsOpen, true));

        // Assert
        var aside = cut.Find("aside.tenant-sidebar");
        aside.ClassList.Should().Contain("open");
    }

    /// <summary>
    /// Valida que o evento OnClose é acionado ao clicar no botão de fechar móvel ou no overlay.
    /// </summary>
    [Fact]
    public void TenantSidebar_WhenCloseButtonClicked_ShouldInvokeOnClose()
    {
        // Arrange
        var closeInvoked = false;
        var cut = Render<TenantSidebar>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OnClose, () => closeInvoked = true));

        var closeButton = cut.Find("button.sidebar-close-btn");

        // Act
        closeButton.Click();

        // Assert
        closeInvoked.Should().BeTrue();
    }
}
