using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Master.Application.Services;
using Master.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace UnitTests.Backend.Persistence;

/// <summary>
/// Unit tests for <see cref="MasterDatabaseMigrationRunner"/>.
/// </summary>
public sealed class MasterDatabaseMigrationRunnerTests
{
    /// <summary>
    /// Validates that ApplyMigrationsAsync returns failure when database cannot be reached.
    /// </summary>
    [Fact]
    public async Task ApplyMigrationsAsync_ShouldReturnFailure_WhenDatabaseConnectionFails()
    {
        // Arrange: DbContext configured with an invalid, unreachable connection string
        var options = new DbContextOptionsBuilder<MasterDbContext>()
            .UseSqlServer("Server=invalid-unreachable-host;Database=MasterCatalog;Connect Timeout=1;TrustServerCertificate=True;")
            .Options;

        await using var dbContext = new MasterDbContext(options);
        var runner = new MasterDatabaseMigrationRunner(dbContext, NullLogger<MasterDatabaseMigrationRunner>.Instance);

        // Act
        var result = await runner.ApplyMigrationsAsync(CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("MasterMigration.ExecutionFailed");
    }

    /// <summary>
    /// Validates that ApplyMigrationsAsync respects cancellation token.
    /// </summary>
    [Fact]
    public async Task ApplyMigrationsAsync_ShouldReturnFailure_WhenCancelled()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<MasterDbContext>()
            .UseSqlServer("Server=localhost;Database=MasterCatalog;")
            .Options;

        await using var dbContext = new MasterDbContext(options);
        var runner = new MasterDatabaseMigrationRunner(dbContext, NullLogger<MasterDatabaseMigrationRunner>.Instance);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await runner.ApplyMigrationsAsync(cts.Token);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("MasterMigration.ExecutionFailed");
    }
}
