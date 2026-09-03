using FluentAssertions;
using Master.Domain.Tenants;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Unit tests for <see cref="Tenant"/> aggregate and subscription lifecycle.
/// </summary>
public sealed class TenantTests
{
    /// <summary>
    /// Verifies that new tenant creation defaults to Trial tier with 14 days expiration.
    /// </summary>
    [Fact]
    public void Create_ShouldInitializeWithDefaultTrialSubscriptionTier()
    {
        // Act
        var result = Tenant.Create("Agencia Beta", "12345678000190", "agencia-beta");

        // Assert
        result.IsSuccess.Should().BeTrue();
        var tenant = result.Value;
        tenant.Tier.Should().Be(SubscriptionTier.Trial);
        tenant.SubscriptionExpiresAtUtc.Should().NotBeNull();
        tenant.SubscriptionExpiresAtUtc!.Value.Should().BeAfter(DateTime.UtcNow);
        tenant.Status.Should().Be(TenantStatus.Active);
    }

    /// <summary>
    /// Verifies tenant creation with explicit subscription tier and expiration date.
    /// </summary>
    [Fact]
    public void Create_WithExplicitSubscriptionTier_ShouldSetTierAndExpiration()
    {
        // Arrange
        var expiresAt = DateTime.UtcNow.AddMonths(1);

        // Act
        var result = Tenant.Create("Agencia Pro", "12345678000190", "agencia-pro", SubscriptionTier.Pro, expiresAt);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var tenant = result.Value;
        tenant.Tier.Should().Be(SubscriptionTier.Pro);
        tenant.SubscriptionExpiresAtUtc.Should().Be(expiresAt);
    }

    /// <summary>
    /// Verifies upgrading the subscription tier updates both tier and expiration date.
    /// </summary>
    [Fact]
    public void UpgradeSubscription_ShouldUpdateTierAndExpiration()
    {
        // Arrange
        var tenant = Tenant.Create("Agencia Alfa", "12345678000190", "agencia-alfa").Value;
        var futureDate = DateTime.UtcNow.AddYears(1);

        // Act
        var upgradeResult = tenant.UpgradeSubscription(SubscriptionTier.Enterprise, futureDate);

        // Assert
        upgradeResult.IsSuccess.Should().BeTrue();
        tenant.Tier.Should().Be(SubscriptionTier.Enterprise);
        tenant.SubscriptionExpiresAtUtc.Should().Be(futureDate);
    }

    /// <summary>
    /// Verifies trial extension when expiration date is in the future.
    /// </summary>
    [Fact]
    public void ExtendTrial_ShouldUpdateExpiration_WhenTenantIsInTrial()
    {
        // Arrange
        var tenant = Tenant.Create("Agencia Trial", "12345678000190", "agencia-trial").Value;
        var extendedDate = DateTime.UtcNow.AddDays(30);

        // Act
        var extendResult = tenant.ExtendTrial(extendedDate);

        // Assert
        extendResult.IsSuccess.Should().BeTrue();
        tenant.SubscriptionExpiresAtUtc.Should().Be(extendedDate);
    }

    /// <summary>
    /// Verifies trial extension fails when proposed expiration date is in the past.
    /// </summary>
    [Fact]
    public void ExtendTrial_ShouldFail_WhenDateIsInThePast()
    {
        // Arrange
        var tenant = Tenant.Create("Agencia Trial", "12345678000190", "agencia-trial").Value;
        var pastDate = DateTime.UtcNow.AddDays(-1);

        // Act
        var extendResult = tenant.ExtendTrial(pastDate);

        // Assert
        extendResult.IsFailure.Should().BeTrue();
        extendResult.Error.Code.Should().Be("Tenant.InvalidExpirationDate");
    }

    /// <summary>
    /// Verifies suspending a tenant transitions status to Suspended.
    /// </summary>
    [Fact]
    public void Suspend_ShouldChangeStatusToSuspended()
    {
        // Arrange
        var tenant = Tenant.Create("Agencia Susp", "12345678000190", "agencia-susp").Value;

        // Act
        var suspendResult = tenant.Suspend("Inadimplência recorrente");

        // Assert
        suspendResult.IsSuccess.Should().BeTrue();
        tenant.Status.Should().Be(TenantStatus.Suspended);
    }

    /// <summary>
    /// Verifies reactivating a suspended tenant restores status to Active.
    /// </summary>
    [Fact]
    public void Reactivate_ShouldRestoreStatusToActive_WhenSuspended()
    {
        // Arrange
        var tenant = Tenant.Create("Agencia React", "12345678000190", "agencia-react").Value;
        tenant.Suspend("Manutenção");

        // Act
        var reactivateResult = tenant.Reactivate();

        // Assert
        reactivateResult.IsSuccess.Should().BeTrue();
        tenant.Status.Should().Be(TenantStatus.Active);
    }
}
