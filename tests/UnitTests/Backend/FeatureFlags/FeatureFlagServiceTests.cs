using FluentAssertions;
using Master.Application.Auditing;
using Master.Application.FeatureFlags.Commands.ActivateKillSwitch;
using Master.Application.FeatureFlags.Commands.DeactivateKillSwitch;
using Master.Application.FeatureFlags.DTOs;
using Master.Application.FeatureFlags.Repositories;
using Master.Application.FeatureFlags.Services;
using Master.Domain.FeatureFlags;
using Master.Domain.Integrations;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;

namespace UnitTests.Backend.FeatureFlags;

/// <summary>
/// Unit tests for the <see cref="FeatureFlagService"/> and associated MediatR handlers.
/// Validates caching behavior, global and network-specific kill switches freezing the automation engine,
/// and audit trail emission on emergency operations.
/// </summary>
public sealed class FeatureFlagServiceTests
{
    private readonly IFeatureFlagRepository _repository;
    private readonly IMasterAuditService _auditService;
    private readonly IMemoryCache _memoryCache;
    private readonly FeatureFlagService _service;

    /// <summary>
    /// Initializes test dependencies and system under test.
    /// </summary>
    public FeatureFlagServiceTests()
    {
        _repository = Substitute.For<IFeatureFlagRepository>();
        _auditService = Substitute.For<IMasterAuditService>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _service = new FeatureFlagService(_repository, _auditService, _memoryCache);
    }

    /// <summary>
    /// Verifies that IsFeatureEnabledAsync evaluates the flag and uses memory caching on subsequent calls.
    /// </summary>
    [Fact]
    public async Task IsFeatureEnabledAsync_ShouldCacheEvaluationResult()
    {
        // Arrange
        var flag = FeatureFlag.Create(
            key: "feature.fast-sync",
            name: "Fast Sync",
            description: "Fast sync mode",
            isEnabled: true,
            isKillSwitch: false,
            targetingType: FeatureFlagTargetingType.Global,
            rolloutPercentage: 100,
            targetTenantIds: null,
            createdBy: "admin").Value;

        _repository.GetByKeyAsync("feature.fast-sync", Arg.Any<CancellationToken>())
            .Returns(flag);

        // Act
        var firstCall = await _service.IsFeatureEnabledAsync("feature.fast-sync");
        var secondCall = await _service.IsFeatureEnabledAsync("feature.fast-sync");

        // Assert
        firstCall.Should().BeTrue();
        secondCall.Should().BeTrue();
        await _repository.Received(1).GetByKeyAsync("feature.fast-sync", Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that IsAutomationFrozenAsync returns true if the global automation kill switch is active.
    /// </summary>
    [Fact]
    public async Task IsAutomationFrozenAsync_ShouldReturnTrue_WhenGlobalKillSwitchIsActive()
    {
        // Arrange
        var globalKillSwitch = FeatureFlag.CreateKillSwitch(
            key: "killswitch.automation.global",
            name: "Global Automation Kill Switch",
            description: "Emergency freeze",
            createdBy: "admin").Value;

        globalKillSwitch.ActivateKillSwitch("Critical system bug", "ops", DateTime.UtcNow);

        _repository.GetByKeyAsync("killswitch.automation.global", Arg.Any<CancellationToken>())
            .Returns(globalKillSwitch);

        // Act
        var isFrozen = await _service.IsAutomationFrozenAsync(AdPlatform.Meta);

        // Assert
        isFrozen.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that IsAutomationFrozenAsync returns true for Meta when only the Meta kill switch is active,
    /// but returns false for Google Ads.
    /// </summary>
    [Fact]
    public async Task IsAutomationFrozenAsync_ShouldIsolateNetworkSpecificKillSwitches()
    {
        // Arrange
        var globalKillSwitch = FeatureFlag.CreateKillSwitch(
            key: "killswitch.automation.global",
            name: "Global Automation Kill Switch",
            description: "Emergency freeze",
            createdBy: "admin").Value;

        var metaKillSwitch = FeatureFlag.CreateKillSwitch(
            key: "killswitch.automation.meta",
            name: "Meta Automation Kill Switch",
            description: "Freeze Meta automations",
            createdBy: "admin").Value;
        metaKillSwitch.ActivateKillSwitch("Meta Graph API 503 incident", "ops", DateTime.UtcNow);

        var googleKillSwitch = FeatureFlag.CreateKillSwitch(
            key: "killswitch.automation.google",
            name: "Google Automation Kill Switch",
            description: "Freeze Google automations",
            createdBy: "admin").Value;

        _repository.GetByKeyAsync("killswitch.automation.global", Arg.Any<CancellationToken>())
            .Returns(globalKillSwitch);
        _repository.GetByKeyAsync("killswitch.automation.meta", Arg.Any<CancellationToken>())
            .Returns(metaKillSwitch);
        _repository.GetByKeyAsync("killswitch.automation.google", Arg.Any<CancellationToken>())
            .Returns(googleKillSwitch);

        // Act
        var metaFrozen = await _service.IsAutomationFrozenAsync(AdPlatform.Meta);
        var googleFrozen = await _service.IsAutomationFrozenAsync(AdPlatform.Google);

        // Assert
        metaFrozen.Should().BeTrue();
        googleFrozen.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that activating a Kill Switch emits an audit log and invalidates cache.
    /// </summary>
    [Fact]
    public async Task ActivateKillSwitchAsync_ShouldUpdateRepositoryAndRecordAuditLog()
    {
        // Arrange
        var killSwitch = FeatureFlag.CreateKillSwitch(
            key: "killswitch.automation.tiktok",
            name: "TikTok Automation Kill Switch",
            description: "Freeze TikTok automations",
            createdBy: "admin").Value;

        _repository.GetByKeyAsync("killswitch.automation.tiktok", Arg.Any<CancellationToken>())
            .Returns(killSwitch);

        // Act
        var result = await _service.ActivateKillSwitchAsync(
            "killswitch.automation.tiktok",
            "Rate limit exhausted on TikTok",
            "oncall@admetricspro.com");

        // Assert
        result.IsSuccess.Should().BeTrue();
        killSwitch.IsKillSwitchActive.Should().BeTrue();
        await _repository.Received(1).UpdateAsync(killSwitch, Arg.Any<CancellationToken>());
        await _auditService.Received(1).RecordAsync(
            action: "KillSwitch.Activated",
            resource: "FeatureFlag",
            resourceId: "killswitch.automation.tiktok",
            details: "Rate limit exhausted on TikTok",
            tenantId: null,
            ipAddress: null,
            additionalTags: Arg.Is<IEnumerable<string>>(tags => tags.Contains("kill_switch")),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that deactivating a Kill Switch records restoration audit log.
    /// </summary>
    [Fact]
    public async Task DeactivateKillSwitchAsync_ShouldUpdateRepositoryAndRecordAuditLog()
    {
        // Arrange
        var killSwitch = FeatureFlag.CreateKillSwitch(
            key: "killswitch.automation.bing",
            name: "Bing Automation Kill Switch",
            description: "Freeze Bing automations",
            createdBy: "admin").Value;
        killSwitch.ActivateKillSwitch("Incident active", "ops", DateTime.UtcNow);

        _repository.GetByKeyAsync("killswitch.automation.bing", Arg.Any<CancellationToken>())
            .Returns(killSwitch);

        // Act
        var result = await _service.DeactivateKillSwitchAsync(
            "killswitch.automation.bing",
            "Bing Ads service recovered",
            "incident-commander@admetricspro.com");

        // Assert
        result.IsSuccess.Should().BeTrue();
        killSwitch.IsKillSwitchActive.Should().BeFalse();
        await _repository.Received(1).UpdateAsync(killSwitch, Arg.Any<CancellationToken>());
        await _auditService.Received(1).RecordAsync(
            action: "KillSwitch.Deactivated",
            resource: "FeatureFlag",
            resourceId: "killswitch.automation.bing",
            details: "Bing Ads service recovered",
            tenantId: null,
            ipAddress: null,
            additionalTags: Arg.Is<IEnumerable<string>>(tags => tags.Contains("kill_switch")),
            cancellationToken: Arg.Any<CancellationToken>());
    }
}
