using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Infrastructure.MultiTenancy;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Security;
using FluentAssertions;
using IntegrationTests.Infrastructure;
using Master.Application.Repositories;
using Master.Infrastructure.Persistence;
using Master.Infrastructure.Repositories;
using Master.Infrastructure.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace IntegrationTests;

/// <summary>
/// Integration tests verifying dynamic tenant connection resolution, caching, and database-per-tenant isolation.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class TenantConnectionResolverIntegrationTests
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
    /// Initializes a new instance of the <see cref="TenantConnectionResolverIntegrationTests"/> class.
    /// </summary>
    /// <param name="fixture">SQL Server test fixture.</param>
    public TenantConnectionResolverIntegrationTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Verifies end-to-end flow: provisioning tenants, resolving connections via cache, and validating isolated tenant database storage.
    /// </summary>
    [Fact]
    public async Task DynamicResolution_And_DbContextFactory_ShouldEnsureDatabasePerTenantIsolation()
    {
        // 1. Setup fresh MasterDb
        var masterDbName = $"Master_{Guid.NewGuid():N}";
        var masterConnectionString = WithDatabase(_fixture.ConnectionString, masterDbName);
        await EnsureDatabaseCreatedAsync(masterConnectionString);

        var masterOptions = new DbContextOptionsBuilder<MasterDbContext>()
            .UseSqlServer(masterConnectionString)
            .Options;

        await using var masterDbContext = new MasterDbContext(masterOptions);
        await masterDbContext.Database.EnsureCreatedAsync();

        // 2. Setup infrastructure services
        ITenantRepository tenantRepository = new TenantRepository(masterDbContext);
        IUnitOfWork unitOfWork = new UnitOfWork(masterDbContext);
        IEncryptionService encryptionService = new AesEncryptionService(EncryptionKey);
        var provisioningService = new TenantProvisioningService(masterDbContext, tenantRepository, unitOfWork, encryptionService);
        var tenantContextAccessor = new TenantContextAccessor();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var connectionResolver = new CachedTenantConnectionResolver(tenantRepository, encryptionService, tenantContextAccessor, memoryCache);
        var dbContextFactory = new TenantDbContextFactory<TenantDbContext>(connectionResolver);

        // 3. Provision two distinct tenants with unique subdomains
        var testRunId = Guid.NewGuid().ToString("N")[..6];
        var subdomainA = $"alfa-{testRunId}";
        var subdomainB = $"beta-{testRunId}";

        var tenantAResult = await provisioningService.ProvisionTenantDatabaseAsync(
            "Agencia Alfa", "11111111000111", subdomainA, CancellationToken.None);
        tenantAResult.IsSuccess.Should().BeTrue(tenantAResult.Error.Code + ": " + tenantAResult.Error.Description);
        var tenantAId = tenantAResult.Value.Value;

        var tenantBResult = await provisioningService.ProvisionTenantDatabaseAsync(
            "Agencia Beta", "22222222000222", subdomainB, CancellationToken.None);
        tenantBResult.IsSuccess.Should().BeTrue(tenantBResult.Error.Code + ": " + tenantBResult.Error.Description);
        var tenantBId = tenantBResult.Value.Value;

        // 4. Resolve connection strings using the resolver
        var connAResult = await connectionResolver.ResolveConnectionStringAsync(tenantAId);
        connAResult.IsSuccess.Should().BeTrue();
        var connBResult = await connectionResolver.ResolveConnectionStringAsync(tenantBId);
        connBResult.IsSuccess.Should().BeTrue();

        connAResult.Value.Should().NotBe(connBResult.Value);
        new SqlConnectionStringBuilder(connAResult.Value).InitialCatalog.Should().Contain("alfa");
        new SqlConnectionStringBuilder(connBResult.Value).InitialCatalog.Should().Contain("beta");

        // 5. Create DbContext for Tenant A and write data
        var contextAResult = await dbContextFactory.CreateDbContextAsync(tenantAId);
        contextAResult.IsSuccess.Should().BeTrue();
        await using (var contextA = contextAResult.Value)
        {
            contextA.TenantSchemaMarkers.Add(new TenantSchemaMarker { Name = "Marker-Alfa" });
            await contextA.SaveChangesAsync();
        }

        // 6. Create DbContext for Tenant B and write data
        var contextBResult = await dbContextFactory.CreateDbContextAsync(tenantBId);
        contextBResult.IsSuccess.Should().BeTrue();
        await using (var contextB = contextBResult.Value)
        {
            contextB.TenantSchemaMarkers.Add(new TenantSchemaMarker { Name = "Marker-Beta" });
            await contextB.SaveChangesAsync();
        }

        // 7. Verify physical database isolation between tenants
        await using (var verifyContextA = (await dbContextFactory.CreateDbContextAsync(tenantAId)).Value)
        {
            var markersA = await verifyContextA.TenantSchemaMarkers.ToListAsync();
            markersA.Should().ContainSingle();
            markersA[0].Name.Should().Be("Marker-Alfa");
        }

        await using (var verifyContextB = (await dbContextFactory.CreateDbContextAsync(tenantBId)).Value)
        {
            var markersB = await verifyContextB.TenantSchemaMarkers.ToListAsync();
            markersB.Should().ContainSingle();
            markersB[0].Name.Should().Be("Marker-Beta");
        }

        // 8. Test contextual resolution via TenantContextAccessor
        tenantContextAccessor.TenantContext = TenantContext.Create(tenantAId, subdomainA, TenantResolutionSource.Subdomain, subdomainA);
        var contextualConnResult = await connectionResolver.ResolveCurrentTenantConnectionStringAsync();
        contextualConnResult.IsSuccess.Should().BeTrue();
        new SqlConnectionStringBuilder(contextualConnResult.Value).InitialCatalog.Should().Contain("alfa");

        var contextualContextResult = await dbContextFactory.CreateDbContextAsync();
        contextualContextResult.IsSuccess.Should().BeTrue();
        await using (var contextualContext = contextualContextResult.Value)
        {
            var markers = await contextualContext.TenantSchemaMarkers.ToListAsync();
            markers.Should().ContainSingle();
            markers[0].Name.Should().Be("Marker-Alfa");
        }
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
}
