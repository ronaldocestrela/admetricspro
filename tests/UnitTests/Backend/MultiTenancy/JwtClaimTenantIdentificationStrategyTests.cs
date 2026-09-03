using System.Security.Claims;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Infrastructure.MultiTenancy;
using BuildingBlocks.Infrastructure.MultiTenancy.Strategies;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace UnitTests.Backend.MultiTenancy;

/// <summary>
/// Unit tests for <see cref="JwtClaimTenantIdentificationStrategy"/>.
/// </summary>
public sealed class JwtClaimTenantIdentificationStrategyTests
{
    private readonly JwtClaimTenantIdentificationStrategy _strategy;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtClaimTenantIdentificationStrategyTests"/> class.
    /// </summary>
    public JwtClaimTenantIdentificationStrategyTests()
    {
        var options = Options.Create(new TenantResolutionOptions
        {
            JwtClaimType = "tenant_id"
        });
        _strategy = new JwtClaimTenantIdentificationStrategy(options);
    }

    /// <summary>
    /// Verifies that a valid GUID inside the configured JWT claim resolves to TenantId.
    /// </summary>
    [Fact]
    public async Task IdentifyTenantAsync_WhenClaimContainsValidGuid_ShouldResolveTenantId()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var expectedTenantId = Guid.NewGuid();
        var identity = new ClaimsIdentity(
            [new Claim("tenant_id", expectedTenantId.ToString())],
            "TestAuth");
        context.User = new ClaimsPrincipal(identity);

        // Act
        var result = await _strategy.IdentifyTenantAsync(context);

        // Assert
        result.Should().NotBeNull();
        result!.TenantId.Should().Be(expectedTenantId);
        result.Subdomain.Should().BeNull();
        result.Source.Should().Be(TenantResolutionSource.JwtClaim);
    }

    /// <summary>
    /// Verifies that an alphanumeric slug in the configured JWT claim resolves to Subdomain.
    /// </summary>
    [Fact]
    public async Task IdentifyTenantAsync_WhenClaimContainsSubdomainSlug_ShouldResolveSubdomain()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var identity = new ClaimsIdentity(
            [new Claim("tenant_id", "squad-growth")],
            "TestAuth");
        context.User = new ClaimsPrincipal(identity);

        // Act
        var result = await _strategy.IdentifyTenantAsync(context);

        // Assert
        result.Should().NotBeNull();
        result!.TenantId.Should().BeNull();
        result.Subdomain.Should().Be("squad-growth");
        result.Source.Should().Be(TenantResolutionSource.JwtClaim);
    }

    /// <summary>
    /// Verifies that the standard Microsoft tenant claim schema is supported as fallback.
    /// </summary>
    [Fact]
    public async Task IdentifyTenantAsync_WhenStandardMicrosoftClaimPresent_ShouldResolveTenantId()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var expectedTenantId = Guid.NewGuid();
        var identity = new ClaimsIdentity(
            [new Claim("http://schemas.microsoft.com/identity/claims/tenantid", expectedTenantId.ToString())],
            "TestAuth");
        context.User = new ClaimsPrincipal(identity);

        // Act
        var result = await _strategy.IdentifyTenantAsync(context);

        // Assert
        result.Should().NotBeNull();
        result!.TenantId.Should().Be(expectedTenantId);
        result.Source.Should().Be(TenantResolutionSource.JwtClaim);
    }

    /// <summary>
    /// Verifies that unauthenticated user principals return null.
    /// </summary>
    [Fact]
    public async Task IdentifyTenantAsync_WhenUserIsNotAuthenticated_ShouldReturnNull()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var result = await _strategy.IdentifyTenantAsync(context);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that authenticated user principals without tenant claims return null.
    /// </summary>
    [Fact]
    public async Task IdentifyTenantAsync_WhenClaimIsMissing_ShouldReturnNull()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var identity = new ClaimsIdentity(
            [new Claim("email", "user@example.com")],
            "TestAuth");
        context.User = new ClaimsPrincipal(identity);

        // Act
        var result = await _strategy.IdentifyTenantAsync(context);

        // Assert
        result.Should().BeNull();
    }
}
