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

    /// <summary>
    /// Verifies tenant creation with optional onboarding profile and branding parameters.
    /// </summary>
    [Fact]
    public void Create_WithProfileAndBranding_ShouldSetAllProperties()
    {
        // Act
        var result = Tenant.Create(
            companyName: "Vanguarda Digital",
            cnpj: "12345678000195",
            subdomain: "vanguarda",
            tier: SubscriptionTier.Pro,
            subscriptionExpiresAtUtc: null,
            segment: "Agência de Performance",
            monthlyAdSpendRange: "R$ 20k a R$ 100k",
            billingCycle: "Monthly",
            customDomain: "ads.vanguardadigital.com.br",
            primaryColor: "#4f46e5",
            secondaryColor: "#0f172a");

        // Assert
        result.IsSuccess.Should().BeTrue();
        var tenant = result.Value;
        tenant.Segment.Should().Be("Agência de Performance");
        tenant.MonthlyAdSpendRange.Should().Be("R$ 20k a R$ 100k");
        tenant.BillingCycle.Should().Be("Monthly");
        tenant.CustomDomain.Should().Be("ads.vanguardadigital.com.br");
        tenant.PrimaryColor.Should().Be("#4f46e5");
        tenant.SecondaryColor.Should().Be("#0f172a");
    }

    /// <summary>
    /// Verifies tenant creation fails validation when primary or secondary color is not a valid hex code.
    /// </summary>
    [Theory]
    [InlineData("invalid-color")]
    [InlineData("#12")]
    [InlineData("#GGGGGG")]
    [InlineData("4f46e5")]
    public void Create_WithInvalidHexColor_ShouldFailValidation(string invalidColor)
    {
        // Act
        var result = Tenant.Create(
            companyName: "Vanguarda Digital",
            cnpj: "12345678000195",
            subdomain: "vanguarda",
            primaryColor: invalidColor);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.InvalidColorHex");
    }

    /// <summary>
    /// Verifies tenant creation fails validation when custom domain contains protocol or whitespace.
    /// </summary>
    [Theory]
    [InlineData("https://ads.vanguarda.com")]
    [InlineData("http://ads.vanguarda.com")]
    [InlineData("ads vanguarda com")]
    public void Create_WithInvalidCustomDomain_ShouldFailValidation(string invalidDomain)
    {
        // Act
        var result = Tenant.Create(
            companyName: "Vanguarda Digital",
            cnpj: "12345678000195",
            subdomain: "vanguarda",
            customDomain: invalidDomain);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.InvalidCustomDomain");
    }

    /// <summary>
    /// Verifies UpdateBranding updates styling properties and normalizes valid values.
    /// </summary>
    [Fact]
    public void UpdateBranding_WithValidValues_ShouldUpdateProperties()
    {
        // Arrange
        var tenant = Tenant.Create("Agencia Beta", "12345678000190", "agencia-beta").Value;

        // Act
        var result = tenant.UpdateBranding("#00ff00", "#111111", "portal.agenciabeta.com.br");

        // Assert
        result.IsSuccess.Should().BeTrue();
        tenant.PrimaryColor.Should().Be("#00ff00");
        tenant.SecondaryColor.Should().Be("#111111");
        tenant.CustomDomain.Should().Be("portal.agenciabeta.com.br");
    }

    /// <summary>
    /// Verifies UpdateBusinessProfile updates segment and ad spend range properties.
    /// </summary>
    [Fact]
    public void UpdateBusinessProfile_WithValidValues_ShouldUpdateProperties()
    {
        // Arrange
        var tenant = Tenant.Create("Agencia Beta", "12345678000190", "agencia-beta").Value;

        // Act
        var result = tenant.UpdateBusinessProfile("E-commerce", "Acima de R$ 100k");

        // Assert
        result.IsSuccess.Should().BeTrue();
        tenant.Segment.Should().Be("E-commerce");
        tenant.MonthlyAdSpendRange.Should().Be("Acima de R$ 100k");
    }

    /// <summary>
    /// Verifies SetBillingCycle updates the billing cycle and validates accepted cycles.
    /// </summary>
    [Fact]
    public void SetBillingCycle_WithValidValues_ShouldUpdateBillingCycle()
    {
        // Arrange
        var tenant = Tenant.Create("Agencia Beta", "12345678000190", "agencia-beta").Value;

        // Act
        var result = tenant.SetBillingCycle("Annual");

        // Assert
        result.IsSuccess.Should().BeTrue();
        tenant.BillingCycle.Should().Be("Annual");
    }

    /// <summary>
    /// Verifies SetBillingCycle rejects unrecognized billing cycles.
    /// </summary>
    [Theory]
    [InlineData("Weekly")]
    [InlineData("InvalidCycle")]
    [InlineData("")]
    public void SetBillingCycle_WithInvalidCycle_ShouldReturnFailure(string invalidCycle)
    {
        // Arrange
        var tenant = Tenant.Create("Agencia Beta", "12345678000190", "agencia-beta").Value;

        // Act
        var result = tenant.SetBillingCycle(invalidCycle);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.InvalidBillingCycle");
    }
}
