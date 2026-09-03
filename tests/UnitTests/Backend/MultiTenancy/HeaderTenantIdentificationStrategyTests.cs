using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Infrastructure.MultiTenancy;
using BuildingBlocks.Infrastructure.MultiTenancy.Strategies;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace UnitTests.Backend.MultiTenancy;

/// <summary>
/// Unit tests for <see cref="HeaderTenantIdentificationStrategy"/>.
/// </summary>
public sealed class HeaderTenantIdentificationStrategyTests
{
    private readonly HeaderTenantIdentificationStrategy _strategy;

    /// <summary>
    /// Initializes a new instance of the <see cref="HeaderTenantIdentificationStrategyTests"/> class.
    /// </summary>
    public HeaderTenantIdentificationStrategyTests()
    {
        var options = Options.Create(new TenantResolutionOptions
        {
            HeaderName = "X-Tenant-Id"
        });
        _strategy = new HeaderTenantIdentificationStrategy(options);
    }

    /// <summary>
    /// Verifies that a valid GUID string in the tenant header is parsed as TenantId.
    /// </summary>
    [Fact]
    public async Task IdentifyTenantAsync_WhenHeaderContainsValidGuid_ShouldResolveTenantId()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var expectedTenantId = Guid.NewGuid();
        context.Request.Headers["X-Tenant-Id"] = expectedTenantId.ToString();

        // Act
        var result = await _strategy.IdentifyTenantAsync(context);

        // Assert
        result.Should().NotBeNull();
        result!.TenantId.Should().Be(expectedTenantId);
        result.Subdomain.Should().BeNull();
        result.Source.Should().Be(TenantResolutionSource.Header);
    }

    /// <summary>
    /// Verifies that an alphanumeric slug in the tenant header is parsed as Subdomain.
    /// </summary>
    [Fact]
    public async Task IdentifyTenantAsync_WhenHeaderContainsSubdomainSlug_ShouldResolveSubdomain()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Tenant-Id"] = "agencia-digital";

        // Act
        var result = await _strategy.IdentifyTenantAsync(context);

        // Assert
        result.Should().NotBeNull();
        result!.TenantId.Should().BeNull();
        result.Subdomain.Should().Be("agencia-digital");
        result.Source.Should().Be(TenantResolutionSource.Header);
    }

    /// <summary>
    /// Verifies that empty or whitespace header values return null without failing.
    /// </summary>
    /// <param name="headerValue">The empty or whitespace header test value.</param>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task IdentifyTenantAsync_WhenHeaderIsEmptyOrWhitespace_ShouldReturnNull(string headerValue)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Tenant-Id"] = headerValue;

        // Act
        var result = await _strategy.IdentifyTenantAsync(context);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that a missing header returns null without throwing exceptions.
    /// </summary>
    [Fact]
    public async Task IdentifyTenantAsync_WhenHeaderIsMissing_ShouldReturnNull()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act
        var result = await _strategy.IdentifyTenantAsync(context);

        // Assert
        result.Should().BeNull();
    }
}
