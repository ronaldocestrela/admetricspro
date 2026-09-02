using Testcontainers.MsSql;

namespace IntegrationTests.Infrastructure;

/// <summary>
/// SQL Server container fixture used by integration tests.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .WithCleanUp(true)
        .Build();

    /// <summary>
    /// Gets the SQL Server connection string for the running test container.
    /// </summary>
    public string ConnectionString => _container.GetConnectionString();

    /// <inheritdoc />
    public Task InitializeAsync() => _container.StartAsync();

    /// <inheritdoc />
    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

/// <summary>
/// Defines xUnit collection for SQL Server-backed integration tests.
/// </summary>
[CollectionDefinition(Name)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
    /// <summary>
    /// Collection name used by xUnit tests.
    /// </summary>
    public const string Name = "SqlServerCollection";
}