using Bunit;
using FluentAssertions;
using UnitTests.Frontend.Common;
using WebApp.Components.Layout;
using Xunit;

namespace UnitTests.Frontend.Components.Layout;

/// <summary>
/// Testes unitários com bUnit para o banner informativo de Trial do Tenant (<see cref="TenantTrialBanner"/>).
/// Valida exibição do prazo restante, chamada para ação (CTA) e capacidade de dispensar.
/// </summary>
public sealed class TenantTrialBannerTests : BunitTestBase
{
    /// <summary>
    /// Valida que o banner de trial renderiza o alerta com os dias restantes e o botão de ativação de plano.
    /// </summary>
    [Fact]
    public void TenantTrialBanner_ShouldRenderTrialAlertAndActionButton()
    {
        // Arrange & Act
        var cut = Render<TenantTrialBanner>(parameters => parameters
            .Add(p => p.DaysRemaining, 14));

        // Assert
        var banner = cut.Find(".tenant-trial-banner");
        banner.Should().NotBeNull();
        banner.TextContent.Should().Contain("Ambiente de Testes (Trial) — 14 dias restantes.");

        var ctaButton = cut.Find(".trial-cta-button");
        ctaButton.Should().NotBeNull();
        ctaButton.TextContent.Should().Contain("Ativar Plano Definitivo");
    }

    /// <summary>
    /// Valida que ao clicar no botão de fechar, o banner é ocultado visualmente.
    /// </summary>
    [Fact]
    public void TenantTrialBanner_WhenDismissClicked_ShouldHideBanner()
    {
        // Arrange
        var cut = Render<TenantTrialBanner>(parameters => parameters
            .Add(p => p.DaysRemaining, 14));

        var dismissButton = cut.Find(".trial-dismiss-button");
        dismissButton.Should().NotBeNull();

        // Act
        dismissButton.Click();

        // Assert
        cut.FindAll(".tenant-trial-banner").Should().BeEmpty();
    }

    /// <summary>
    /// Valida que quando o inquilino não está mais em período de degustação (IsTrial = false), o banner não é renderizado.
    /// </summary>
    [Fact]
    public void TenantTrialBanner_WhenNotTrial_ShouldNotRender()
    {
        // Arrange & Act
        var cut = Render<TenantTrialBanner>(parameters => parameters
            .Add(p => p.IsTrial, false));

        // Assert
        cut.FindAll(".tenant-trial-banner").Should().BeEmpty();
    }
}
