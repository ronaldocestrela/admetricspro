using FluentAssertions;
using IntegrationTests.Infrastructure;
using Master.Application.Services;
using Master.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace IntegrationTests;

/// <summary>
/// Integration tests for master database automated migrations runner.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class MasterDatabaseMigrationIntegrationTests
{
    private readonly SqlServerFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="MasterDatabaseMigrationIntegrationTests"/> class.
    /// </summary>
    /// <param name="fixture">SQL Server container fixture.</param>
    public MasterDatabaseMigrationIntegrationTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Verifies that ApplyMigrationsAsync applies pending migrations on a clean database and is idempotent.
    /// </summary>
    [Fact]
    public async Task ApplyMigrationsAsync_Should_ApplyMasterMigrations_AndBeIdempotent()
    {
        var masterDbName = $"Master_Mig_{Guid.NewGuid():N}";
        var masterConnString = WithDatabase(_fixture.ConnectionString, masterDbName);
        await EnsureDatabaseCreatedAsync(masterConnString);

        var options = new DbContextOptionsBuilder<MasterDbContext>()
            .UseSqlServer(masterConnString)
            .Options;

        await using var context = new MasterDbContext(options);
        IMasterDatabaseMigrationRunner runner = new MasterDatabaseMigrationRunner(
            context,
            NullLogger<MasterDatabaseMigrationRunner>.Instance);

        // First run: applies initial migrations
        var firstResult = await runner.ApplyMigrationsAsync(CancellationToken.None);
        firstResult.IsSuccess.Should().BeTrue();

        // Verify tables exist
        await TableShouldExistAsync(masterConnString, "__EFMigrationsHistory");
        await TableShouldExistAsync(masterConnString, "Tenants");

        // Second run: idempotency verification (zero pending migrations, succeeds cleanly)
        var secondResult = await runner.ApplyMigrationsAsync(CancellationToken.None);
        secondResult.IsSuccess.Should().BeTrue();
    }

    private static async Task EnsureDatabaseCreatedAsync(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        var targetDb = builder.InitialCatalog;
        builder.InitialCatalog = "master";

        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"IF DB_ID('{targetDb}') IS NULL CREATE DATABASE [{targetDb}]";
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

    private static async Task TableShouldExistAsync(string connectionString, string tableName)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @tableName";
        command.Parameters.AddWithValue("@tableName", tableName);

        var count = Convert.ToInt32(await command.ExecuteScalarAsync(), null);
        count.Should().BeGreaterThan(0, $"Table {tableName} should exist after migrations.");
    }
}
