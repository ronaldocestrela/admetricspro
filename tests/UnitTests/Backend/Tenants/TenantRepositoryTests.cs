using BuildingBlocks.Application.Persistence;
using FluentAssertions;
using Master.Application.Repositories;
using Master.Domain.Tenants;
using Master.Infrastructure.Persistence;
using Master.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Unit tests for <see cref="TenantRepository"/> and <see cref="UnitOfWork"/>.
/// </summary>
public sealed class TenantRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly MasterDbContext _masterDbContext;
    private readonly ITenantRepository _sut;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantRepositoryTests"/> class.
    /// </summary>
    public TenantRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<MasterDbContext>()
            .UseSqlite(_connection)
            .Options;

        _masterDbContext = new MasterDbContext(options);
        _masterDbContext.Database.EnsureCreated();

        _sut = new TenantRepository(_masterDbContext);
        _unitOfWork = new UnitOfWork(_masterDbContext);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _masterDbContext.Dispose();
        _connection.Dispose();
    }

    /// <summary>
    /// Verifies adding and retrieving a tenant by identifier.
    /// </summary>
    [Fact]
    public async Task AddAsync_And_GetByIdAsync_ShouldPersistAndRetrieveTenant()
    {
        // Arrange
        var tenant = Tenant.Create("Agencia Matrix", "12345678000100", "agencia-matrix").Value;
        tenant.SetEncryptedConnectionString("dummy-cipher");

        // Act
        await _sut.AddAsync(tenant, CancellationToken.None);
        var affectedRows = await _unitOfWork.CommitAsync(CancellationToken.None);

        var retrieved = await _sut.GetByIdAsync(tenant.Id, CancellationToken.None);

        // Assert
        affectedRows.Should().BeGreaterThan(0);
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(tenant.Id);
        retrieved.CompanyName.Should().Be("Agencia Matrix");
        retrieved.Subdomain.Should().Be("agencia-matrix");
        retrieved.Cnpj.Should().Be("12345678000100");
        retrieved.EncryptedConnectionString.Should().Be("dummy-cipher");
    }

    /// <summary>
    /// Verifies retrieving a tenant by normalized subdomain.
    /// </summary>
    [Fact]
    public async Task GetBySubdomainAsync_ShouldRetrieveTenantByNormalizedSubdomain()
    {
        // Arrange
        var tenant = Tenant.Create("Agencia Orbit", "98765432000199", "agencia-orbit").Value;
        tenant.SetEncryptedConnectionString("dummy-cipher");
        await _sut.AddAsync(tenant, CancellationToken.None);
        await _unitOfWork.CommitAsync(CancellationToken.None);

        // Act
        var retrievedUpper = await _sut.GetBySubdomainAsync("AGENCIA-ORBIT", CancellationToken.None);
        var retrievedLower = await _sut.GetBySubdomainAsync("agencia-orbit", CancellationToken.None);
        var notFound = await _sut.GetBySubdomainAsync("inexistent-subdomain", CancellationToken.None);

        // Assert
        retrievedUpper.Should().NotBeNull();
        retrievedUpper!.Id.Should().Be(tenant.Id);
        retrievedLower.Should().NotBeNull();
        retrievedLower!.Id.Should().Be(tenant.Id);
        notFound.Should().BeNull();
    }

    /// <summary>
    /// Verifies updating and deleting a tenant aggregate through repository.
    /// </summary>
    [Fact]
    public async Task Update_And_Remove_ShouldModifyAndRemoveTenant()
    {
        // Arrange
        var tenant = Tenant.Create("Agencia Prime", "11122233000144", "agencia-prime").Value;
        tenant.SetEncryptedConnectionString("cipher-1");
        await _sut.AddAsync(tenant);
        await _unitOfWork.CommitAsync();

        // Act - Update
        tenant.SetEncryptedConnectionString("cipher-2");
        _sut.Update(tenant);
        await _unitOfWork.CommitAsync();

        var updated = await _sut.GetByIdAsync(tenant.Id);
        updated.Should().NotBeNull();
        updated!.EncryptedConnectionString.Should().Be("cipher-2");

        // Act - Remove
        _sut.Remove(tenant);
        await _unitOfWork.CommitAsync();

        var removed = await _sut.GetByIdAsync(tenant.Id);
        removed.Should().BeNull();
    }
}
