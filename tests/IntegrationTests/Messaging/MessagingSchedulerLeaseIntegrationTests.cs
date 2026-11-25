using BuildingBlocks.Application.Messaging.Persistence;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.Persistence.Messaging;
using FluentAssertions;
using IntegrationTests.Infrastructure;
using Master.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace IntegrationTests.Messaging;

/// <summary>
/// Testes de concorrência do lease de scheduler no MasterDb.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class MessagingSchedulerLeaseIntegrationTests(SqlServerFixture fixture)
{
    /// <summary>
    /// Duas instâncias competindo: apenas uma detém o lease até expirar.
    /// </summary>
    [Fact]
    public async Task TryAcquireOrRenew_SecondHolderFailsUntilLeaseExpires()
    {
        await using var context = await CreateMigratedContextAsync();
        var store = new DbMessagingSchedulerLeaseStore(context);
        var utcNow = DateTime.UtcNow;
        var duration = TimeSpan.FromSeconds(30);

        var first = await store.TryAcquireOrRenewAsync(
            MessagingSchedulerLease.OutboxDispatchLeaseKey,
            "holder-a",
            utcNow,
            duration);
        first.IsSuccess.Should().BeTrue();

        var second = await store.TryAcquireOrRenewAsync(
            MessagingSchedulerLease.OutboxDispatchLeaseKey,
            "holder-b",
            utcNow,
            duration);
        second.IsFailure.Should().BeTrue();
        second.Error.Code.Should().Be("Messaging.LeaseNotAcquired");

        var afterExpiry = utcNow.Add(duration).AddSeconds(1);
        var takeover = await store.TryAcquireOrRenewAsync(
            MessagingSchedulerLease.OutboxDispatchLeaseKey,
            "holder-b",
            afterExpiry,
            duration);
        takeover.IsSuccess.Should().BeTrue();
    }

    /// <summary>
    /// Duas aquisições simultâneas da mesma chave não lançam exceção: só uma vence.
    /// </summary>
    [Fact]
    public async Task TryAcquireOrRenew_ConcurrentInsert_OnlyOneHolderSucceeds()
    {
        var databaseName = $"Master_Lease_{Guid.NewGuid():N}";
        var connectionString = new SqlConnectionStringBuilder(fixture.ConnectionString)
        {
            InitialCatalog = databaseName,
        }.ConnectionString;

        var options = new DbContextOptionsBuilder<MasterDbContext>().UseSqlServer(connectionString).Options;
        await using (var migrate = new MasterDbContext(options))
        {
            await migrate.Database.MigrateAsync();
        }

        var utcNow = DateTime.UtcNow;
        var duration = TimeSpan.FromSeconds(30);

        async Task<Result> AcquireAsync(string holder)
        {
            await using var context = new MasterDbContext(options);
            var store = new DbMessagingSchedulerLeaseStore(context);
            return await store.TryAcquireOrRenewAsync(
                MessagingSchedulerLease.OutboxDispatchLeaseKey,
                holder,
                utcNow,
                duration);
        }

        var results = await Task.WhenAll(AcquireAsync("holder-a"), AcquireAsync("holder-b"));

        results.Count(result => result.IsSuccess).Should().Be(1);
        results.Single(result => result.IsFailure).Error.Code.Should().Be("Messaging.LeaseNotAcquired");
    }

    private async Task<MasterDbContext> CreateMigratedContextAsync()
    {
        var options = new DbContextOptionsBuilder<MasterDbContext>()
            .UseSqlServer(fixture.ConnectionString)
            .Options;
        var context = new MasterDbContext(options);
        await context.Database.MigrateAsync();
        return context;
    }
}