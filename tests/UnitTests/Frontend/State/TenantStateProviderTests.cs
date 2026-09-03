using FluentAssertions;
using WebApp.State;

namespace UnitTests.Frontend.State;

/// <summary>
/// Testes unitários para o provedor de estado de tenant no circuito Blazor.
/// </summary>
public class TenantStateProviderTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultTenantState()
    {
        // Arrange & Act
        var provider = new TenantStateProvider();

        // Assert
        provider.CurrentTenant.Should().NotBeNull();
        provider.CurrentTenant.Name.Should().Be("AdMetricsPro");
        provider.CurrentTenant.Branding.Should().NotBeNull();
        provider.CurrentTenant.Branding.ShowPoweredBy.Should().BeTrue();
        provider.IsInitialized.Should().BeTrue();
    }

    [Fact]
    public void SetTenant_ShouldUpdateCurrentTenant_AndRaiseOnTenantChangedEvent()
    {
        // Arrange
        var provider = new TenantStateProvider();
        var eventRaised = false;
        provider.OnTenantChanged += () => eventRaised = true;

        var customBranding = new TenantBranding(
            PrimaryColor: "#7C3AED",
            SecondaryColor: "#0F172A",
            AccentColor: "#A78BFA",
            LogoUrl: "https://cdn.example.com/logo.png",
            DarkLogoUrl: "https://cdn.example.com/dark-logo.png",
            FaviconUrl: "https://cdn.example.com/favicon.ico",
            CompanyName: "Agência Alfa",
            ShowPoweredBy: false);

        var tenantId = Guid.NewGuid();
        var newTenant = new TenantState(
            TenantId: tenantId,
            Name: "Agência Alfa",
            Slug: "agencia-alfa",
            CustomDomain: "analytics.agenciaalfa.com.br",
            Branding: customBranding);

        // Act
        provider.SetTenant(newTenant);

        // Assert
        eventRaised.Should().BeTrue();
        provider.CurrentTenant.Should().Be(newTenant);
        provider.CurrentTenant.TenantId.Should().Be(tenantId);
        provider.CurrentTenant.Name.Should().Be("Agência Alfa");
        provider.CurrentTenant.Slug.Should().Be("agencia-alfa");
        provider.CurrentTenant.CustomDomain.Should().Be("analytics.agenciaalfa.com.br");
        provider.CurrentTenant.Branding.ShowPoweredBy.Should().BeFalse();
        provider.CurrentTenant.Branding.PrimaryColor.Should().Be("#7C3AED");
    }

    [Fact]
    public void ToCssVariables_ShouldGenerateValidCssProperties()
    {
        // Arrange
        var branding = new TenantBranding(
            PrimaryColor: "#10B981",
            SecondaryColor: "#064E3B",
            AccentColor: "#34D399",
            LogoUrl: "/images/custom-logo.svg",
            DarkLogoUrl: null,
            FaviconUrl: "/favicon-custom.ico",
            CompanyName: "Beta Ads",
            ShowPoweredBy: true);

        // Act
        var cssVariables = branding.ToCssVariables();

        // Assert
        cssVariables.Should().Contain("--tenant-primary: #10B981;");
        cssVariables.Should().Contain("--tenant-secondary: #064E3B;");
        cssVariables.Should().Contain("--tenant-accent: #34D399;");
    }

    [Fact]
    public async Task InitializeAsync_WhenTenantProvided_ShouldSetState()
    {
        // Arrange
        var provider = new TenantStateProvider();
        var tenantId = Guid.NewGuid();
        var branding = TenantBranding.Default;
        var customTenant = new TenantState(tenantId, "Tenant Test", "tenant-test", null, branding);

        // Act
        await provider.InitializeAsync(customTenant);

        // Assert
        provider.CurrentTenant.Should().Be(customTenant);
        provider.IsInitialized.Should().BeTrue();
    }
}
