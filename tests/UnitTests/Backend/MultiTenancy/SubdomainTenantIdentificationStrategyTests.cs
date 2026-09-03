using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Infrastructure.MultiTenancy;
using BuildingBlocks.Infrastructure.MultiTenancy.Strategies;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace UnitTests.Backend.MultiTenancy;

/// <summary>
/// Unit tests for <see cref="SubdomainTenantIdentificationStrategy"/>.
/// </summary>
public sealed class SubdomainTenantIdentificationStrategyTests
{
    private readonly SubdomainTenantIdentificationStrategy _strategy;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubdomainTenantIdentificationStrategyTests"/> class.
    /// </summary>
    public SubdomainTenantIdentificationStrategyTests()
    {
        var options = Options.Create(new TenantResolutionOptions
        {
            BaseDomains = ["admetricspro.com", "localhost"]
        });
        _strategy = new SubdomainTenantIdentificationStrategy(options);
    }

    /// <summary>
    /// Verifies that valid subdomains under configured base domains are identified.
    /// </summary>
    /// <param name="host">Host string under test.</param>
    /// <param name="expectedSubdomain">Expected extracted subdomain slug.</param>
    [Theory]
    [InlineData("agencia-alfa.admetricspro.com", "agencia-alfa")]
    [InlineData("squad-performance.admetricspro.com:5001", "squad-performance")]
    [InlineData("cliente-beta.localhost", "cliente-beta")]
    [InlineData("cliente-beta.localhost:5000", "cliente-beta")]
    public async Task IdentifyTenantAsync_WhenHostContainsValidSubdomain_ShouldResolveSubdomain(string host, string expectedSubdomain)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString(host);

        // Act
        var result = await _strategy.IdentifyTenantAsync(context);

        // Assert
        result.Should().NotBeNull();
        result!.TenantId.Should().BeNull();
        result.Subdomain.Should().Be(expectedSubdomain);
        result.Source.Should().Be(TenantResolutionSource.Subdomain);
    }

    /// <summary>
    /// Verifies that base domains without subdomains or direct IP requests return null.
    /// </summary>
    /// <param name="host">Host string representing base domain or raw IP address.</param>
    [Theory]
    [InlineData("admetricspro.com")]
    [InlineData("localhost")]
    [InlineData("localhost:5000")]
    [InlineData("127.0.0.1")]
    [InlineData("192.168.1.100:8080")]
    public async Task IdentifyTenantAsync_WhenHostIsBaseDomainOrIpWithoutSubdomain_ShouldReturnNull(string host)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString(host);

        // Act
        var result = await _strategy.IdentifyTenantAsync(context);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that reserved system subdomains (e.g., www, api, app) do not resolve as tenant instances.
    /// </summary>
    /// <param name="host">Host string containing a reserved prefix.</param>
    [Theory]
    [InlineData("www.admetricspro.com")]
    [InlineData("api.admetricspro.com")]
    [InlineData("app.admetricspro.com")]
    public async Task IdentifyTenantAsync_WhenSubdomainIsReserved_ShouldReturnNull(string host)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString(host);

        // Act
        var result = await _strategy.IdentifyTenantAsync(context);

        // Assert
        result.Should().BeNull();
    }
}
