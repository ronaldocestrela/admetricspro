using System.Security.Claims;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Infrastructure.MultiTenancy;
using BuildingBlocks.Infrastructure.MultiTenancy.Strategies;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace UnitTests.Backend.MultiTenancy;

/// <summary>
/// Unit tests for <see cref="TenantIdentificationMiddleware"/>.
/// </summary>
public sealed class TenantIdentificationMiddlewareTests
{
    private readonly TenantContextAccessor _contextAccessor = new();
    private readonly TenantResolutionOptions _options = new()
    {
        HeaderName = "X-Tenant-Id",
        JwtClaimType = "tenant_id",
        BaseDomains = ["admetricspro.com", "localhost"]
    };

    private TenantIdentificationMiddleware CreateMiddleware(RequestDelegate? next = null)
    {
        var strategies = new ITenantIdentificationStrategy[]
        {
            new HeaderTenantIdentificationStrategy(Options.Create(_options)),
            new JwtClaimTenantIdentificationStrategy(Options.Create(_options)),
            new SubdomainTenantIdentificationStrategy(Options.Create(_options))
        };

        return new TenantIdentificationMiddleware(
            next ?? (_ => Task.CompletedTask),
            strategies,
            Options.Create(_options));
    }

    /// <summary>
    /// Verifies that providing the tenant header updates the context accessor with tenant identity.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WhenHeaderProvided_ShouldPopulateTenantContext()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();
        var tenantId = Guid.NewGuid();
        context.Request.Headers["X-Tenant-Id"] = tenantId.ToString();

        // Act
        await middleware.InvokeAsync(context, _contextAccessor);

        // Assert
        _contextAccessor.TenantContext.IsResolved.Should().BeTrue();
        _contextAccessor.TenantContext.TenantId.Should().Be(tenantId);
        _contextAccessor.TenantContext.Source.Should().Be(TenantResolutionSource.Header);
    }

    /// <summary>
    /// Verifies that the header strategy takes precedence over host subdomain when both are present.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WhenHeaderAndSubdomainPresent_HeaderTakesPrecedence()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();
        var headerTenantId = Guid.NewGuid();
        context.Request.Headers["X-Tenant-Id"] = headerTenantId.ToString();
        context.Request.Host = new HostString("outra-empresa.admetricspro.com");

        // Act
        await middleware.InvokeAsync(context, _contextAccessor);

        // Assert
        _contextAccessor.TenantContext.IsResolved.Should().BeTrue();
        _contextAccessor.TenantContext.TenantId.Should().Be(headerTenantId);
        _contextAccessor.TenantContext.Source.Should().Be(TenantResolutionSource.Header);
    }

    /// <summary>
    /// Verifies that when no header is present, host subdomain resolution is utilized.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WhenSubdomainPresentAndNoHeader_ResolvesFromSubdomain()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("agencia-alfa.admetricspro.com");

        // Act
        await middleware.InvokeAsync(context, _contextAccessor);

        // Assert
        _contextAccessor.TenantContext.IsResolved.Should().BeTrue();
        _contextAccessor.TenantContext.Subdomain.Should().Be("agencia-alfa");
        _contextAccessor.TenantContext.Source.Should().Be(TenantResolutionSource.Subdomain);
    }

    /// <summary>
    /// Verifies that when no header is present, authenticated JWT claims resolve tenant identity.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WhenJwtClaimPresentAndNoHeader_ResolvesFromJwtClaim()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();
        var tenantId = Guid.NewGuid();
        var identity = new ClaimsIdentity([new Claim("tenant_id", tenantId.ToString())], "TestAuth");
        context.User = new ClaimsPrincipal(identity);

        // Act
        await middleware.InvokeAsync(context, _contextAccessor);

        // Assert
        _contextAccessor.TenantContext.IsResolved.Should().BeTrue();
        _contextAccessor.TenantContext.TenantId.Should().Be(tenantId);
        _contextAccessor.TenantContext.Source.Should().Be(TenantResolutionSource.JwtClaim);
    }

    /// <summary>
    /// Verifies that requests without tenant identifiers gracefully resolve to an empty context.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WhenNoTenantIdentifiersPresent_ShouldRemainUnresolvedGracefully()
    {
        // Arrange
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("admetricspro.com");

        // Act
        await middleware.InvokeAsync(context, _contextAccessor);

        // Assert
        _contextAccessor.TenantContext.IsResolved.Should().BeFalse();
        _contextAccessor.TenantContext.TenantId.Should().BeNull();
        _contextAccessor.TenantContext.Subdomain.Should().BeNull();
        _contextAccessor.TenantContext.Source.Should().Be(TenantResolutionSource.None);
    }

    /// <summary>
    /// Verifies that the middleware always forwards the request to the next component in the pipeline.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_ShouldCallNextMiddlewareInPipeline()
    {
        // Arrange
        var nextInvoked = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextInvoked = true;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext();

        // Act
        await middleware.InvokeAsync(context, _contextAccessor);

        // Assert
        nextInvoked.Should().BeTrue();
    }
}
