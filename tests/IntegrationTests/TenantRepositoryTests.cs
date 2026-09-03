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

    /// <summary>
    /// Verifies that GetTenantsForDunningEvaluationAsync returns tenants with overdue dates and persists DunningStage correctly.
    /// </summary>
    [Fact]
    public async Task GetTenantsForDunningEvaluationAsync_ShouldReturnTenantsRequiringDunning()
    {
        var masterDatabaseName = $"Master_Dunning_{Guid.NewGuid():N}";
        var masterConnectionString = WithDatabase(_fixture.ConnectionString, masterDatabaseName);
        await EnsureDatabaseCreatedAsync(masterConnectionString);

        var options = new DbContextOptionsBuilder<MasterDbContext>()
            .UseSqlServer(masterConnectionString)
            .Options;

        await using var dbContext = new MasterDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var tenant1 = Tenant.Create("Tenant Overdue", "12345678000191", "tenant-overdue").Value;
        tenant1.SetEncryptedConnectionString("enc-1");
        var dueDate = DateTime.UtcNow.AddDays(-5);
        tenant1.MarkPaymentOverdue(dueDate);
        tenant1.EvaluateDunningStage(DateTime.UtcNow);

        var tenant2 = Tenant.Create("Tenant Regular", "12345678000192", "tenant-regular").Value;
        tenant2.SetEncryptedConnectionString("enc-2");

        var repository = new TenantRepository(dbContext);
        IUnitOfWork unitOfWork = new UnitOfWork(dbContext);

        await repository.AddAsync(tenant1, CancellationToken.None);
        await repository.AddAsync(tenant2, CancellationToken.None);
        await unitOfWork.CommitAsync(CancellationToken.None);

        // Act
        var overdueList = await repository.GetTenantsForDunningEvaluationAsync(CancellationToken.None);

        // Assert
        overdueList.Should().ContainSingle(t => t.Id == tenant1.Id);
        var loaded = overdueList.Single(t => t.Id == tenant1.Id);
        loaded.DunningStage.Should().Be(DunningStage.AutomationsDisabled);
        loaded.PaymentDueDateUtc.Should().NotBeNull();
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