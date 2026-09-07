using Bunit;
using FluentAssertions;
using UnitTests.Frontend.Common;
using WebApp.Components.Landing;
using Xunit;

namespace UnitTests.Frontend.Components.Layout;

/// <summary>
/// Testes unitários com bUnit para o layout mestre da Landing Page (<see cref="LandingLayout"/>).
/// Valida o isolamento visual estrutural, renderização do container de conteúdo e o bloco padrão de erro do Blazor.
/// </summary>
public sealed class LandingLayoutTests : BunitTestBase
{
    /// <summary>
    /// Valida que o LandingLayout renderiza o container raiz .landing-shell com o conteúdo passado no Body.
    /// </summary>
    [Fact]
    public void LandingLayout_ShouldRenderLandingShellWithBodyContent()
    {
        // Arrange & Act
        var cut = Render<LandingLayout>(parameters => parameters
            .Add(p => p.Body, builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "id", "landing-test-content");
                builder.AddContent(2, "Conteúdo da Landing Page");
                builder.CloseElement();
            }));

        // Assert
        var landingShell = cut.Find(".landing-shell");
        landingShell.Should().NotBeNull();
        cut.Find("#landing-test-content").TextContent.Should().Be("Conteúdo da Landing Page");
    }

    /// <summary>
    /// Valida que o LandingLayout possui o container blazor-error-ui estruturado corretamente
    /// para tratamento de exceções de UI pelo Blazor Server.
    /// </summary>
    [Fact]
    public void LandingLayout_ShouldContainBlazorErrorUiContainer()
    {
        // Act
        var cut = Render<LandingLayout>();

        // Assert
        var errorUi = cut.Find("#blazor-error-ui");
        errorUi.Should().NotBeNull();
        errorUi.HasAttribute("data-nosnippet").Should().BeTrue();
        errorUi.QuerySelector(".reload").Should().NotBeNull();
        errorUi.QuerySelector(".dismiss").Should().NotBeNull();
    }
}
