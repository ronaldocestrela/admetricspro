using BuildingBlocks.Application.Persistence;
using FluentAssertions;
using IntegrationTests.Infrastructure;
using Master.Domain.Tenants;
using Master.Infrastructure.Persistence;
using Master.Infrastructure.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace IntegrationTests;

/// <summary>
/// Integration tests for tenant repository and unit of work behavior.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class TenantRepositoryTests
{
    private readonly SqlServerFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantRepositoryTests"/> class.
    /// </summary>
    /// <param name="fixture">SQL Server test fixture.</param>
    public TenantRepositoryTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Adds and retrieves tenant aggregate with explicit commit.
    /// </summary>
    [Fact]
    public async Task AddAsync_And_GetByIdAsync_Should_PersistTenant_WhenCommitted()
    {
        var masterDatabaseName = $"Master_{Guid.NewGuid():N}";
        var masterConnectionString = WithDatabase(_fixture.ConnectionString, masterDatabaseName);
        await EnsureDatabaseCreatedAsync(masterConnectionString);

        var options = new DbContextOptionsBuilder<MasterDbContext>()
            .UseSqlServer(masterConnectionString)
            .Options;

        await using var dbContext = new MasterDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var creationResult = Tenant.Create("Agencia Alfa", "12345678000190", "agencia-alfa");
        creationResult.IsSuccess.Should().BeTrue();
        creationResult.Value.SetEncryptedConnectionString("encrypted-placeholder").IsSuccess.Should().BeTrue();

        var repository = new TenantRepository(dbContext);
        IUnitOfWork unitOfWork = new UnitOfWork(dbContext);

        await repository.AddAsync(creationResult.Value, CancellationToken.None);
        await unitOfWork.CommitAsync(CancellationToken.None);

        var reloaded = await repository.GetByIdAsync(creationResult.Value.Id, CancellationToken.None);
        reloaded.Should().NotBeNull();
        reloaded!.CompanyName.Should().Be("Agencia Alfa");
        reloaded.Cnpj.Should().Be("12345678000190");
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