using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Infrastructure.MultiTenancy;
using BuildingBlocks.Infrastructure.MultiTenancy.Strategies;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace UnitTests.Backend.MultiTenancy;

/// <summary>
/// Unit tests for <see cref="CustomDomainTenantIdentificationStrategy"/>.
/// </summary>
public sealed class CustomDomainTenantIdentificationStrategyTests
{
    private readonly ITenantCustomDomainResolver _resolver = Substitute.For<ITenantCustomDomainResolver>();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly CustomDomainTenantIdentificationStrategy _strategy;

    /// <summary>
    /// Initializes a new instance of <see cref="CustomDomainTenantIdentificationStrategyTests"/>.
    /// </summary>
    public CustomDomainTenantIdentificationStrategyTests()
    {
        var options = Options.Create(new TenantResolutionOptions
        {
            BaseDomains = ["admetricspro.com", "localhost"]
        });

        _strategy = new CustomDomainTenantIdentificationStrategy(options, _resolver, _cache);
    }

    /// <summary>
    /// Verifies that when host matches an agency's custom CNAME, the tenant is resolved with CustomDomain source.
    /// </summary>
    [Fact]
    public async Task IdentifyTenantAsync_WhenCustomDomainMatches_ShouldResolveTenant()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        const string customHost = "relatorios.agenciaalfa.com.br";
        const string subdomain = "agencia-alfa";

        _resolver.ResolveTenantByCustomDomainAsync(customHost, Arg.Any<CancellationToken>())
            .Returns(new TenantCustomDomainMapping(tenantId, subdomain, customHost));

        var context = new DefaultHttpContext();
        context.Request.Host = new HostString(customHost);

        // Act
        var result = await _strategy.IdentifyTenantAsync(context);

        // Assert
        result.Should().NotBeNull();
        result!.TenantId.Should().Be(tenantId);
        result.Subdomain.Should().Be(subdomain);
        result.Source.Should().Be(TenantResolutionSource.CustomDomain);
        result.RawIdentifier.Should().Be(customHost);
    }

    /// <summary>
    /// Verifies that base domains are ignored by this strategy so Subdomain strategy can handle them.
    /// </summary>
    [Theory]
    [InlineData("agencia.admetricspro.com")]
    [InlineData("cliente.localhost")]
    [InlineData("127.0.0.1")]
    [InlineData("192.168.1.100")]
    public async Task IdentifyTenantAsync_WhenHostIsBaseDomainOrIp_ShouldReturnNull(string host)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString(host);

        // Act
        var result = await _strategy.IdentifyTenantAsync(context);

        // Assert
        result.Should().BeNull();
        await _resolver.DidNotReceive().ResolveTenantByCustomDomainAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that unknown custom domains return null and do not crash.
    /// </summary>
    [Fact]
    public async Task IdentifyTenantAsync_WhenCustomDomainNotFound_ShouldReturnNull()
    {
        // Arrange
        const string unknownHost = "unknown.domain.com";
        _resolver.ResolveTenantByCustomDomainAsync(unknownHost, Arg.Any<CancellationToken>())
            .Returns((TenantCustomDomainMapping?)null);

        var context = new DefaultHttpContext();
        context.Request.Host = new HostString(unknownHost);

        // Act
        var result = await _strategy.IdentifyTenantAsync(context);

        // Assert
        result.Should().BeNull();
    }
}
