using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Infrastructure.Security;
using FluentAssertions;
using IntegrationTests.Infrastructure;
using Master.Application.Repositories;
using Master.Infrastructure.Persistence;
using Master.Infrastructure.Repositories;
using Master.Infrastructure.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace IntegrationTests;

/// <summary>
/// Integration tests for tenant database provisioning service.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class TenantProvisioningServiceTests
{
    private static readonly string EncryptionKey = Convert.ToBase64String(new byte[32]
    {
        21, 75, 14, 111, 32, 53, 198, 77,
        91, 62, 45, 219, 140, 231, 109, 88,
        16, 37, 57, 168, 204, 94, 101, 123,
        12, 42, 63, 184, 205, 98, 87, 129
    });

    private readonly SqlServerFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantProvisioningServiceTests"/> class.
    /// </summary>
    /// <param name="fixture">SQL Server test fixture.</param>
    public TenantProvisioningServiceTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Provisions a tenant database and persists encrypted connection metadata in master catalog.
    /// </summary>
    [Fact]
    public async Task ProvisionTenantDatabaseAsync_Should_CreateDatabase_ApplySchema_AndPersistTenant()
    {
        var masterDatabaseName = $"Master_{Guid.NewGuid():N}";
        var masterConnectionString = WithDatabase(_fixture.ConnectionString, masterDatabaseName);
        await EnsureDatabaseCreatedAsync(masterConnectionString);

        var options = new DbContextOptionsBuilder<MasterDbContext>()
            .UseSqlServer(masterConnectionString)
            .Options;

        await using var masterDbContext = new MasterDbContext(options);
        await masterDbContext.Database.EnsureCreatedAsync();

        ITenantRepository tenantRepository = new TenantRepository(masterDbContext);
        IUnitOfWork unitOfWork = new UnitOfWork(masterDbContext);
        IEncryptionService encryptionService = new AesEncryptionService(EncryptionKey);

        var service = new TenantProvisioningService(masterDbContext, tenantRepository, unitOfWork, encryptionService);
        var result = await service.ProvisionTenantDatabaseAsync("Agencia Alfa", "12345678000190", "agencia-alfa", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var persistedTenant = await masterDbContext.Tenants.SingleOrDefaultAsync(t => t.Id == result.Value);
        persistedTenant.Should().NotBeNull();
        persistedTenant!.EncryptedConnectionString.Should().NotBeNullOrWhiteSpace();

        var decryptedConnection = encryptionService.Decrypt(persistedTenant.EncryptedConnectionString);
        var tenantDatabaseName = new SqlConnectionStringBuilder(decryptedConnection).InitialCatalog;

        await DatabaseShouldExistAsync(_fixture.ConnectionString, tenantDatabaseName);
        await TenantSchemaMarkerTableShouldExistAsync(decryptedConnection);
        await MigrationHistoryTableShouldExistAsync(decryptedConnection);
    }

    /// <summary>
    /// Prevents reprovisioning when the same tenant database already exists.
    /// </summary>
    [Fact]
    public async Task ProvisionTenantDatabaseAsync_Should_ReturnConflict_WhenDatabaseAlreadyExists()
    {
        var masterDatabaseName = $"Master_{Guid.NewGuid():N}";
        var masterConnectionString = WithDatabase(_fixture.ConnectionString, masterDatabaseName);
        await EnsureDatabaseCreatedAsync(masterConnectionString);
        await EnsureDatabaseCreatedAsync(WithDatabase(_fixture.ConnectionString, "Tenant_agenciaconflito"));

        var options = new DbContextOptionsBuilder<MasterDbContext>()
            .UseSqlServer(masterConnectionString)
            .Options;

        await using var masterDbContext = new MasterDbContext(options);
        await masterDbContext.Database.EnsureCreatedAsync();

        ITenantRepository tenantRepository = new TenantRepository(masterDbContext);
        IUnitOfWork unitOfWork = new UnitOfWork(masterDbContext);
        IEncryptionService encryptionService = new AesEncryptionService(EncryptionKey);

        var service = new TenantProvisioningService(masterDbContext, tenantRepository, unitOfWork, encryptionService);

        var result = await service.ProvisionTenantDatabaseAsync("Agencia Beta", "12345678000191", "agencia-conflito", CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.DatabaseAlreadyExists");
    }

    private static async Task EnsureDatabaseCreatedAsync(string connectionString)
    {
        await using var connection = new SqlConnection(new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master"
        }.ConnectionString);

        await connection.OpenAsync();
        var databaseName = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
        await using var command = connection.CreateCommand();
        command.CommandText = $"IF DB_ID('{databaseName}') IS NULL CREATE DATABASE [{databaseName}]";
        await command.ExecuteNonQueryAsync();
    }

    private static string WithDatabase(string connectionString, string databaseName)
    {
        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = databaseName
        };

        return builder.ConnectionString;
    }

    private static async Task DatabaseShouldExistAsync(string rootConnectionString, string databaseName)
    {
        await using var connection = new SqlConnection(new SqlConnectionStringBuilder(rootConnectionString)
        {
            InitialCatalog = "master"
        }.ConnectionString);

        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM sys.databases WHERE name = @db";
        command.Parameters.AddWithValue("@db", databaseName);

        var count = Convert.ToInt32(await command.ExecuteScalarAsync(), null);
        count.Should().BeGreaterThan(0);
    }

    private static async Task TenantSchemaMarkerTableShouldExistAsync(string tenantConnectionString)
    {
        await using var connection = new SqlConnection(tenantConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'TenantSchemaMarkers'";

        var count = Convert.ToInt32(await command.ExecuteScalarAsync(), null);
        count.Should().BeGreaterThan(0);
    }

    private static async Task MigrationHistoryTableShouldExistAsync(string tenantConnectionString)
    {
        await using var connection = new SqlConnection(tenantConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '__EFMigrationsHistory'";

        var count = Convert.ToInt32(await command.ExecuteScalarAsync(), null);
        count.Should().BeGreaterThan(0);
    }
}