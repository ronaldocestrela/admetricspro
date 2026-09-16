using Bunit;
using FluentAssertions;
using UnitTests.Frontend.Common;
using WebApp.Components.Creatives;
using Xunit;

namespace UnitTests.Frontend.Components.Creatives;

/// <summary>
/// Testes bUnit para o componente visual <see cref="CreativeFatigueBadge"/>.
/// </summary>
public sealed class CreativeFatigueBadgeTests : BunitTestBase
{
    /// <summary>
    /// Valida que o badge renderiza o status Saudável com estilo de sucesso.
    /// </summary>
    [Fact]
    public void CreativeFatigueBadge_ShouldRenderHealthy_WhenStatusIsHealthy()
    {
        // Act
        var cut = Render<CreativeFatigueBadge>(parameters => parameters
            .Add(p => p.Status, "Healthy")
            .Add(p => p.ReplacementSuggested, false));

        // Assert
        cut.Markup.Should().Contain("Saudável");
        cut.Markup.Should().Contain("bg-success");
        cut.Find("#badge-status-healthy").Should().NotBeNull();
        cut.FindAll("#badge-replacement-suggested").Should().BeEmpty();
    }

    /// <summary>
    /// Valida que o badge renderiza Fadiga Crítica e a tag de Substituição Sugerida.
    /// </summary>
    [Fact]
    public void CreativeFatigueBadge_ShouldRenderFatiguedAndReplacementTag_WhenSuggested()
    {
        // Act
        var cut = Render<CreativeFatigueBadge>(parameters => parameters
            .Add(p => p.Status, "Fatigued")
            .Add(p => p.ReplacementSuggested, true));

        // Assert
        cut.Markup.Should().Contain("Fadiga Crítica");
        cut.Markup.Should().Contain("Troca Sugerida");
        cut.Find("#badge-status-fatigued").Should().NotBeNull();
        cut.Find("#badge-replacement-suggested").Should().NotBeNull();
    }
}
