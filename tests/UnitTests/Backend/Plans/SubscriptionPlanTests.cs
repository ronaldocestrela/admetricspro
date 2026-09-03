using FluentAssertions;
using Master.Domain.Plans;
using Master.Domain.Tenants;

namespace UnitTests.Backend.Plans;

/// <summary>
/// Unit tests for <see cref="SubscriptionPlan"/> aggregate root, limits, and feature flags.
/// </summary>
public sealed class SubscriptionPlanTests
{
    /// <summary>
    /// Verifies that creating a plan with valid parameters succeeds.
    /// </summary>
    [Fact]
    public void Create_WithValidParameters_ShouldSucceed()
    {
        // Arrange
        var limits = PlanLimits.Create(maxSeats: 10, maxWorkspaces: 5, monthlyAdSpendCap: 50_000m).Value;
        var features = PlanFeatures.Create(hasWhiteLabel: true, hasCustomCname: true, hasAiCopilot: false, hasCrossNetworkAutomations: true).Value;

        // Act
        var result = SubscriptionPlan.Create(
            name: "Agência Pro",
            description: "Plano para médias agências",
            tier: SubscriptionTier.Pro,
            monthlyPrice: 499.00m,
            annualDiscountPercentage: 20,
            limits: limits,
            features: features);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var plan = result.Value;
        plan.Name.Should().Be("Agência Pro");
        plan.Description.Should().Be("Plano para médias agências");
        plan.Tier.Should().Be(SubscriptionTier.Pro);
        plan.MonthlyPrice.Should().Be(499.00m);
        plan.AnnualDiscountPercentage.Should().Be(20);
        plan.Limits.MaxSeats.Should().Be(10);
        plan.Limits.MaxWorkspaces.Should().Be(5);
        plan.Limits.MonthlyAdSpendCap.Should().Be(50_000m);
        plan.Features.HasWhiteLabel.Should().BeTrue();
        plan.Features.HasCustomCname.Should().BeTrue();
        plan.Features.HasAiCopilot.Should().BeFalse();
        plan.Features.HasCrossNetworkAutomations.Should().BeTrue();
        plan.IsActive.Should().BeTrue();
        plan.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "PlanCreatedDomainEvent");
    }

    /// <summary>
    /// Verifies that creating limits with invalid seat count fails with validation error.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void PlanLimits_Create_WithZeroOrNegativeSeats_ShouldFail(int seats)
    {
        // Act
        var result = PlanLimits.Create(maxSeats: seats, maxWorkspaces: 5, monthlyAdSpendCap: 10_000m);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Plan.InvalidSeats");
    }

    /// <summary>
    /// Verifies that creating limits with invalid workspace count fails with validation error.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void PlanLimits_Create_WithZeroOrNegativeWorkspaces_ShouldFail(int workspaces)
    {
        // Act
        var result = PlanLimits.Create(maxSeats: 5, maxWorkspaces: workspaces, monthlyAdSpendCap: 10_000m);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Plan.InvalidWorkspaces");
    }

    /// <summary>
    /// Verifies that creating limits with negative ad spend cap fails with validation error.
    /// </summary>
    [Fact]
    public void PlanLimits_Create_WithNegativeAdSpendCap_ShouldFail()
    {
        // Act
        var result = PlanLimits.Create(maxSeats: 5, maxWorkspaces: 2, monthlyAdSpendCap: -100m);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Plan.InvalidAdSpendCap");
    }

    /// <summary>
    /// Verifies that creating a plan with empty name fails with validation error.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyName_ShouldFail(string? name)
    {
        // Arrange
        var limits = PlanLimits.Create(5, 2, 10_000m).Value;
        var features = PlanFeatures.Default();

        // Act
        var result = SubscriptionPlan.Create(
            name: name!,
            description: "Desc",
            tier: SubscriptionTier.Starter,
            monthlyPrice: 199m,
            annualDiscountPercentage: 10,
            limits: limits,
            features: features);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Plan.NameRequired");
    }

    /// <summary>
    /// Verifies that creating a plan with negative monthly price fails with validation error.
    /// </summary>
    [Fact]
    public void Create_WithNegativeMonthlyPrice_ShouldFail()
    {
        // Arrange
        var limits = PlanLimits.Create(5, 2, 10_000m).Value;
        var features = PlanFeatures.Default();

        // Act
        var result = SubscriptionPlan.Create(
            name: "Plano Inválido",
            description: "Desc",
            tier: SubscriptionTier.Starter,
            monthlyPrice: -50m,
            annualDiscountPercentage: 10,
            limits: limits,
            features: features);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Plan.InvalidMonthlyPrice");
    }

    /// <summary>
    /// Verifies that annual discount percentage must be between 0 and 100.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Create_WithInvalidAnnualDiscount_ShouldFail(int discount)
    {
        // Arrange
        var limits = PlanLimits.Create(5, 2, 10_000m).Value;
        var features = PlanFeatures.Default();

        // Act
        var result = SubscriptionPlan.Create(
            name: "Plano Desconto Inválido",
            description: "Desc",
            tier: SubscriptionTier.Starter,
            monthlyPrice: 100m,
            annualDiscountPercentage: discount,
            limits: limits,
            features: features);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Plan.InvalidAnnualDiscount");
    }

    /// <summary>
    /// Verifies updating plan details modifies values and records update timestamp.
    /// </summary>
    [Fact]
    public void UpdateDetails_ShouldUpdateFieldsAndTimestamp()
    {
        // Arrange
        var limits = PlanLimits.Create(5, 2, 10_000m).Value;
        var features = PlanFeatures.Default();
        var plan = SubscriptionPlan.Create("Plano Original", "Desc 1", SubscriptionTier.Starter, 100m, 10, limits, features).Value;

        // Act
        var updateResult = plan.UpdateDetails("Plano Atualizado", "Nova Desc", 150m, 15);

        // Assert
        updateResult.IsSuccess.Should().BeTrue();
        plan.Name.Should().Be("Plano Atualizado");
        plan.Description.Should().Be("Nova Desc");
        plan.MonthlyPrice.Should().Be(150m);
        plan.AnnualDiscountPercentage.Should().Be(15);
        plan.UpdatedAtUtc.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies updating plan limits modifies limits object and records update timestamp.
    /// </summary>
    [Fact]
    public void UpdateLimits_ShouldUpdateLimits()
    {
        // Arrange
        var limits = PlanLimits.Create(5, 2, 10_000m).Value;
        var features = PlanFeatures.Default();
        var plan = SubscriptionPlan.Create("Plano Starter", "Desc", SubscriptionTier.Starter, 100m, 10, limits, features).Value;
        var newLimits = PlanLimits.Create(15, 8, 30_000m).Value;

        // Act
        var updateResult = plan.UpdateLimits(newLimits);

        // Assert
        updateResult.IsSuccess.Should().BeTrue();
        plan.Limits.MaxSeats.Should().Be(15);
        plan.Limits.MaxWorkspaces.Should().Be(8);
        plan.Limits.MonthlyAdSpendCap.Should().Be(30_000m);
        plan.UpdatedAtUtc.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies updating plan features modifies features object and records update timestamp.
    /// </summary>
    [Fact]
    public void UpdateFeatures_ShouldUpdateFeatures()
    {
        // Arrange
        var limits = PlanLimits.Create(5, 2, 10_000m).Value;
        var features = PlanFeatures.Default();
        var plan = SubscriptionPlan.Create("Plano Starter", "Desc", SubscriptionTier.Starter, 100m, 10, limits, features).Value;
        var newFeatures = PlanFeatures.Create(hasWhiteLabel: true, hasCustomCname: true, hasAiCopilot: true, hasCrossNetworkAutomations: true).Value;

        // Act
        var updateResult = plan.UpdateFeatures(newFeatures);

        // Assert
        updateResult.IsSuccess.Should().BeTrue();
        plan.Features.HasWhiteLabel.Should().BeTrue();
        plan.Features.HasCustomCname.Should().BeTrue();
        plan.Features.HasAiCopilot.Should().BeTrue();
        plan.Features.HasCrossNetworkAutomations.Should().BeTrue();
        plan.UpdatedAtUtc.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies deactivating and reactivating a plan.
    /// </summary>
    [Fact]
    public void DeactivateAndReactivate_ShouldToggleStatus()
    {
        // Arrange
        var limits = PlanLimits.Create(5, 2, 10_000m).Value;
        var features = PlanFeatures.Default();
        var plan = SubscriptionPlan.Create("Plano Ativo", "Desc", SubscriptionTier.Starter, 100m, 10, limits, features).Value;

        // Act & Assert Deactivate
        var deactivateResult = plan.Deactivate();
        deactivateResult.IsSuccess.Should().BeTrue();
        plan.IsActive.Should().BeFalse();

        // Already deactivated should fail
        var secondDeactivate = plan.Deactivate();
        secondDeactivate.IsFailure.Should().BeTrue();
        secondDeactivate.Error.Code.Should().Be("Plan.AlreadyInactive");

        // Reactivate
        var reactivateResult = plan.Reactivate();
        reactivateResult.IsSuccess.Should().BeTrue();
        plan.IsActive.Should().BeTrue();

        // Already active should fail
        var secondReactivate = plan.Reactivate();
        secondReactivate.IsFailure.Should().BeTrue();
        secondReactivate.Error.Code.Should().Be("Plan.AlreadyActive");
    }
}
