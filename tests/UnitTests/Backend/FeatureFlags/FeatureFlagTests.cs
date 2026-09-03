using FluentAssertions;
using Master.Domain.FeatureFlags;
using Master.Domain.FeatureFlags.Events;

namespace UnitTests.Backend.FeatureFlags;

/// <summary>
/// Unit tests for the <see cref="FeatureFlag"/> aggregate root.
/// Validates business rules, percentage rollout determinism, tenant allowlist targeting,
/// and emergency Kill Switch operations with required justification and domain events.
/// </summary>
public sealed class FeatureFlagTests
{
    /// <summary>
    /// Verifies that creating a feature flag with empty or whitespace key fails validation.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldFail_WhenKeyIsEmpty(string invalidKey)
    {
        // Act
        var result = FeatureFlag.Create(
            key: invalidKey,
            name: "Test Flag",
            description: "A test feature flag",
            isEnabled: true,
            isKillSwitch: false,
            targetingType: FeatureFlagTargetingType.Global,
            rolloutPercentage: 100,
            targetTenantIds: null,
            createdBy: "admin@admetricspro.com");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("FeatureFlag.EmptyKey");
    }

    /// <summary>
    /// Verifies that configuring rollout percentage outside [0, 100] fails validation.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Create_ShouldFail_WhenRolloutPercentageIsOutOfRange(int invalidPercentage)
    {
        // Act
        var result = FeatureFlag.Create(
            key: "feature.test",
            name: "Test Flag",
            description: "A test feature flag",
            isEnabled: true,
            isKillSwitch: false,
            targetingType: FeatureFlagTargetingType.PercentageRollout,
            rolloutPercentage: invalidPercentage,
            targetTenantIds: null,
            createdBy: "admin@admetricspro.com");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("FeatureFlag.InvalidRolloutPercentage");
    }

    /// <summary>
    /// Verifies that creating a valid feature flag succeeds and assigns default properties.
    /// </summary>
    [Fact]
    public void Create_ShouldSucceed_WithValidGlobalFlag()
    {
        // Act
        var result = FeatureFlag.Create(
            key: "feature.analytics.mer-v2",
            name: "MER v2 Analytics",
            description: "New Marketing Efficiency Ratio engine",
            isEnabled: true,
            isKillSwitch: false,
            targetingType: FeatureFlagTargetingType.Global,
            rolloutPercentage: 100,
            targetTenantIds: null,
            createdBy: "admin@admetricspro.com");

        // Assert
        result.IsSuccess.Should().BeTrue();
        var flag = result.Value;
        flag.Key.Should().Be("feature.analytics.mer-v2");
        flag.Name.Should().Be("MER v2 Analytics");
        flag.IsEnabled.Should().BeTrue();
        flag.IsKillSwitch.Should().BeFalse();
        flag.TargetingType.Should().Be(FeatureFlagTargetingType.Global);
        flag.RolloutPercentage.Should().Be(100);
        flag.TargetTenantIds.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that global targeting evaluates purely based on the IsEnabled property.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Evaluate_ShouldReturnIsEnabled_WhenTargetingIsGlobal(bool isEnabled)
    {
        // Arrange
        var flag = FeatureFlag.Create(
            key: "feature.global.toggle",
            name: "Global Toggle",
            description: "Test",
            isEnabled: isEnabled,
            isKillSwitch: false,
            targetingType: FeatureFlagTargetingType.Global,
            rolloutPercentage: 100,
            targetTenantIds: null,
            createdBy: "admin@admetricspro.com").Value;

        // Act & Assert
        flag.Evaluate(Guid.NewGuid()).Should().Be(isEnabled);
        flag.Evaluate(null).Should().Be(isEnabled);
    }

    /// <summary>
    /// Verifies that percentage rollout evaluation is deterministic for the same tenant.
    /// </summary>
    [Fact]
    public void Evaluate_ShouldBeDeterministic_ForPercentageRollout()
    {
        // Arrange
        var flag = FeatureFlag.Create(
            key: "feature.staged.rollout",
            name: "Staged Rollout",
            description: "Test",
            isEnabled: true,
            isKillSwitch: false,
            targetingType: FeatureFlagTargetingType.PercentageRollout,
            rolloutPercentage: 50,
            targetTenantIds: null,
            createdBy: "admin@admetricspro.com").Value;

        var tenantId = Guid.NewGuid();

        // Act
        var firstEval = flag.Evaluate(tenantId);
        var secondEval = flag.Evaluate(tenantId);
        var thirdEval = flag.Evaluate(tenantId);

        // Assert
        firstEval.Should().Be(secondEval);
        secondEval.Should().Be(thirdEval);
    }

    /// <summary>
    /// Verifies that 0% rollout always returns false and 100% rollout always returns true when enabled.
    /// </summary>
    [Fact]
    public void Evaluate_ShouldRespectExtremes_ForPercentageRollout()
    {
        // Arrange
        var flag0 = FeatureFlag.Create(
            key: "feature.rollout.0",
            name: "0% Rollout",
            description: "Test",
            isEnabled: true,
            isKillSwitch: false,
            targetingType: FeatureFlagTargetingType.PercentageRollout,
            rolloutPercentage: 0,
            targetTenantIds: null,
            createdBy: "admin@admetricspro.com").Value;

        var flag100 = FeatureFlag.Create(
            key: "feature.rollout.100",
            name: "100% Rollout",
            description: "Test",
            isEnabled: true,
            isKillSwitch: false,
            targetingType: FeatureFlagTargetingType.PercentageRollout,
            rolloutPercentage: 100,
            targetTenantIds: null,
            createdBy: "admin@admetricspro.com").Value;

        var tenantId = Guid.NewGuid();

        // Assert
        flag0.Evaluate(tenantId).Should().BeFalse();
        flag100.Evaluate(tenantId).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that tenant allowlist targeting returns true only for included tenants.
    /// </summary>
    [Fact]
    public void Evaluate_ShouldReturnTrue_OnlyWhenTenantIsInAllowlist()
    {
        // Arrange
        var allowedTenant = Guid.NewGuid();
        var disallowedTenant = Guid.NewGuid();

        var flag = FeatureFlag.Create(
            key: "feature.vip.only",
            name: "VIP Feature",
            description: "Test",
            isEnabled: true,
            isKillSwitch: false,
            targetingType: FeatureFlagTargetingType.TenantList,
            rolloutPercentage: 0,
            targetTenantIds: new[] { allowedTenant },
            createdBy: "admin@admetricspro.com").Value;

        // Act & Assert
        flag.Evaluate(allowedTenant).Should().BeTrue();
        flag.Evaluate(disallowedTenant).Should().BeFalse();
        flag.Evaluate(null).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that activating a Kill Switch fails if no justification reason is provided.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ActivateKillSwitch_ShouldFail_WhenReasonIsEmpty(string invalidReason)
    {
        // Arrange
        var killSwitch = FeatureFlag.CreateKillSwitch(
            key: "killswitch.automation.meta",
            name: "Meta Ads Kill Switch",
            description: "Freezes Meta automations",
            createdBy: "admin@admetricspro.com").Value;

        // Act
        var result = killSwitch.ActivateKillSwitch(invalidReason, "ops-bot", DateTime.UtcNow);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KillSwitch.ReasonRequired");
    }

    /// <summary>
    /// Verifies that activating a Kill Switch marks it as active (frozen), records reason and metadata, and dispatches domain event.
    /// </summary>
    [Fact]
    public void ActivateKillSwitch_ShouldSucceed_AndRaiseDomainEvent()
    {
        // Arrange
        var killSwitch = FeatureFlag.CreateKillSwitch(
            key: "killswitch.automation.global",
            name: "Global Automation Kill Switch",
            description: "Freezes all cross-network automations",
            createdBy: "admin@admetricspro.com").Value;

        var activationTime = DateTime.UtcNow;

        // Act
        var result = killSwitch.ActivateKillSwitch(
            reason: "Meta Graph API v21.0 experiencing heavy 500 error spikes",
            triggeredBy: "incident-lead@admetricspro.com",
            activationTime);

        // Assert
        result.IsSuccess.Should().BeTrue();
        killSwitch.IsKillSwitch.Should().BeTrue();
        killSwitch.IsEnabled.Should().BeTrue(); // In a kill switch, IsEnabled=true means the circuit breaker is ENGAGED/ACTIVE (halted)
        killSwitch.KillSwitchReason.Should().Be("Meta Graph API v21.0 experiencing heavy 500 error spikes");
        killSwitch.KillSwitchTriggeredBy.Should().Be("incident-lead@admetricspro.com");
        killSwitch.KillSwitchActivatedAtUtc.Should().Be(activationTime);

        killSwitch.DomainEvents.Should().ContainSingle(e => e is KillSwitchActivatedDomainEvent);
        var domainEvent = killSwitch.DomainEvents.OfType<KillSwitchActivatedDomainEvent>().Single();
        domainEvent.Key.Should().Be("killswitch.automation.global");
        domainEvent.Reason.Should().Be("Meta Graph API v21.0 experiencing heavy 500 error spikes");
        domainEvent.TriggeredBy.Should().Be("incident-lead@admetricspro.com");
    }

    /// <summary>
    /// Verifies that deactivating a Kill Switch restores operations and raises deactivation domain event.
    /// </summary>
    [Fact]
    public void DeactivateKillSwitch_ShouldSucceed_AndRaiseDomainEvent()
    {
        // Arrange
        var killSwitch = FeatureFlag.CreateKillSwitch(
            key: "killswitch.automation.global",
            name: "Global Automation Kill Switch",
            description: "Freezes all cross-network automations",
            createdBy: "admin@admetricspro.com").Value;

        killSwitch.ActivateKillSwitch("Incident ongoing", "ops", DateTime.UtcNow);
        killSwitch.ClearDomainEvents();

        var restorationTime = DateTime.UtcNow;

        // Act
        var result = killSwitch.DeactivateKillSwitch(
            reason: "Platform metrics stabilized, resuming engine",
            triggeredBy: "incident-lead@admetricspro.com",
            restorationTime);

        // Assert
        result.IsSuccess.Should().BeTrue();
        killSwitch.IsEnabled.Should().BeFalse(); // Disengaged
        killSwitch.KillSwitchReason.Should().Be("Platform metrics stabilized, resuming engine");
        killSwitch.KillSwitchTriggeredBy.Should().Be("incident-lead@admetricspro.com");

        killSwitch.DomainEvents.Should().ContainSingle(e => e is KillSwitchDeactivatedDomainEvent);
        var domainEvent = killSwitch.DomainEvents.OfType<KillSwitchDeactivatedDomainEvent>().Single();
        domainEvent.Key.Should().Be("killswitch.automation.global");
        domainEvent.Reason.Should().Be("Platform metrics stabilized, resuming engine");
    }

    /// <summary>
    /// Verifies that updating rollout percentage updates state and dispatches update event.
    /// </summary>
    [Fact]
    public void SetRolloutPercentage_ShouldUpdateStateAndRaiseEvent()
    {
        // Arrange
        var flag = FeatureFlag.Create(
            key: "feature.canary",
            name: "Canary Feature",
            description: "Test",
            isEnabled: true,
            isKillSwitch: false,
            targetingType: FeatureFlagTargetingType.PercentageRollout,
            rolloutPercentage: 10,
            targetTenantIds: null,
            createdBy: "admin@admetricspro.com").Value;

        flag.ClearDomainEvents();
        var updateTime = DateTime.UtcNow;

        // Act
        var result = flag.SetRolloutPercentage(50, "product-manager@admetricspro.com", updateTime);

        // Assert
        result.IsSuccess.Should().BeTrue();
        flag.RolloutPercentage.Should().Be(50);
        flag.UpdatedBy.Should().Be("product-manager@admetricspro.com");
        flag.UpdatedAtUtc.Should().Be(updateTime);
        flag.DomainEvents.Should().ContainSingle(e => e is FeatureFlagUpdatedDomainEvent);
    }
}
