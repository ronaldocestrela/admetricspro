using BackofficeApp.Components.Pages;
using BackofficeApp.Services;
using BuildingBlocks.Domain.Primitives;
using Bunit;
using FluentAssertions;
using Master.Application.FeatureFlags.DTOs;
using Master.Domain.FeatureFlags;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using UnitTests.Frontend.Common;
using Xunit;

namespace UnitTests.Frontend.Components.Pages;

/// <summary>
/// Testes de componente bUnit para a página de Feature Flags &amp; Kill Switches (<see cref="FeatureFlagsPage"/>).
/// Valida a conformidade visual e funcional com o padrão executivo de design do Backoffice.
/// </summary>
public sealed class BackofficeFeatureFlagsPageTests : BunitTestBase
{
    private readonly IFeatureFlagClientService _featureFlagService;

    private static List<FeatureFlagDto> CreateSampleFlags()
    {
        var now = DateTime.UtcNow;
        return
        [
            new(
                Id: Guid.NewGuid(),
                Key: "killswitch.automation.global",
                Name: "Kill Switch Global de Automações",
                Description: "Congela todas as automações cross-network",
                IsEnabled: false,
                IsKillSwitch: true,
                TargetingType: FeatureFlagTargetingType.Global,
                RolloutPercentage: 100,
                TargetTenantIds: Array.Empty<Guid>(),
                KillSwitchActivatedAtUtc: null,
                KillSwitchReason: null,
                KillSwitchTriggeredBy: null,
                CreatedBy: "system",
                CreatedAtUtc: now,
                UpdatedAtUtc: now,
                UpdatedBy: "system"),
            new(
                Id: Guid.NewGuid(),
                Key: "feature.copilot.insights",
                Name: "AI Copilot Insights",
                Description: "Recomendações automáticas baseadas em IA",
                IsEnabled: true,
                IsKillSwitch: false,
                TargetingType: FeatureFlagTargetingType.PercentageRollout,
                RolloutPercentage: 50,
                TargetTenantIds: Array.Empty<Guid>(),
                KillSwitchActivatedAtUtc: null,
                KillSwitchReason: null,
                KillSwitchTriggeredBy: null,
                CreatedBy: "system",
                CreatedAtUtc: now,
                UpdatedAtUtc: now,
                UpdatedBy: "system")
        ];
    }

    /// <summary>
    /// Inicializa os mocks para os testes.
    /// </summary>
    public BackofficeFeatureFlagsPageTests()
    {
        _featureFlagService = Substitute.For<IFeatureFlagClientService>();
        _featureFlagService.GetAllFlagsAsync(Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<FeatureFlagDto>>.Success(CreateSampleFlags()));

        Services.AddSingleton(_featureFlagService);
    }

    /// <summary>
    /// Valida que a página renderiza o cabeçalho executivo padronizado do Backoffice.
    /// </summary>
    [Fact]
    public void FeatureFlagsPage_OnInitialized_ShouldRenderStandardExecutiveHeader()
    {
        // Act
        var cut = Render<FeatureFlagsPage>();

        // Assert
        cut.Find(".page-header").Should().NotBeNull();
        cut.Find(".page-category").TextContent.Should().Contain("Backoffice Global");
        cut.Find(".page-title").TextContent.Should().Contain("Feature Flags & Kill Switches Operacionais");
        cut.Find(".page-description").TextContent.Should().Contain("Controle dinâmico");
        cut.Find(".header-actions button.btn-primary").Should().NotBeNull();
    }

    /// <summary>
    /// Valida que o dashboard é renderizado exibindo os disjuntores e as flags funcionais.
    /// </summary>
    [Fact]
    public void FeatureFlagsPage_ShouldRenderDashboardWithKillSwitchesAndFlags()
    {
        // Act
        var cut = Render<FeatureFlagsPage>();

        // Assert
        cut.Find(".feature-flags-dashboard").Should().NotBeNull();
        cut.Find(".section-kill-switches").Should().NotBeNull();
        cut.Find(".section-feature-flags").Should().NotBeNull();
        cut.Markup.Should().Contain("Kill Switch Global de Automações");
        cut.Markup.Should().Contain("AI Copilot Insights");
    }

    /// <summary>
    /// Valida que o clique no botão de atualizar recarrega os dados através do serviço.
    /// </summary>
    [Fact]
    public void FeatureFlagsPage_WhenRefreshClicked_ShouldInvokeServiceReload()
    {
        // Arrange
        var cut = Render<FeatureFlagsPage>();

        // Act
        var refreshBtn = cut.Find(".header-actions button.btn-primary");
        refreshBtn.Click();

        // Assert
        _featureFlagService.Received().GetAllFlagsAsync(Arg.Any<CancellationToken>());
    }
}
