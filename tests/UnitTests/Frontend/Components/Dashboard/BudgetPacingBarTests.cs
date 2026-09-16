using Automations.Application.Pacing.DTOs;
using BuildingBlocks.Domain.Automations.Pacing;
using Bunit;
using FluentAssertions;
using UnitTests.Frontend.Common;
using WebApp.Components.Dashboard;
using Xunit;

namespace UnitTests.Frontend.Components.Dashboard;

/// <summary>
/// Testes unitários com bUnit para o componente visual de velocidade de consumo de verba <see cref="BudgetPacingBar"/>.
/// Valida renderização nos 3 status (No Ritmo, Sobreaquecido, Subinvestido), modo compacto, agulha de meta ideal e projeção.
/// </summary>
public sealed class BudgetPacingBarTests : BunitTestBase
{
    private static WorkspaceBudgetPacingDto CreateSamplePacing(PacingStatus status)
    {
        var targetBudget = 10_000m;
        var currentSpend = status switch
        {
            PacingStatus.OnTrack => 5_000m,
            PacingStatus.Over => 7_500m,
            PacingStatus.Under => 2_500m,
            _ => 5_000m
        };

        var ratio = currentSpend / 5_000m;
        var runRate = currentSpend / 15m;
        var projected = runRate * 30m;

        return new WorkspaceBudgetPacingDto
        {
            WorkspaceId = Guid.NewGuid(),
            WorkspaceName = "E-commerce Estilo",
            TargetBudget = targetBudget,
            CurrentSpend = currentSpend,
            RemainingBudget = targetBudget - currentSpend,
            TotalDaysInCycle = 30,
            ElapsedDays = 15,
            RemainingDays = 15,
            ExpectedSpendToDate = 5_000m,
            PacingRatio = ratio,
            PacingPercentage = ratio * 100m,
            ActualDailyRunRate = runRate,
            IdealDailyRunRate = 333.33m,
            RequiredDailyRunRate = (targetBudget - currentSpend) / 15m,
            ProjectedMonthEndSpend = projected,
            ProjectedVariance = projected - targetBudget,
            ProjectedVariancePercentage = ((projected - targetBudget) / targetBudget) * 100m,
            Status = status,
            Recommendation = status switch
            {
                PacingStatus.OnTrack => "Ritmo equilibrado dentro da tolerância.",
                PacingStatus.Over => "Ritmo Sobreaquecido: reduza investimento.",
                PacingStatus.Under => "Ritmo Subinvestido: aumente o investimento.",
                _ => string.Empty
            },
            Currency = "BRL"
        };
    }

    /// <summary>
    /// Valida que quando o status for OnTrack (No Ritmo), o componente exibe as classes e textos semânticos em verde esmeralda.
    /// </summary>
    [Fact]
    public void BudgetPacingBar_WhenOnTrack_RendersCorrectStatusAndColors()
    {
        // Arrange
        var pacingData = CreateSamplePacing(PacingStatus.OnTrack);

        // Act
        var cut = Render<BudgetPacingBar>(parameters => parameters
            .Add(p => p.PacingData, pacingData)
            .Add(p => p.ShowDetails, true));

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("No Ritmo");
        markup.Should().Contain("E-commerce Estilo");
        markup.Should().Contain("status-ontrack");
        markup.Should().Contain("R$ 5.000,00");
        markup.Should().Contain("R$ 10.000,00");

        var badge = cut.Find(".pacing-status-badge");
        badge.ClassList.Should().Contain("status-ontrack");
    }

    /// <summary>
    /// Valida que quando o status for Over (Sobreaquecido), o componente exibe a classe de alerta vermelho e aviso textual.
    /// </summary>
    [Fact]
    public void BudgetPacingBar_WhenOver_RendersRedWarningAndStatus()
    {
        // Arrange
        var pacingData = CreateSamplePacing(PacingStatus.Over);

        // Act
        var cut = Render<BudgetPacingBar>(parameters => parameters
            .Add(p => p.PacingData, pacingData));

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("Sobreaquecido (Over)");
        markup.Should().Contain("status-over");
        markup.Should().Contain("reduza investimento");

        var badge = cut.Find(".pacing-status-badge");
        badge.ClassList.Should().Contain("status-over");
    }

    /// <summary>
    /// Valida que quando o status for Under (Subinvestido), o componente exibe a classe de atenção âmbar e recomendação.
    /// </summary>
    [Fact]
    public void BudgetPacingBar_WhenUnder_RendersAmberWarningAndStatus()
    {
        // Arrange
        var pacingData = CreateSamplePacing(PacingStatus.Under);

        // Act
        var cut = Render<BudgetPacingBar>(parameters => parameters
            .Add(p => p.PacingData, pacingData));

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("Subinvestido (Under)");
        markup.Should().Contain("status-under");
        markup.Should().Contain("aumente o investimento");

        var badge = cut.Find(".pacing-status-badge");
        badge.ClassList.Should().Contain("status-under");
    }

    /// <summary>
    /// Valida que em modo compacto o componente oculta o grid de métricas táticas e a recomendação textual.
    /// </summary>
    [Fact]
    public void BudgetPacingBar_WhenCompactMode_HidesDetailedMetricsGrid()
    {
        // Arrange
        var pacingData = CreateSamplePacing(PacingStatus.OnTrack);

        // Act
        var cut = Render<BudgetPacingBar>(parameters => parameters
            .Add(p => p.PacingData, pacingData)
            .Add(p => p.CompactMode, true));

        // Assert
        cut.FindAll(".pacing-metrics-grid").Should().BeEmpty();
        cut.FindAll(".pacing-recommendation-box").Should().BeEmpty();
        cut.Find(".budget-pacing-container").ClassList.Should().Contain("compact-mode");
    }

    /// <summary>
    /// Valida que o marcador ideal (needle) e a régua de progresso são renderizados no elemento progressbar.
    /// </summary>
    [Fact]
    public void BudgetPacingBar_RendersNeedleAndTrackCorrectly()
    {
        // Arrange
        var pacingData = CreateSamplePacing(PacingStatus.OnTrack);

        // Act
        var cut = Render<BudgetPacingBar>(parameters => parameters
            .Add(p => p.PacingData, pacingData));

        // Assert
        var needle = cut.Find(".pacing-needle");
        needle.Should().NotBeNull();
        needle.GetAttribute("style").Should().Contain("left: 50");

        var fill = cut.Find(".pacing-bar-fill");
        fill.Should().NotBeNull();
        fill.GetAttribute("style").Should().Contain("width: 50");
    }
}
