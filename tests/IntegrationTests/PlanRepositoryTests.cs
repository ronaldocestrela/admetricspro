using BuildingBlocks.Application.Persistence;
using FluentAssertions;
using IntegrationTests.Infrastructure;
using Master.Domain.Plans;
using Master.Domain.Tenants;
using Master.Infrastructure.Persistence;
using Master.Infrastructure.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace IntegrationTests;

/// <summary>
/// Integration tests for subscription plan persistence and read-only repository behavior.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class PlanRepositoryTests
{
    private readonly SqlServerFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlanRepositoryTests"/> class.
    /// </summary>
    /// <param name="fixture">SQL Server test fixture.</param>
    public PlanRepositoryTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Verifies adding and retrieving a plan aggregate with limits and features.
    /// </summary>
    [Fact]
    public async Task AddAsync_And_GetByIdAsync_Should_PersistPlanWithLimitsAndFeatures()
    {
        var masterDatabaseName = $"Master_Plans_{Guid.NewGuid():N}";
        var masterConnectionString = WithDatabase(_fixture.ConnectionString, masterDatabaseName);
        await EnsureDatabaseCreatedAsync(masterConnectionString);

        var options = new DbContextOptionsBuilder<MasterDbContext>()
            .UseSqlServer(masterConnectionString)
            .Options;

        await using var dbContext = new MasterDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var limits = PlanLimits.Create(10, 5, 50_000m).Value;
        var features = PlanFeatures.Create(true, true, false, true).Value;
        var plan = SubscriptionPlan.Create("Plano Agência Pro", "Para médias agências", SubscriptionTier.Pro, 499m, 20, limits, features).Value;

        var repository = new PlanRepository(dbContext);
        var readOnlyRepo = new PlanReadOnlyRepository(dbContext);
        IUnitOfWork unitOfWork = new UnitOfWork(dbContext);

        await repository.AddAsync(plan, CancellationToken.None);
        await unitOfWork.CommitAsync(CancellationToken.None);

        var reloaded = await repository.GetByIdAsync(plan.Id, CancellationToken.None);
        reloaded.Should().NotBeNull();
        reloaded!.Name.Should().Be("Plano Agência Pro");
        reloaded.Limits.MaxSeats.Should().Be(10);
        reloaded.Limits.MaxWorkspaces.Should().Be(5);
        reloaded.Features.HasWhiteLabel.Should().BeTrue();
        reloaded.Features.HasAiCopilot.Should().BeFalse();

        var dtoList = await readOnlyRepo.ListAllAsync(false, CancellationToken.None);
        dtoList.Should().ContainSingle(p => p.Id == plan.Id.Value);
        var dto = dtoList.Single(p => p.Id == plan.Id.Value);
        dto.Name.Should().Be("Plano Agência Pro");
        dto.MaxSeats.Should().Be(10);
        dto.HasWhiteLabel.Should().BeTrue();
    }

    /// <summary>
    /// Verifies name uniqueness check in PlanRepository.
    /// </summary>
    [Fact]
    public async Task ExistsByNameAsync_Should_DetectDuplicateNames()
    {
        var masterDatabaseName = $"Master_Plans_{Guid.NewGuid():N}";
        var masterConnectionString = WithDatabase(_fixture.ConnectionString, masterDatabaseName);
        await EnsureDatabaseCreatedAsync(masterConnectionString);

        var options = new DbContextOptionsBuilder<MasterDbContext>()
            .UseSqlServer(masterConnectionString)
            .Options;

        await using var dbContext = new MasterDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var limits = PlanLimits.Create(5, 2, 10_000m).Value;
        var features = PlanFeatures.Default();
        var plan = SubscriptionPlan.Create("Plano Básico", "Desc", SubscriptionTier.Starter, 99m, 0, limits, features).Value;

        var repository = new PlanRepository(dbContext);
        IUnitOfWork unitOfWork = new UnitOfWork(dbContext);

        await repository.AddAsync(plan, CancellationToken.None);
        await unitOfWork.CommitAsync(CancellationToken.None);

        var exists = await repository.ExistsByNameAsync("plano básico", null, CancellationToken.None);
        exists.Should().BeTrue();

        var existsExcludingSelf = await repository.ExistsByNameAsync("plano básico", plan.Id, CancellationToken.None);
        existsExcludingSelf.Should().BeFalse();
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
