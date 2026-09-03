using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Infrastructure.MultiTenancy;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace UnitTests.Backend.MultiTenancy;

/// <summary>
/// Unit tests for <see cref="MultiTenancyServiceExtensions"/>.
/// </summary>
public sealed class MultiTenancyServiceExtensionsTests
{
    /// <summary>
    /// Verifies that AddMultiTenancy registers the context accessor, scoped context, and default identification strategies.
    /// </summary>
    [Fact]
    public void AddMultiTenancy_ShouldRegisterCoreServicesAndStrategies()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMultiTenancy(options =>
        {
            options.HeaderName = "X-Custom-Tenant";
        });

        using var provider = services.BuildServiceProvider();

        // Assert
        var accessor = provider.GetService<ITenantContextAccessor>();
        accessor.Should().NotBeNull();

        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetService<ITenantContext>();
        context.Should().NotBeNull();
        context!.IsResolved.Should().BeFalse();

        var strategies = provider.GetServices<ITenantIdentificationStrategy>().ToList();
        strategies.Should().HaveCount(3);
    }
}
