using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Tenants;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Persistence.Messaging;
using FluentAssertions;
using IntegrationTests.Infrastructure;
using Master.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace IntegrationTests.Messaging;

/// <summary>
/// Testes de integração da outbox transacional (Fase 3).
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class IntegrationOutboxPersistenceTests(SqlServerFixture fixture)
{
    private static readonly DateTime FixedUtc = new(2026, 11, 25, 15, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Agregado e outbox commitam na mesma transação.
    /// </summary>
    [Fact]
    public async Task SaveChanges_ComMarkerEOutbox_CommitamJuntos()
    {
        var connectionString = await CreateAndMigrateTenantDatabaseAsync();
        await using var context = CreateTenantContext(connectionString);

        var marker = new TenantSchemaMarker { Name = "messaging-test" };
        var outbox = IntegrationOutboxMessage.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "messaging.ping.v1",
            """{"ok":true}""",
            Guid.NewGuid(),
            FixedUtc).Value;

        context.TenantSchemaMarkers.Add(marker);
        context.IntegrationOutboxMessages.Add(outbox);
        await context.SaveChangesAsync();

        await using var verify = CreateTenantContext(connectionString);
        (await verify.TenantSchemaMarkers.CountAsync()).Should().Be(1);
        (await verify.IntegrationOutboxMessages.CountAsync()).Should().Be(1);
    }

    /// <summary>
    /// Falha antes do save não persiste outbox.
    /// </summary>
    [Fact]
    public async Task SaveChanges_QuandoTransacaoAborta_NaoDeixaOutbox()
    {
        var connectionString = await CreateAndMigrateTenantDatabaseAsync();
        await using var context = CreateTenantContext(connectionString);

        var outbox = IntegrationOutboxMessage.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "messaging.ping.v1",
            "{}",
            null,
            FixedUtc).Value;

        await using var transaction = await context.Database.BeginTransactionAsync();
        context.IntegrationOutboxMessages.Add(outbox);
        await context.SaveChangesAsync();
        await transaction.RollbackAsync();

        await using var verify = CreateTenantContext(connectionString);
        (await verify.IntegrationOutboxMessages.CountAsync()).Should().Be(0);
    }

    /// <summary>
    /// EventId duplicado viola índice único.
    /// </summary>
    [Fact]
    public async Task SaveChanges_EventIdDuplicado_DeveLancar()
    {
        var connectionString = await CreateAndMigrateTenantDatabaseAsync();
        var eventId = Guid.NewGuid();

        await using (var context = CreateTenantContext(connectionString))
        {
            context.IntegrationOutboxMessages.Add(IntegrationOutboxMessage.Create(
                Guid.NewGuid(), eventId, "messaging.ping.v1", "{}", null, FixedUtc).Value);
            await context.SaveChangesAsync();
        }

        await using var second = CreateTenantContext(connectionString);
        second.IntegrationOutboxMessages.Add(IntegrationOutboxMessage.Create(
            Guid.NewGuid(), eventId, "messaging.ping.v1", "{}", null, FixedUtc).Value);

        var act = () => second.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    /// <summary>
    /// Dois claims concorrentes: apenas um vence.
    /// </summary>
    [Fact]
    public async Task TryClaimNextAsync_Concorrente_ApenasUmVence()
    {
        var connectionString = await CreateAndMigrateTenantDatabaseAsync();
        var messageId = Guid.NewGuid();

        await using (var seed = CreateTenantContext(connectionString))
        {
            seed.IntegrationOutboxMessages.Add(IntegrationOutboxMessage.Create(
                messageId,
                Guid.NewGuid(),
                "messaging.ping.v1",
                "{}",
                null,
                FixedUtc).Value);
            await seed.SaveChangesAsync();
        }

        await using var ctx1 = CreateTenantContext(connectionString);
        await using var ctx2 = CreateTenantContext(connectionString);
        var store1 = new DbIntegrationOutboxStore(ctx1);
        var store2 = new DbIntegrationOutboxStore(ctx2);

        var results = await Task.WhenAll(
            store1.TryClaimNextAsync(FixedUtc.AddMinutes(1)),
            store2.TryClaimNextAsync(FixedUtc.AddMinutes(1)));

        results.Count(r => r != null).Should().Be(1);
        results.Single(r => r != null)!.Id.Should().Be(messageId);
    }

    /// <summary>
    /// InProgress expirado é reclaimado; recente permanece.
    /// </summary>
    [Fact]
    public async Task ReclaimStaleAsync_ExpiradoVoltaPending_RecentePermanece()
    {
        var connectionString = await CreateAndMigrateTenantDatabaseAsync();
        var staleId = Guid.NewGuid();
        var freshId = Guid.NewGuid();
        var oldClaim = FixedUtc.AddMinutes(-30);
        var freshClaim = FixedUtc.AddMinutes(-1);

        await using (var seed = CreateTenantContext(connectionString))
        {
            var stale = IntegrationOutboxMessage.Create(
                staleId, Guid.NewGuid(), "messaging.ping.v1", "{}", null, FixedUtc).Value;
            stale.MarkInProgress(oldClaim).IsSuccess.Should().BeTrue();

            var fresh = IntegrationOutboxMessage.Create(
                freshId, Guid.NewGuid(), "messaging.ping.v1", "{}", null, FixedUtc).Value;
            fresh.MarkInProgress(freshClaim).IsSuccess.Should().BeTrue();

            seed.IntegrationOutboxMessages.AddRange(stale, fresh);
            await seed.SaveChangesAsync();
        }

        await using var context = CreateTenantContext(connectionString);
        var store = new DbIntegrationOutboxStore(context);
        var reclaimed = await store.ReclaimStaleAsync(FixedUtc, TimeSpan.FromMinutes(10));

        reclaimed.Should().Be(1);

        var reloaded = await context.IntegrationOutboxMessages.AsNoTracking().ToListAsync();
        reloaded.Single(m => m.Id == staleId).Status.Should().Be(IntegrationOutboxMessageStatus.Pending);
        reloaded.Single(m => m.Id == freshId).Status.Should().Be(IntegrationOutboxMessageStatus.InProgress);
    }

    private async Task<string> CreateAndMigrateTenantDatabaseAsync()
    {
        var builder = new SqlConnectionStringBuilder(fixture.ConnectionString)
        {
            InitialCatalog = $"Tenant_Messaging_{Guid.NewGuid():N}",
        };

        await using var context = CreateTenantContext(builder.ConnectionString);
        await context.Database.MigrateAsync();
        return builder.ConnectionString;
    }

    private static TenantOperationalDbContext CreateTenantContext(string connectionString) =>
        new(new DbContextOptionsBuilder<TenantOperationalDbContext>()
            .UseSqlServer(connectionString)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options);
}