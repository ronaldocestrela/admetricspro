using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace UnitTests.Backend.Persistence;

/// <summary>
/// Unit tests for <see cref="TenantDbContextFactory{TContext}"/>.
/// </summary>
public sealed class TenantDbContextFactoryTests
{
    private readonly FakeTenantConnectionResolver _resolver = new();

    /// <summary>
    /// Verifies CreateDbContextAsync by tenantId creates a DbContext with resolved connection.
    /// </summary>
    [Fact]
    public async Task CreateDbContextAsync_WithTenantId_ShouldReturnConfiguredDbContext_WhenResolverSucceeds()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        const string connectionString = "Server=sql.local;Database=Tenant_test;User Id=sa;Password=Secret!;";
        _resolver.SetupTenant(tenantId, connectionString);

        var factory = new TenantDbContextFactory<TenantDbContext>(_resolver);

        // Act
        var result = await factory.CreateDbContextAsync(tenantId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        var actualBuilder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(result.Value.Database.GetConnectionString());
        var expectedBuilder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
        actualBuilder.DataSource.Should().Be(expectedBuilder.DataSource);
        actualBuilder.InitialCatalog.Should().Be(expectedBuilder.InitialCatalog);
    }

    /// <summary>
    /// Verifies CreateDbContextAsync by tenantId returns failure when resolver fails.
    /// </summary>
    [Fact]
    public async Task CreateDbContextAsync_WithTenantId_ShouldReturnFailure_WhenResolverFails()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var factory = new TenantDbContextFactory<TenantDbContext>(_resolver);

        // Act
        var result = await factory.CreateDbContextAsync(tenantId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.NotFound");
    }

    /// <summary>
    /// Verifies CreateDbContextAsync for current tenant creates a DbContext when resolver succeeds.
    /// </summary>
    [Fact]
    public async Task CreateDbContextAsync_ForCurrentTenant_ShouldReturnConfiguredDbContext_WhenResolverSucceeds()
    {
        // Arrange
        const string connectionString = "Server=sql.local;Database=Tenant_current;User Id=sa;Password=Secret!;";
        _resolver.CurrentConnectionString = connectionString;

        var factory = new TenantDbContextFactory<TenantDbContext>(_resolver);

        // Act
        var result = await factory.CreateDbContextAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        var actualBuilder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(result.Value.Database.GetConnectionString());
        var expectedBuilder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
        actualBuilder.DataSource.Should().Be(expectedBuilder.DataSource);
        actualBuilder.InitialCatalog.Should().Be(expectedBuilder.InitialCatalog);
    }

    /// <summary>
    /// Verifies CreateDbContextAsync for current tenant returns failure when resolver fails.
    /// </summary>
    [Fact]
    public async Task CreateDbContextAsync_ForCurrentTenant_ShouldReturnFailure_WhenResolverFails()
    {
        // Arrange
        _resolver.CurrentConnectionString = null;
        var factory = new TenantDbContextFactory<TenantDbContext>(_resolver);

        // Act
        var result = await factory.CreateDbContextAsync();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.ContextNotResolved");
    }

    private sealed class FakeTenantConnectionResolver : ITenantConnectionResolver
    {
        private readonly Dictionary<Guid, string> _connections = new();
        public string? CurrentConnectionString { get; set; }

        public void SetupTenant(Guid tenantId, string connectionString)
        {
            _connections[tenantId] = connectionString;
        }

        public Task<Result<string>> ResolveConnectionStringAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            if (_connections.TryGetValue(tenantId, out var connection))
            {
                return Task.FromResult(Result<string>.Success(connection));
            }

            return Task.FromResult(Result<string>.Failure(Error.NotFound("Tenant.NotFound", "Tenant not found.")));
        }

        public Task<Result<string>> ResolveConnectionStringBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result<string>.Failure(Error.NotFound("Tenant.NotFound", "Subdomain not found.")));
        }

        public Task<Result<string>> ResolveCurrentTenantConnectionStringAsync(CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrWhiteSpace(CurrentConnectionString))
            {
                return Task.FromResult(Result<string>.Success(CurrentConnectionString));
            }

            return Task.FromResult(Result<string>.Failure(Error.NotFound("Tenant.ContextNotResolved", "Current context not resolved.")));
        }

        public void InvalidateCache(Guid tenantId)
        {
            _connections.Remove(tenantId);
        }

        public void InvalidateCacheBySubdomain(string subdomain)
        {
        }
    }
}
