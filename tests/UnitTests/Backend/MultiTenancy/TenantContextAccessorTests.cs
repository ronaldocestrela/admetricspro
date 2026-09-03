using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Infrastructure.MultiTenancy;
using FluentAssertions;

namespace UnitTests.Backend.MultiTenancy;

/// <summary>
/// Unit tests for <see cref="TenantContextAccessor"/> and context state propagation.
/// </summary>
public sealed class TenantContextAccessorTests
{
    /// <summary>
    /// Verifies that accessing the context without prior assignment returns an empty, unresolved instance.
    /// </summary>
    [Fact]
    public void TenantContext_DefaultValue_ShouldReturnEmptyContext()
    {
        // Arrange
        var accessor = new TenantContextAccessor();

        // Act
        var context = accessor.TenantContext;

        // Assert
        context.Should().NotBeNull();
        context.IsResolved.Should().BeFalse();
        context.TenantId.Should().BeNull();
        context.Subdomain.Should().BeNull();
        context.Source.Should().Be(TenantResolutionSource.None);
    }

    /// <summary>
    /// Verifies that assigning a valid tenant context updates the accessor property.
    /// </summary>
    [Fact]
    public void TenantContext_WhenSet_ShouldReturnAssignedContext()
    {
        // Arrange
        var accessor = new TenantContextAccessor();
        var tenantId = Guid.NewGuid();
        var expectedContext = TenantContext.Create(tenantId, "empresa-alfa", TenantResolutionSource.Header);

        // Act
        accessor.TenantContext = expectedContext;

        // Assert
        accessor.TenantContext.Should().BeSameAs(expectedContext);
        accessor.TenantContext.TenantId.Should().Be(tenantId);
        accessor.TenantContext.Subdomain.Should().Be("empresa-alfa");
        accessor.TenantContext.Source.Should().Be(TenantResolutionSource.Header);
        accessor.TenantContext.IsResolved.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that asynchronous execution flows maintain isolated contexts without leaking state across tasks.
    /// </summary>
    [Fact]
    public async Task TenantContext_AcrossAsyncFlows_ShouldMaintainIsolation()
    {
        // Arrange
        var accessor = new TenantContextAccessor();
        var tenant1 = TenantContext.Create(Guid.NewGuid(), "tenant-1", TenantResolutionSource.Header);
        var tenant2 = TenantContext.Create(Guid.NewGuid(), "tenant-2", TenantResolutionSource.Subdomain);

        // Act & Assert
        var task1 = Task.Run(async () =>
        {
            accessor.TenantContext = tenant1;
            await Task.Delay(20);
            accessor.TenantContext.TenantId.Should().Be(tenant1.TenantId);
            accessor.TenantContext.Subdomain.Should().Be("tenant-1");
        });

        var task2 = Task.Run(async () =>
        {
            accessor.TenantContext = tenant2;
            await Task.Delay(20);
            accessor.TenantContext.TenantId.Should().Be(tenant2.TenantId);
            accessor.TenantContext.Subdomain.Should().Be("tenant-2");
        });

        await Task.WhenAll(task1, task2);
    }
}
