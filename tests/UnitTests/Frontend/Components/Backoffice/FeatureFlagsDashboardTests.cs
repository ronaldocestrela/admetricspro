using Bunit;
using FluentAssertions;
using Master.Application.FeatureFlags.DTOs;
using Master.Domain.FeatureFlags;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using UnitTests.Frontend.Common;
using WebApp.Components.Backoffice;
using WebApp.Services;
using BuildingBlocks.Domain.Primitives;

namespace UnitTests.Frontend.Components.Backoffice;

/// <summary>
/// Unit tests with bUnit for the <see cref="FeatureFlagsDashboard"/> component.
/// Validates rendering of operational Kill Switch cards, emergency freeze banner when active,
/// and functional feature flags rollout sliders.
/// </summary>
public sealed class FeatureFlagsDashboardTests : BunitTestBase
{
    private readonly IFeatureFlagClientService _clientService = Substitute.For<IFeatureFlagClientService>();

    /// <summary>
    /// Initializes test dependencies.
    /// </summary>
    public FeatureFlagsDashboardTests()
    {
        Services.AddSingleton<IFeatureFlagClientService>(_clientService);
    }

    private static List<FeatureFlagDto> CreateSampleFlags(bool globalFrozen = false)
    {
        var now = DateTime.UtcNow;
        return new List<FeatureFlagDto>
        {
            new(
                Id: Guid.NewGuid(),
                Key: "killswitch.automation.global",
                Name: "Kill Switch Global de Automações",
                Description: "Congela todas as automações",
                IsEnabled: globalFrozen,
                IsKillSwitch: true,
                TargetingType: FeatureFlagTargetingType.Global,
                RolloutPercentage: 100,
                TargetTenantIds: Array.Empty<Guid>(),
                KillSwitchActivatedAtUtc: globalFrozen ? now : null,
                KillSwitchReason: globalFrozen ? "Saturação crítica de API" : null,
                KillSwitchTriggeredBy: globalFrozen ? "ops-lead" : null,
                CreatedBy: "system",
                CreatedAtUtc: now,
                UpdatedAtUtc: now,
                UpdatedBy: "system"),
            new(
                Id: Guid.NewGuid(),
                Key: "killswitch.automation.meta",
                Name: "Kill Switch Meta Ads",
                Description: "Congela automações da Meta",
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
                Key: "feature.analytics.mer-v2",
                Name: "Motor MER v2",
                Description: "Novo algoritmo de atribuição",
                IsEnabled: true,
                IsKillSwitch: false,
                TargetingType: FeatureFlagTargetingType.PercentageRollout,
                RolloutPercentage: 35,
                TargetTenantIds: Array.Empty<Guid>(),
                KillSwitchActivatedAtUtc: null,
                KillSwitchReason: null,
                KillSwitchTriggeredBy: null,
                CreatedBy: "system",
                CreatedAtUtc: now,
                UpdatedAtUtc: now,
                UpdatedBy: "system")
        };
    }

    /// <summary>
    /// Verifies that Kill Switches and functional feature flags are rendered in the dashboard.
    /// </summary>
    [Fact]
    public void FeatureFlagsDashboard_ShouldRenderKillSwitchesAndFlags_WhenLoaded()
    {
        // Arrange
        var flags = CreateSampleFlags(globalFrozen: false);
        _clientService.GetAllFlagsAsync(Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<FeatureFlagDto>>.Success(flags));

        // Act
        var cut = Render<FeatureFlagsDashboard>();

        // Assert
        cut.Find(".section-kill-switches").Should().NotBeNull();
        cut.FindAll(".kill-switch-card").Should().HaveCount(2);
        cut.Markup.Should().Contain("Kill Switch Global de Automações");
        cut.Markup.Should().Contain("OPERANDO NORMALMENTE");

        cut.Find(".section-feature-flags").Should().NotBeNull();
        cut.Markup.Should().Contain("Motor MER v2");
        cut.Markup.Should().Contain("35%");
    }

    /// <summary>
    /// Verifies that an emergency freeze banner is prominently rendered when any Kill Switch is active.
    /// </summary>
    [Fact]
    public void FeatureFlagsDashboard_ShouldRenderCriticalFreezeBanner_WhenKillSwitchIsActive()
    {
        // Arrange
        var flags = CreateSampleFlags(globalFrozen: true);
        _clientService.GetAllFlagsAsync(Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<FeatureFlagDto>>.Success(flags));

        // Act
        var cut = Render<FeatureFlagsDashboard>();

        // Assert
        var banner = cut.Find(".freeze-alert-banner");
        banner.Should().NotBeNull();
        banner.TextContent.Should().Contain("DISJUNTOR OPERACIONAL ATIVO — EXECUÇÕES CONGELADAS");
        banner.TextContent.Should().Contain("Saturação crítica de API");
        banner.TextContent.Should().Contain("ops-lead");

        cut.Markup.Should().Contain("CONGELADO / INTERROMPIDO");
    }
}
