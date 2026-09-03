using FluentAssertions;
using Master.Application.Repositories;
using Master.Domain.Tenants;
using Master.Infrastructure.Persistence;
using Master.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Unit tests for <see cref="TenantReadOnlyRepository"/>.
/// </summary>
public sealed class TenantReadOnlyRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly MasterDbContext _masterDbContext;
    private readonly ITenantReadOnlyRepository _sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantReadOnlyRepositoryTests"/> class.
    /// </summary>
    public TenantReadOnlyRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<MasterDbContext>()
            .UseSqlite(_connection)
            .Options;

        _masterDbContext = new MasterDbContext(options);
        _masterDbContext.Database.EnsureCreated();

        _sut = new TenantReadOnlyRepository(_masterDbContext);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _masterDbContext.Dispose();
        _connection.Dispose();
    }

    /// <summary>
    /// Verifies that querying a non-existent tenant returns null.
    /// </summary>
    [Fact]
    public async Task GetDetailsByIdAsync_ShouldReturnNull_WhenTenantDoesNotExist()
    {
        // Arrange
        var nonExistentId = TenantId.New();

        // Act
        var result = await _sut.GetDetailsByIdAsync(nonExistentId, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that querying an existing tenant returns safe projected details without exposing sensitive connection string.
    /// </summary>
    [Fact]
    public async Task GetDetailsByIdAsync_ShouldReturnProjectedDetails_WhenTenantExists()
    {
        // Arrange
        var tenant = Tenant.Create("Omni Media", "99888777000166", "omni-media", SubscriptionTier.Enterprise).Value;
        tenant.SetEncryptedConnectionString("sensitive-db-secret");

        await _masterDbContext.Tenants.AddAsync(tenant);
        await _masterDbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetDetailsByIdAsync(tenant.Id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(tenant.Id.Value);
        result.CompanyName.Should().Be("Omni Media");
        result.Cnpj.Should().Be("99888777000166");
        result.Subdomain.Should().Be("omni-media");
        result.Status.Should().Be(tenant.Status.ToString());
        result.Tier.Should().Be(SubscriptionTier.Enterprise.ToString());
        result.SubscriptionExpiresAtUtc.Should().Be(tenant.SubscriptionExpiresAtUtc);
    }

    /// <summary>
    /// Verifies retrieving all tenants projected for directory listings.
    /// </summary>
    [Fact]
    public async Task GetAllAsync_ShouldReturnAllTenants()
    {
        // Arrange
        var tenant1 = Tenant.Create("Empresa Um", "11111111000111", "empresa-um").Value;
        var tenant2 = Tenant.Create("Empresa Dois", "22222222000222", "empresa-dois").Value;

        await _masterDbContext.Tenants.AddRangeAsync(tenant1, tenant2);
        await _masterDbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetAllAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Select(x => x.Subdomain).Should().Contain(["empresa-um", "empresa-dois"]);
    }

    /// <summary>
    /// Verifies existence check by tenant identifier.
    /// </summary>
    [Fact]
    public async Task ExistsAsync_ShouldReturnExpectedResult()
    {
        // Arrange
        var tenant = Tenant.Create("Empresa Três", "33333333000333", "empresa-tres").Value;
        await _masterDbContext.Tenants.AddAsync(tenant);
        await _masterDbContext.SaveChangesAsync();

        // Act & Assert
        (await _sut.ExistsAsync(tenant.Id, CancellationToken.None)).Should().BeTrue();
        (await _sut.ExistsAsync(TenantId.New(), CancellationToken.None)).Should().BeFalse();
    }
}
