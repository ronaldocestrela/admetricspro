using BuildingBlocks.Domain.Integrations;
using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using IntegrationTests.Infrastructure;
using Master.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Infrastructure.Persistence;

namespace IntegrationTests.SocialPublishing;

/// <summary>
/// Testes de integração do <see cref="SocialPublishingDbContext"/> e fábrica por tenant.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class SocialPublishingDbContextTests(SqlServerFixture fixture)
{
    private const string PlaintextTokenMarker = "super-secret-access-token-plaintext";

    /// <summary>
    /// Valida fábrica, isolamento entre tenants, histórico de migrações e ciphertext persistido.
    /// </summary>
    [Fact]
    public async Task FactoryAndMigrations_Should_IsolateTenants_UseSocialHistory_AndPersistCiphertextOnly()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var workspaceA = Guid.NewGuid();
        var workspaceB = Guid.NewGuid();

        var dbA = $"Tenant_SocialA_{Guid.NewGuid():N}";
        var dbB = $"Tenant_SocialB_{Guid.NewGuid():N}";
        var connectionA = SocialPublishingTestDatabase.WithDatabase(fixture.ConnectionString, dbA);
        var connectionB = SocialPublishingTestDatabase.WithDatabase(fixture.ConnectionString, dbB);
        await SocialPublishingTestDatabase.EnsureDatabaseCreatedAsync(connectionA);
        await SocialPublishingTestDatabase.EnsureDatabaseCreatedAsync(connectionB);

        await ApplyOperationalMigrationsAsync(connectionA);
        await ApplyOperationalMigrationsAsync(connectionB);

        var migrator = new SocialPublishingSchemaMigrator(new FakeTenantConnectionResolver(new Dictionary<Guid, string>
        {
            [tenantA] = connectionA,
            [tenantB] = connectionB,
        }));

        (await migrator.ApplyAsync(connectionA, CancellationToken.None)).IsSuccess.Should().BeTrue();
        (await migrator.ApplyAsync(connectionB, CancellationToken.None)).IsSuccess.Should().BeTrue();

        var migrationId = "20261103005602_Initial_SocialPublishingSchema";
        await SocialHistoryShouldContainAsync(connectionA, migrationId);
        await DboHistoryShouldNotContainAsync(connectionA, migrationId);

        var factory = new SocialPublishingDbContextFactory(new FakeTenantConnectionResolver(new Dictionary<Guid, string>
        {
            [tenantA] = connectionA,
            [tenantB] = connectionB,
        }));

        var contextAResult = await factory.CreateDbContextAsync(tenantA, CancellationToken.None);
        contextAResult.IsSuccess.Should().BeTrue();
        contextAResult.Value.Database.GetDbConnection().Database.Should().Be(dbA);

        var ciphertext = "AES256-CBC-CIPHERTEXT-ONLY";
        var connectionEntity = OrganicConnection.Create(
            workspaceA,
            OrganicPlatform.Instagram,
            "ext-ig-1",
            "Conta Teste",
            "publish",
            ciphertext,
            null,
            DateTime.UtcNow.AddDays(7)).Value;

        await using (var writeContext = contextAResult.Value)
        {
            writeContext.OrganicConnections.Add(connectionEntity);
            await writeContext.SaveChangesAsync();
        }

        var storedToken = await ReadEncryptedAccessTokenAsync(connectionA, connectionEntity.Id);
        storedToken.Should().Be(ciphertext);
        storedToken.Should().NotContain(PlaintextTokenMarker);

        var resolver = new FakeTenantConnectionResolver(new Dictionary<Guid, string>
        {
            [tenantA] = connectionA,
            [tenantB] = connectionB,
        });
        resolver.SetCurrentTenant(tenantA);
        var factoryScoped = new SocialPublishingDbContextFactory(resolver);
        await using var readTenantA = (await factoryScoped.CreateDbContextAsync(CancellationToken.None)).Value;
        (await readTenantA.OrganicConnections.CountAsync()).Should().Be(1);

        resolver.SetCurrentTenant(tenantB);
        await using var readTenantB = (await factoryScoped.CreateDbContextAsync(CancellationToken.None)).Value;
        (await readTenantB.OrganicConnections.CountAsync()).Should().Be(0);

        await using var modelContext = new SocialPublishingDbContext(SocialPublishingDbContext.CreateOptions(connectionA));
        var entityTypeNames = modelContext.Model.GetEntityTypes().Select(t => t.ClrType.Name).ToList();
        entityTypeNames.Should().NotContain(nameof(Workspace));
        entityTypeNames.Should().NotContain(nameof(OAuthTokenVault));
        entityTypeNames.Should().NotContain(nameof(TenantBranding));
    }

    private static async Task ApplyOperationalMigrationsAsync(string connectionString)
    {
        await using var context = new TenantOperationalDbContext(
            new DbContextOptionsBuilder<TenantOperationalDbContext>()
                .UseSqlServer(connectionString)
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
                .Options);
        await context.Database.MigrateAsync();
    }

    private static async Task SocialHistoryShouldContainAsync(string connectionString, string migrationId)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM [social].[__EFMigrationsHistory] WHERE [MigrationId] = @id";
        command.Parameters.AddWithValue("@id", migrationId);
        var count = Convert.ToInt32(await command.ExecuteScalarAsync(), null);
        count.Should().Be(1);
    }

    private static async Task DboHistoryShouldNotContainAsync(string connectionString, string migrationId)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(1)
            FROM INFORMATION_SCHEMA.TABLES t
            INNER JOIN [dbo].[__EFMigrationsHistory] h ON 1 = 1
            WHERE t.TABLE_SCHEMA = 'dbo' AND t.TABLE_NAME = '__EFMigrationsHistory'
              AND h.[MigrationId] = @id
            """;
        command.Parameters.AddWithValue("@id", migrationId);
        var count = Convert.ToInt32(await command.ExecuteScalarAsync(), null);
        count.Should().Be(0);
    }

    private static async Task<string> ReadEncryptedAccessTokenAsync(string connectionString, Guid connectionId)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT [EncryptedAccessToken] FROM [social].[OrganicConnections] WHERE [Id] = @id";
        command.Parameters.AddWithValue("@id", connectionId);
        var value = await command.ExecuteScalarAsync();
        return Convert.ToString(value, null) ?? string.Empty;
    }
}
