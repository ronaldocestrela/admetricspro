using FluentAssertions;
using IntegrationTests.Infrastructure;
using Master.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace IntegrationTests;

/// <summary>Valida a atualização de bancos operacionais criados pelas migrações históricas.</summary>
/// <param name="fixture">SQL Server isolado para executar as migrações reais.</param>
[Collection(SqlServerCollection.Name)]
public sealed class TenantOperationalMigrationTests(SqlServerFixture fixture)
{
    /// <summary>Atualiza o schema anterior à Meta independentemente da presença do índice opcional de usuários ativos.</summary>
    /// <param name="hasLegacyIndex">Indica se o banco legado possui o índice adicional.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpgradeFromSeptemberSchema_WithOptionalLegacyIndex_PreservesData(bool hasLegacyIndex)
    {
        var connection = new SqlConnectionStringBuilder(fixture.ConnectionString)
        {
            InitialCatalog = $"Tenant_MetaUpgrade_{Guid.NewGuid():N}"
        };
        await using var context = new TenantOperationalDbContext(
            new DbContextOptionsBuilder<TenantOperationalDbContext>()
                .UseSqlServer(connection.ConnectionString)
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
                .Options);
        await context.GetService<IMigrator>().MigrateAsync("20260915180000_Add_OAuthTokenVaults_Table");
        await context.Database.ExecuteSqlRawAsync("""
            INSERT INTO [TenantUsers] ([Id], [FullName], [Email], [PhoneNumber], [PasswordHash], [Role], [IsActive], [CreatedAtUtc])
            VALUES ('00000000-0000-0000-0000-000000000123', 'Migration Test', 'migration@example.test', '', 'test-hash', 'Owner', 1, SYSUTCDATETIME());
            """);
        if (hasLegacyIndex)
            await context.Database.ExecuteSqlRawAsync("CREATE INDEX [IX_TenantUsers_IsActive] ON [TenantUsers] ([IsActive]);");

        await context.Database.MigrateAsync();
        await context.Database.MigrateAsync();

        (await context.Database.GetPendingMigrationsAsync()).Should().BeEmpty();
        (await context.TenantUsers.CountAsync()).Should().Be(1);
        (await context.ConnectedAdAccounts.ToListAsync()).Should().BeEmpty();
        (await context.MetaApiOperations.CountAsync()).Should().Be(0);
    }
}
